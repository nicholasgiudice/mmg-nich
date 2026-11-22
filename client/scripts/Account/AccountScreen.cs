using Game.Core.Combat;
using Godot;
using MmoRpgClient.Data;
using MmoRpgClient.Hud;
using MmoRpgClient.Match;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Account;

/// <summary>
/// Tela inicial (T-220): login/cadastro na <c>Game.Api</c>, lista de personagens, criação (nome + classe, com descrição
/// e prévia do sprite) e exclusão com confirmação. "Jogar" pede o token de jogo e conecta no servidor com ele.
/// O token de sessão fica só em memória (<see cref="ApiClient"/>); senha e tokens nunca vão para disco. Com "Lembrar
/// e-mail" marcado, só o e-mail vai para <c>user://settings.cfg</c> (<see cref="LoginMemory"/>, T-381).
/// Os endereços da API e do servidor do jogo não aparecem na tela: vêm do padrão ou dos argumentos. O jogo é só online; o menu
/// de encontros locais (T-024) só abre pelos argumentos de partida direta. Argumentos de depuração (depois de <c>--</c>):
/// <c>--api=url</c>, <c>--game-server=host:porta</c>, <c>--login=email:senha</c> (entra sozinho), <c>--register</c>
/// (cria a conta antes), <c>--create=Nome:class.id</c>, <c>--play=Nome</c>, <c>--account-view=create</c> e
/// <c>--capture=caminho.png</c> (salva a tela após 2,5 s e fecha) e <c>--account-sample[=n]</c> (seleção de personagem com n
/// personagens de exemplo, sem conta, T-074).
/// Os argumentos de partida direta (<c>--encounter</c>, <c>--server</c>...) seguem para o menu, como antes.
/// </summary>
public partial class AccountScreen : Control
{
    public const string ScenePath = "res://scenes/Account.tscn";
    public const string DefaultApi = "https://api.nich.click";

    private static readonly string[] DirectArgs = ["encounter", "class", "fake-authority", "demo-input", "auto-move", "hold", "ranks", "rank", "server"];
    private static readonly Color ErrorColor = new(1f, 0.5f, 0.4f);
    private static readonly Color InfoColor = new(0.7f, 1f, 0.7f);

    // Sobrevivem à troca de cena (jogar → voltar), só em memória.
    private static ApiClient? s_api;
    private static string? s_gameServer;
    private static string? s_email;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private const string BackgroundPath = "res://assets/art/ui/login_background.png";
    private const string LogoPath = "res://assets/art/ui/logo.png";
    private const int LogoScale = 2;
    private bool _hasLogo;

    private readonly VBoxContainer _root = new() { CustomMinimumSize = new Godot.Vector2(420, 0) };
    private readonly Label _status = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private ContentCatalog _content = ContentCatalog.Empty;
    private SpriteCatalog _sprites = SpriteCatalog.Empty;
    private IReadOnlyList<CharacterSummary> _characters = [];
    private readonly List<CharacterCard> _cards = [];
    private int _selected = -1;
    private bool _busy;
    private double _elapsed;

    // Contador de jogadores online (só na tela de login), abaixo do painel; consulta GET /status a cada 30 s.
    private readonly Label _online = new() { HorizontalAlignment = HorizontalAlignment.Center, Visible = false, MouseFilter = MouseFilterEnum.Ignore };
    private ApiClient? _statusApi;
    private string _onlineApi = "";
    private int? _onlineCount;
    private double _onlineAge;
    private bool _onlineBusy;

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
        AddBackground();
        _root.AddThemeConstantOverride("separation", 10);
        var frame = new PanelContainer();
        frame.AddThemeStyleboxOverride("panel", HudTheme.StonePanel());
        frame.AddChild(_root);
        // Entre o castelo (à esquerda) e o grupo (à direita) da imagem de fundo; o logo (T-073) fica acima do painel.
        var column = new VBoxContainer { AnchorLeft = 0.4f, AnchorRight = 0.4f, GrowHorizontal = GrowDirection.Both, OffsetTop = 24 };
        column.AddThemeConstantOverride("separation", 12);
        _hasLogo = AddLogo(column);
        column.AddChild(frame);
        StyleOnlineCounter();
        column.AddChild(_online);
        AddChild(column);
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

        if (s_api is { LoggedIn: true } || _args.ContainsKey("account-sample"))
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
        _onlineAge += delta;
        if (_online.Visible && _onlineAge >= OnlineCounterText.RefreshSeconds)
        {
            Forget(RefreshOnline());
        }

        if (_args.TryGetValue("capture", out var path) && _elapsed > 2.5)
        {
            _args.Remove("capture");
            GetViewport().GetTexture().GetImage().SavePng(path);
            GetTree().Quit();
        }
    }

    /// <summary>
    /// Fundo da tela (T-088): a cena do PixelLab cobre a janela sem distorcer, com pixels nítidos; sem o arquivo, fundo liso.
    /// </summary>
    private void AddBackground()
    {
        AddChild(new ColorRect { Color = new Color(0.08f, 0.09f, 0.11f), AnchorRight = 1, AnchorBottom = 1 });
        if (!ResourceLoader.Exists(BackgroundPath))
        {
            return;
        }

        AddChild(new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(BackgroundPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter = TextureFilterEnum.Nearest,
            AnchorRight = 1,
            AnchorBottom = 1,
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    /// <summary>
    /// Logo do jogo (T-073) acima do painel, centralizado, em 2× exato e pixels nítidos. Sem o arquivo, a tela segue
    /// com o nome em texto no painel, como antes.
    /// </summary>
    private static bool AddLogo(Container parent)
    {
        if (!ResourceLoader.Exists(LogoPath))
        {
            return false;
        }

        var texture = ResourceLoader.Load<Texture2D>(LogoPath);
        parent.AddChild(new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = texture.GetSize() * LogoScale,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        return true;
    }

    // ---------------- telas ----------------

    private void ShowLogin()
    {
        Clear("Entrar");
        var api = s_api?.BaseUrl ?? _args.GetValueOrDefault("api", DefaultApi);
        var game = s_gameServer ?? _args.GetValueOrDefault("game-server", GameServerFrom(api));
        var memory = LoginMemory.FromSettingsFile();
        var known = s_email ?? memory.Email;
        var email = Field("E-mail", known ?? "");
        var password = Field("Senha", "");
        password.Secret = true;
        var remember = RememberBox(memory.Remember);
        ShowOnlineCounter(api);
        password.TextSubmitted += _ => Forget(Login(api, game, email.Text, password.Text, register: false, remember.ButtonPressed));

        var buttons = Row();
        buttons.AddChild(ButtonFor("Entrar", () => Forget(Login(api, game, email.Text, password.Text, register: false, remember.ButtonPressed)), 140));
        buttons.AddChild(ButtonFor("Criar conta", () => Forget(Login(api, game, email.Text, password.Text, register: true, remember.ButtonPressed)), 140));
        _root.AddChild(Hint("Criar conta usa o e-mail e a senha acima (senha de 8 a 128 caracteres) e já entra."));
        AddStatus();
        (string.IsNullOrEmpty(known) ? email : password).GrabFocus();
    }

    /// <summary>T-381: "Lembrar e-mail", logo abaixo dos campos. A escolha só é gravada ao entrar com sucesso.</summary>
    private CheckBox RememberBox(bool pressed)
    {
        var box = new CheckBox
        {
            Text = "Lembrar e-mail",
            ButtonPressed = pressed,
            FocusMode = FocusModeEnum.Click,
            TooltipText = "Guarda só o e-mail neste computador. A senha nunca é guardada.",
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin,
        };
        box.AddThemeFontSizeOverride("font_size", 16);
        box.AddThemeColorOverride("font_color", HudTheme.StoneText);
        box.AddThemeColorOverride("font_hover_color", Colors.White);
        box.AddThemeColorOverride("font_pressed_color", HudTheme.StoneText);
        box.AddThemeColorOverride("font_hover_pressed_color", Colors.White);
        _root.AddChild(box);
        return box;
    }

    /// <summary>
    /// Seleção de personagem (T-074): um cartão por personagem, lado a lado, e o cartão de criar no fim. Clique seleciona,
    /// clique duplo ou Enter joga, setas trocam. Com <c>--account-sample[=n]</c> e sem conta, mostra n personagens de exemplo
    /// (padrão 3, até 5) para prévias e capturas.
    /// </summary>
    private async Task ShowCharacters()
    {
        Clear("Personagens");
        _root.AddChild(Hint(s_api is null ? "Prévia, sem conta" : $"Conta: {s_api.Email}"));
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 10);
        _root.AddChild(row);
        var buttons = Row();
        var play = ButtonFor("Jogar", () => Forget(PlaySelected()), 140);
        var delete = ButtonFor("Apagar", ConfirmDelete, 120);
        play.Disabled = delete.Disabled = true;
        buttons.AddChild(play);
        buttons.AddChild(delete);
        buttons.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        buttons.AddChild(ButtonFor("Sair da conta", Logout, 140));
        AddStatus();

        await Run(async () =>
        {
            _characters = s_api is null ? SampleCharacters() : await s_api.Characters();
            FillCards(row, play, delete);
        });
    }

    private void FillCards(HBoxContainer row, Button play, Button delete)
    {
        _cards.Clear();
        for (var i = 0; i < _characters.Count; i++)
        {
            var c = _characters[i];
            var index = i;
            var card = CharacterCard.ForCharacter(_sprites.For(c.ClassId), c.Name, ClassName(c.ClassId), c.Level, ZoneName(c.ZoneId));
            card.Chosen += () => Select(index);
            card.Activated += () =>
            {
                Select(index);
                Forget(PlaySelected());
            };
            row.AddChild(card);
            _cards.Add(card);
        }

        var create = CharacterCard.ForCreate();
        create.Chosen += ShowCreate;
        row.AddChild(create);
        play.Disabled = delete.Disabled = _characters.Count == 0;
        if (_characters.Count > 0)
        {
            Select(0);
        }
        else
        {
            SetStatus("Nenhum personagem ainda. Crie o primeiro.", InfoColor);
        }
    }

    private void Select(int index)
    {
        _selected = Math.Clamp(index, 0, Math.Max(0, _characters.Count - 1));
        for (var i = 0; i < _cards.Count; i++)
        {
            _cards[i].SetSelected(i == _selected);
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (_cards.Count == 0 || !_cards[0].IsInsideTree() || _busy)
        {
            return;
        }

        if (@event.IsActionPressed("ui_left"))
        {
            Select(_selected - 1);
        }
        else if (@event.IsActionPressed("ui_right"))
        {
            Select(_selected + 1);
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            Forget(PlaySelected());
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    /// <summary>Personagens de exemplo de <c>--account-sample</c> (T-074), um de cada classe na ordem do menu.</summary>
    private List<CharacterSummary> SampleCharacters()
    {
        var count = int.TryParse(_args.GetValueOrDefault("account-sample"), out var n) ? Math.Clamp(n, 0, 5) : 3;
        var classes = _content.Classes.Keys.OrderBy(ClassOrder).ToList();
        var zones = _content.Zones.Keys.Where(z => !z.Contains("test", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();
        string[] names = ["Aldric", "Mira", "Teodoro", "Brann", "Sela"];
        int[] levels = [12, 4, 1, 7, 20];
        return Enumerable.Range(0, count)
            .Select(i => new CharacterSummary(Guid.Empty, names[i], classes[i % classes.Count], levels[i], zones.Count == 0 ? "" : zones[i % zones.Count]))
            .ToList();
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

    // ---------------- contador de online ----------------

    /// <summary>Texto claro com contorno escuro, legível sobre a imagem de fundo.</summary>
    private void StyleOnlineCounter()
    {
        _online.AddThemeFontSizeOverride("font_size", 16);
        _online.AddThemeConstantOverride("outline_size", 6);
        _online.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.85f));
        PaintOnlineCounter();
    }

    /// <summary>Mostra o contador da API <paramref name="apiUrl"/> e já consulta (o último número aparece enquanto isso).</summary>
    private void ShowOnlineCounter(string apiUrl)
    {
        if (_onlineApi != apiUrl)
        {
            _onlineApi = apiUrl;
            _statusApi = null;
            _onlineCount = null;
        }

        _online.Visible = true;
        PaintOnlineCounter();
        Forget(RefreshOnline());
    }

    private async Task RefreshOnline()
    {
        if (_onlineBusy)
        {
            return;
        }

        _onlineBusy = true;
        _onlineAge = 0;
        int count;
        try
        {
            _statusApi ??= s_api?.BaseUrl == _onlineApi.Trim().TrimEnd('/') ? s_api : new ApiClient(_onlineApi.Trim());
            count = await _statusApi.OnlinePlayers();
        }
        catch (Exception ex) when (ex is ApiException or UriFormatException or System.Text.Json.JsonException)
        {
            count = OnlineCounterText.Unavailable;
        }
        finally
        {
            _onlineBusy = false;
        }

        if (IsInstanceValid(_online))
        {
            _onlineCount = count;
            PaintOnlineCounter();
        }
    }

    private void PaintOnlineCounter()
    {
        var live = OnlineCounterText.IsLive(_onlineCount);
        _online.Text = (live ? "● " : "") + OnlineCounterText.Describe(_onlineCount);
        _online.AddThemeColorOverride("font_color", live ? new Color(0.55f, 0.95f, 0.55f) : new Color(HudTheme.StoneText, 0.75f));
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

    /// <summary>
    /// Entra (ou cria a conta e entra). <paramref name="remember"/> é a caixa "Lembrar e-mail" (T-381): null = entrada pelo
    /// argumento <c>--login</c>, que não grava nada.
    /// </summary>
    private async Task Login(string apiUrl, string gameServer, string email, string password, bool register, bool? remember = null)
    {
        if (!Uri.TryCreate(apiUrl.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            SetStatus("Servidor de contas mal configurado. Avise quem mantém o jogo.", ErrorColor);
            return;
        }

        if (OnlineSettings.Parse(gameServer, "") is null)
        {
            SetStatus("Servidor do jogo mal configurado. Avise quem mantém o jogo.", ErrorColor);
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
            if (remember is { } keep)
            {
                LoginMemory.FromSettingsFile().AfterLogin(keep, email);
            }

            await ShowCharacters();
        }
    }

    private async Task Create(string name, string classId)
    {
        if (HasAccount() && await Run(() => s_api!.Create(name.Trim(), classId), "Criando..."))
        {
            await ShowCharacters();
            SetStatus($"{name.Trim()} criado.", InfoColor);
        }
    }

    private void ConfirmDelete()
    {
        if (Selected() is not { } character || !HasAccount())
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

    private async Task PlaySelected()
    {
        if (Selected() is { } character && HasAccount())
        {
            await Play(character);
        }
    }

    /// <summary>Na prévia de <c>--account-sample</c> não há conta: as ações que falam com a API só avisam.</summary>
    private bool HasAccount()
    {
        if (s_api is not null)
        {
            return true;
        }

        SetStatus("Prévia sem conta: entre para jogar, criar ou apagar.", InfoColor);
        return false;
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
            GD.PushError($"Endereço do servidor do jogo inválido: '{address}'.");
            SetStatus("Servidor do jogo mal configurado. Avise quem mantém o jogo.", ErrorColor);
            return;
        }

        var encounter = MenuScreen.OrderedEncounterIds(_content).FirstOrDefault() ?? "";
        MatchSettings.Current = new MatchSettings(encounter, character.ClassId, MatchSettings.RandomSeed(),
            online with { Ticket = ticket.Ticket, CharacterId = character.Id.ToString() });
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
        _online.Visible = false;
        foreach (var child in _root.GetChildren())
        {
            _root.RemoveChild(child);
            if (child != _status)
            {
                child.QueueFree(); // o rótulo de mensagens é reaproveitado entre as telas
            }
        }

        if (!_hasLogo)
        {
            var game = new Label { Text = "Valdran", Modulate = new Color(1, 1, 1, 0.7f) };
            game.AddThemeFontSizeOverride("font_size", 18);
            _root.AddChild(game);
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
    };

    private CharacterSummary? Selected() => _selected >= 0 && _selected < _characters.Count ? _characters[_selected] : null;

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
