using Game.Core.Combat;
using Godot;
using MmoRpgClient.Data;
using MmoRpgClient.Match;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Account;

/// <summary>
/// Tela inicial (T-220): login/cadastro na <c>Game.Api</c>, lista de personagens, criação (nome + classe, com descrição
/// e prévia do sprite) e exclusão com confirmação. "Jogar" pede o token de jogo e conecta no servidor com ele.
/// O token de sessão fica só em memória (<see cref="ApiClient"/>); senha e tokens nunca vão para disco.
/// "Modo de teste" abre o menu de encontros locais (T-024). Argumentos de depuração (depois de <c>--</c>):
/// <c>--api=url</c>, <c>--game-server=host:porta</c>, <c>--login=email:senha</c> (entra sozinho), <c>--register</c>
/// (cria a conta antes), <c>--create=Nome:class.id</c>, <c>--play=Nome</c>, <c>--account-view=create</c> e
/// <c>--capture=caminho.png</c> (salva a tela após 2,5 s e fecha).
/// Os argumentos de partida direta (<c>--encounter</c>, <c>--server</c>...) seguem para o menu, como antes.
/// </summary>
public partial class AccountScreen : Control
{
    public const string ScenePath = "res://scenes/Account.tscn";
    public const string DefaultApi = "http://127.0.0.1:8080";

    private static readonly string[] DirectArgs = ["encounter", "class", "fake-authority", "demo-input", "auto-move", "hold", "ranks", "rank", "server"];
    private static readonly Color ErrorColor = new(1f, 0.5f, 0.4f);
    private static readonly Color InfoColor = new(0.7f, 1f, 0.7f);

    // Sobrevivem à troca de cena (jogar → voltar), só em memória.
    private static ApiClient? s_api;
    private static string? s_gameServer;
    private static string? s_email;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private readonly VBoxContainer _root = new() { Position = new Godot.Vector2(48, 36), CustomMinimumSize = new Godot.Vector2(700, 0) };
    private readonly Label _status = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Godot.Vector2(700, 0) };
    private ContentCatalog _content = ContentCatalog.Empty;
    private SpriteCatalog _sprites = SpriteCatalog.Empty;
    private IReadOnlyList<CharacterSummary> _characters = [];
    private bool _busy;
    private double _elapsed;

    public override void _Ready()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            var trimmed = arg.TrimStart('-');
            var eq = trimmed.IndexOf('=', StringComparison.Ordinal);
            _args[eq < 0 ? trimmed : trimmed[..eq]] = eq < 0 ? "" : trimmed[(eq + 1)..];
        }

        if (_args.Keys.Any(k => DirectArgs.Contains(k)) && MatchSettings.Current is null)
        {
            CallDeferred(MethodName.OpenLocalMenu);
            return;
        }

        SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(new ColorRect { Color = new Color(0.08f, 0.09f, 0.11f), AnchorRight = 1, AnchorBottom = 1 });
        _root.AddThemeConstantOverride("separation", 10);
        AddChild(_root);
        try
        {
            _content = ContentLoader.Load();
            _sprites = SpriteCatalog.Load();
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            _root.AddChild(new Label { Text = $"Erro ao carregar dados: {ex.Message}" });
            return;
        }

        if (MenuScreen.LastError is { } error)
        {
            MenuScreen.LastError = null;
            SetStatus(error, ErrorColor);
        }

        if (s_api is { LoggedIn: true })
        {
            Forget(ShowCharacters());
        }
        else if (_args.TryGetValue("login", out var login) && login.Split(':', 2) is [var email, var password])
        {
            ShowLogin();
            Forget(AutoLogin(email, password));
        }
        else
        {
            ShowLogin();
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_args.TryGetValue("capture", out var path) && _elapsed > 2.5)
        {
            _args.Remove("capture");
            GetViewport().GetTexture().GetImage().SavePng(path);
            GetTree().Quit();
        }
    }

    // ---------------- telas ----------------

    private void ShowLogin()
    {
        Clear("Entrar");
        var api = Field("Servidor de contas", s_api?.BaseUrl ?? _args.GetValueOrDefault("api", DefaultApi));
        var game = Field("Servidor do jogo (host:porta, usado se a conta não informar)", s_gameServer ?? _args.GetValueOrDefault("game-server", GameServerFrom(api.Text)));
        var email = Field("E-mail", s_email ?? "");
        var password = Field("Senha", "");
        password.Secret = true;
        api.TextChanged += text => game.Text = s_gameServer ?? _args.GetValueOrDefault("game-server", GameServerFrom(text));
        password.TextSubmitted += _ => Forget(Login(api.Text, game.Text, email.Text, password.Text, register: false));

        var buttons = Row();
        buttons.AddChild(ButtonFor("Entrar", () => Forget(Login(api.Text, game.Text, email.Text, password.Text, register: false)), 140));
        buttons.AddChild(ButtonFor("Criar conta", () => Forget(Login(api.Text, game.Text, email.Text, password.Text, register: true)), 140));
        _root.AddChild(Hint("Criar conta usa o e-mail e a senha acima (senha de 8 a 128 caracteres) e já entra."));
        AddStatus();
        AddLocalMode();
    }

    private async Task ShowCharacters()
    {
        Clear("Personagens");
        _root.AddChild(Hint($"Conta: {s_api?.Email} · {s_api?.BaseUrl}"));
        var list = new ItemList { CustomMinimumSize = new Godot.Vector2(700, 200) };
        _root.AddChild(list);
        var buttons = Row();
        var play = ButtonFor("Jogar", () => Forget(Play(list)), 140);
        var delete = ButtonFor("Apagar", () => ConfirmDelete(list), 120);
        buttons.AddChild(play);
        buttons.AddChild(ButtonFor("Criar personagem", ShowCreate, 180));
        buttons.AddChild(delete);
        buttons.AddChild(ButtonFor("Sair da conta", Logout, 140));
        list.ItemActivated += _ => Forget(Play(list));
        AddStatus();
        AddLocalMode();

        await Run(async () =>
        {
            _characters = await s_api!.Characters();
            list.Clear();
            foreach (var c in _characters)
            {
                list.AddItem($"{c.Name}  —  {ClassName(c.ClassId)}, nível {c.Level}  ·  {ZoneName(c.ZoneId)}");
            }

            if (_characters.Count > 0)
            {
                list.Select(0);
            }
            else
            {
                SetStatus("Nenhum personagem ainda. Crie o primeiro.", InfoColor);
            }

            play.Disabled = delete.Disabled = _characters.Count == 0;
        });
    }

    private void ShowCreate()
    {
        Clear("Criar personagem");
        var name = Field("Nome (3 a 16 letras ou números, começando por letra)", "");
        name.MaxLength = 16;
        _root.AddChild(Caption("Classe"));
        var row = Row();
        var classes = new ItemList { CustomMinimumSize = new Godot.Vector2(200, 192) };
        var ids = _content.Classes.Values.OrderBy(c => ClassOrder(c.Id)).Select(c => c.Id).ToList();
        foreach (var id in ids)
        {
            classes.AddItem(ClassName(id));
        }

        var preview = new SpritePreview();
        var description = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Godot.Vector2(280, 0) };
        row.AddChild(classes);
        row.AddChild(preview);
        row.AddChild(description);
        classes.ItemSelected += index =>
        {
            preview.Sprite = _sprites.For(ids[(int)index]);
            description.Text = ClassText.Describe(ids[(int)index]);
        };
        classes.Select(0);
        preview.Sprite = _sprites.For(ids[0]);
        description.Text = ClassText.Describe(ids[0]);

        var buttons = Row();
        buttons.AddChild(ButtonFor("Criar", () => Forget(Create(name.Text, ids[classes.GetSelectedItems().FirstOrDefault()])), 140));
        buttons.AddChild(ButtonFor("Voltar", () => Forget(ShowCharacters()), 120));
        name.TextSubmitted += _ => Forget(Create(name.Text, ids[classes.GetSelectedItems().FirstOrDefault()]));
        AddStatus();
        name.GrabFocus();
    }

    // ---------------- ações ----------------

    private async Task AutoLogin(string email, string password)
    {
        var api = _args.GetValueOrDefault("api", DefaultApi);
        await Login(api, _args.GetValueOrDefault("game-server", GameServerFrom(api)), email, password, register: _args.ContainsKey("register"));
        if (s_api is not { LoggedIn: true })
        {
            return;
        }

        if (_args.TryGetValue("create", out var create) && create.Split(':', 2) is [var name, var classId])
        {
            await Create(name, classId);
        }

        if (_args.TryGetValue("play", out var play) && _characters.FirstOrDefault(c => c.Name == play) is { } character)
        {
            await Play(character);
        }
        else if (_args.GetValueOrDefault("account-view") == "create")
        {
            ShowCreate();
        }
    }

    private async Task Login(string apiUrl, string gameServer, string email, string password, bool register)
    {
        if (!Uri.TryCreate(apiUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            SetStatus("Endereço do servidor de contas inválido (ex.: http://127.0.0.1:8080).", ErrorColor);
            return;
        }

        if (OnlineSettings.Parse(gameServer, "") is null)
        {
            SetStatus("Endereço do servidor do jogo inválido (ex.: 127.0.0.1:7777).", ErrorColor);
            return;
        }

        var api = s_api?.BaseUrl == apiUrl.Trim().TrimEnd('/') ? s_api : new ApiClient(apiUrl.Trim());
        var ok = await Run(async () =>
        {
            if (register)
            {
                await api.Register(email.Trim(), password);
            }

            await api.Login(email.Trim(), password);
        }, register ? "Criando a conta..." : "Entrando...");
        if (ok)
        {
            s_api = api;
            s_gameServer = gameServer.Trim();
            s_email = email.Trim();
            await ShowCharacters();
        }
    }

    private async Task Create(string name, string classId)
    {
        if (await Run(() => s_api!.Create(name.Trim(), classId), "Criando..."))
        {
            await ShowCharacters();
            SetStatus($"{name.Trim()} criado.", InfoColor);
        }
    }

    private void ConfirmDelete(ItemList list)
    {
        if (Selected(list) is not { } character)
        {
            return;
        }

        var dialog = new ConfirmationDialog
        {
            Title = "Apagar personagem",
            DialogText = $"Apagar {character.Name} ({ClassName(character.ClassId)}, nível {character.Level})?\nIsso não pode ser desfeito.",
            OkButtonText = "Apagar",
            CancelButtonText = "Cancelar",
        };
        dialog.Confirmed += async () =>
        {
            if (await Run(() => s_api!.Delete(character.Id), "Apagando..."))
            {
                await ShowCharacters();
                SetStatus($"{character.Name} apagado.", InfoColor);
            }
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private async Task Play(ItemList list)
    {
        if (Selected(list) is { } character)
        {
            await Play(character);
        }
    }

    private async Task Play(CharacterSummary character)
    {
        PlayTicket? ticket = null;
        if (!await Run(async () => ticket = await s_api!.Play(character.Id), "Pedindo entrada..."))
        {
            return;
        }

        var address = string.IsNullOrWhiteSpace(ticket!.Address) ? s_gameServer ?? "" : ticket.Address;
        if (OnlineSettings.Parse(address, character.Name) is not { } online)
        {
            SetStatus($"Endereço do servidor do jogo inválido: '{address}'.", ErrorColor);
            return;
        }

        var encounter = MenuScreen.OrderedEncounterIds(_content).FirstOrDefault() ?? "";
        MatchSettings.Current = new MatchSettings(encounter, character.ClassId, MatchSettings.RandomSeed(),
            online with { Ticket = ticket.Ticket });
        GetTree().ChangeSceneToFile(Main.ScenePath);
    }

    private void Logout()
    {
        s_api?.Logout();
        s_api = null;
        ShowLogin();
    }

    /// <summary>Dispara uma ação assíncrona a partir de um sinal; os erros já viram mensagem em <see cref="Run"/>.</summary>
    private static async void Forget(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
        }
    }

    private void OpenLocalMenu() => GetTree().ChangeSceneToFile(MenuScreen.ScenePath);

    /// <summary>Roda um pedido à API com o botão travado e a mensagem de erro na tela. Sessão expirada volta ao login.</summary>
    private async Task<bool> Run(Func<Task> action, string? progress = null)
    {
        if (_busy)
        {
            return false;
        }

        _busy = true;
        if (progress is not null)
        {
            SetStatus(progress, InfoColor);
        }

        try
        {
            await action();
            if (_status.Text == progress)
            {
                SetStatus("", InfoColor);
            }

            return true;
        }
        catch (ApiException ex)
        {
            GD.Print($"[api] {ex.Code}: {ex.Message}");
            if (ex.Code is "session_expired" or "not_logged_in")
            {
                s_api = null;
                ShowLogin();
            }

            SetStatus(ex.Message, ErrorColor);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    // ---------------- montagem ----------------

    private void Clear(string title)
    {
        foreach (var child in _root.GetChildren())
        {
            _root.RemoveChild(child);
            if (child != _status)
            {
                child.QueueFree(); // o rótulo de mensagens é reaproveitado entre as telas
            }
        }

        var label = new Label { Text = title };
        label.AddThemeFontSizeOverride("font_size", 30);
        _root.AddChild(label);
    }

    private void AddStatus()
    {
        _status.GetParent()?.RemoveChild(_status);
        _root.AddChild(_status);
    }

    private void AddLocalMode()
    {
        _root.AddChild(new HSeparator());
        _root.AddChild(ButtonFor("Modo de teste (encontros locais)", OpenLocalMenu, 280));
    }

    private void SetStatus(string text, Color color)
    {
        _status.Text = text;
        _status.AddThemeColorOverride("font_color", color);
    }

    private LineEdit Field(string caption, string text)
    {
        _root.AddChild(Caption(caption));
        var edit = new LineEdit { Text = text, CustomMinimumSize = new Godot.Vector2(420, 0) };
        _root.AddChild(edit);
        return edit;
    }

    private HBoxContainer Row()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _root.AddChild(row);
        return row;
    }

    private static Button ButtonFor(string text, Action pressed, float width)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Godot.Vector2(width, 36) };
        button.Pressed += pressed;
        return button;
    }

    private static Label Caption(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.45f));
        return label;
    }

    private static Label Hint(string text) => new()
    {
        Text = text,
        Modulate = new Color(1, 1, 1, 0.65f),
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        CustomMinimumSize = new Godot.Vector2(700, 0),
    };

    private CharacterSummary? Selected(ItemList list) =>
        list.GetSelectedItems() is [var index, ..] && index < _characters.Count ? _characters[index] : null;

    private string ClassName(string id) => _content.Classes.TryGetValue(id, out var c) ? c.Name : id;

    private string ZoneName(string id) => _content.Zones.TryGetValue(id, out var zone) ? zone.Name : id;

    /// <summary>Servidor do jogo padrão: o mesmo host da API, porta 7777.</summary>
    private static string GameServerFrom(string apiUrl) =>
        Uri.TryCreate(apiUrl.Trim(), UriKind.Absolute, out var uri) ? $"{uri.Host}:7777" : "127.0.0.1:7777";

    private static int ClassOrder(string id) => id switch
    {
        "class.warrior" => 0,
        "class.archer" => 1,
        "class.mage" => 2,
        "class.cleric" => 3,
        _ => 10,
    };
}
