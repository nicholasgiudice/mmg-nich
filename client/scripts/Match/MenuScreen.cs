using System.Globalization;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Data;

namespace MmoRpgClient.Match;

/// <summary>
/// Modo de teste (aberto pela tela de conta, T-220): lista os encontros de <c>data/</c>, escolhe a classe do jogador e a seed (vazia = aleatória).
/// Com argumentos de depuração na linha de comando (<c>--encounter</c>, <c>--fake-authority</c>...), pula direto para o jogo.
/// </summary>
public partial class MenuScreen : Control
{
    public const string ScenePath = "res://scenes/Menu.tscn";

    private static readonly string[] DirectArgs = ["encounter", "class", "fake-authority", "demo-input", "auto-move", "hold", "ranks", "rank", "server"];

    /// <summary>Erro da última partida online (recusa, queda), mostrado no menu (T-107).</summary>
    public static string? LastError { get; set; }

    private readonly ItemList _encounters = new();
    private readonly OptionButton _classes = new();
    private readonly LineEdit _seed = new();
    private readonly Label _details = new();
    private readonly VBoxContainer _skills = new();
    private readonly Label _points = new();
    private readonly LineEdit _server = new() { Text = "127.0.0.1:7777", CustomMinimumSize = new Vector2(200, 0) };
    private readonly LineEdit _name = new() { Text = "jogador", CustomMinimumSize = new Vector2(140, 0) };
    private readonly List<string> _encounterIds = new();
    private readonly List<string> _classIds = new();
    private ContentCatalog _content = ContentCatalog.Empty;
    private double _elapsed;

    public override void _Process(double delta)
    {
        // --capture sem encontro: fotografa o menu (verificação sem interação).
        _elapsed += delta;
        var capture = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--capture=", StringComparison.Ordinal));
        if (capture is not null && _elapsed > 1.0)
        {
            GetViewport().GetTexture().GetImage().SavePng(capture["--capture=".Length..]);
            GetTree().Quit();
        }
    }

    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Any(a => DirectArgs.Any(d => a.TrimStart('-').StartsWith(d, StringComparison.Ordinal)))
            && MatchSettings.Current is null)
        {
            CallDeferred(MethodName.StartGame);
            return;
        }

        SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(new ColorRect { Color = new Color(0.08f, 0.09f, 0.11f), AnchorRight = 1, AnchorBottom = 1 });

        try
        {
            _content = ContentLoader.Load();
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            AddChild(new Label { Text = $"Erro ao carregar dados: {ex.Message}", Position = new Vector2(24, 24) });
            return;
        }

        var root = new VBoxContainer { Position = new Vector2(48, 36), CustomMinimumSize = new Vector2(620, 0) };
        root.AddThemeConstantOverride("separation", 10);
        AddChild(root);

        var title = new Label { Text = "Protótipo de combate" };
        title.AddThemeFontSizeOverride("font_size", 30);
        root.AddChild(title);
        root.AddChild(new Label { Text = $"Core {CoreInfo.Version} · escolha um encontro e uma classe", Modulate = new Color(1, 1, 1, 0.7f) });

        root.AddChild(Caption("Encontro"));
        _encounters.CustomMinimumSize = new Vector2(620, 170);
        _encounters.ItemSelected += _ => OnEncounterSelected();
        _encounters.ItemActivated += _ => StartGame();
        foreach (var enc in OrderedEncounterIds(_content).Select(id => _content.Encounters[id]))
        {
            var suggested = ClassName(MatchSetup.SuggestedClass(enc));
            var who = enc.Players.Count > 1 ? $"grupo de {enc.Players.Count}" : suggested;
            _encounters.AddItem($"{enc.Name}  —  {who}");
            _encounterIds.Add(enc.Id);
        }

        root.AddChild(_encounters);
        _details.Modulate = new Color(1, 1, 1, 0.75f);
        _details.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _details.CustomMinimumSize = new Vector2(620, 0);
        root.AddChild(_details);

        root.AddChild(Caption("Sua classe"));
        foreach (var cls in _content.Classes.Values.OrderBy(c => ClassOrder(c.Id)))
        {
            _classes.AddItem(cls.Name);
            _classIds.Add(cls.Id);
        }

        _classes.ItemSelected += _ =>
        {
            UpdateDetails();
            BuildSkills();
        };
        root.AddChild(_classes);

        root.AddChild(Caption("Seed (vazio = aleatória)"));
        _seed.PlaceholderText = "ex.: 12345";
        _seed.CustomMinimumSize = new Vector2(200, 0);
        root.AddChild(_seed);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        var play = new Button { Text = "Jogar", CustomMinimumSize = new Vector2(160, 40) };
        play.Pressed += StartGame;
        buttons.AddChild(play);
        var account = new Button { Text = "Entrar com conta", CustomMinimumSize = new Vector2(160, 40) };
        account.Pressed += () => GetTree().ChangeSceneToFile(Account.AccountScreen.ScenePath);
        buttons.AddChild(account);
        root.AddChild(buttons);

        // T-107: jogar no servidor. A sala do servidor decide o encontro; a classe escolhida acima vai junto.
        root.AddChild(Caption("Jogar online (servidor e seu nome)"));
        var online = new HBoxContainer();
        online.AddThemeConstantOverride("separation", 8);
        if (MatchSettings.Current?.Online is { } lastOnline)
        {
            _server.Text = $"{lastOnline.Host}:{lastOnline.Port}";
            _name.Text = lastOnline.Name;
        }

        online.AddChild(_server);
        online.AddChild(_name);
        var playOnline = new Button { Text = "Jogar online", CustomMinimumSize = new Vector2(140, 0) };
        playOnline.Pressed += StartOnline;
        online.AddChild(playOnline);
        root.AddChild(online);
        if (LastError is { } error)
        {
            var label = new Label { Text = error, AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(620, 0) };
            label.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.4f));
            root.AddChild(label);
            LastError = null;
        }

        // T-064: níveis de habilidade da classe (modo de teste: a regra dos pontos só é mostrada).
        var side = new VBoxContainer { Position = new Vector2(720, 36), CustomMinimumSize = new Vector2(500, 0) };
        side.AddThemeConstantOverride("separation", 8);
        AddChild(side);
        side.AddChild(Caption("Níveis de habilidade (NH)"));
        var profiles = new HBoxContainer();
        profiles.AddThemeConstantOverride("separation", 6);
        foreach (var (label, profile) in new[]
        {
            ("NH1", SkillPlan.Profile.Nh1), ("Focado (5+5)", SkillPlan.Profile.Focused), ("Espalhado (3+3+3+3)", SkillPlan.Profile.Spread),
        })
        {
            var button = new Button { Text = label };
            button.Pressed += () =>
            {
                CurrentPlan()?.ApplyProfile(profile);
                BuildSkills();
            };
            profiles.AddChild(button);
        }

        side.AddChild(profiles);
        _skills.AddThemeConstantOverride("separation", 4);
        side.AddChild(_skills);
        side.AddChild(_points);
        side.AddChild(new Label
        {
            Text = "Modo de teste: qualquer combinação vale. No jogo completo cada nível do personagem dá 1 ponto (15 no nível 15), "
                + "o NH n exige o nível 2n − 1 e a assinatura (0–3) exige os níveis 12, 13 e 15. Os bots aliados ficam no NH1.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(500, 0),
            Modulate = new Color(1, 1, 1, 0.6f),
        });

        root.AddChild(new Label
        {
            Text = "Em jogo: WASD mover · Tab ou clique seleciona o alvo e o ataque básico é automático · 1-5 habilidades · Q poção · Esc limpa alvo/pausa · R reiniciar",
            Modulate = new Color(1, 1, 1, 0.6f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(620, 0), // não estica a coluna por cima do painel de NH
        });

        // Volta selecionando o último encontro jogado.
        var last = MatchSettings.Current is { } current ? _encounterIds.IndexOf(current.EncounterId) : 0;
        _encounters.Select(Math.Max(0, last));
        OnEncounterSelected();
        if (MatchSettings.Current?.ClassId is { } lastClass && _classIds.IndexOf(lastClass) is >= 0 and var ci)
        {
            _classes.Select(ci);
            UpdateDetails();
        }

        BuildSkills();
    }

    private SkillPlan? CurrentPlan() =>
        _classes.Selected >= 0 && _content.Classes.TryGetValue(_classIds[_classes.Selected], out var cls)
            ? SkillPlan.For(cls, _content)
            : null;

    /// <summary>Uma linha por habilidade da classe: nome, seletor de NH e resumo do NH escolhido.</summary>
    private void BuildSkills()
    {
        foreach (var child in _skills.GetChildren())
        {
            child.QueueFree();
        }

        var plan = CurrentPlan();
        if (plan is null)
        {
            return;
        }

        foreach (var ability in plan.Abilities)
        {
            var row = new VBoxContainer();
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            line.AddChild(new Label
            {
                Text = ability.Name + (ability.Signature ? "  (assinatura)" : ""),
                CustomMinimumSize = new Vector2(260, 0),
            });
            var spin = new SpinBox
            {
                MinValue = SkillPlan.MinRank(ability),
                MaxValue = SkillPlan.MaxRank(ability),
                Step = 1,
                Value = plan.RankOf(ability.Id),
                CustomMinimumSize = new Vector2(80, 0),
            };
            var summary = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(500, 0),
                Modulate = new Color(1, 1, 1, 0.65f),
            };
            summary.AddThemeFontSizeOverride("font_size", 12);
            spin.ValueChanged += value =>
            {
                plan.Set(ability.Id, (int)value);
                UpdateSkillSummary(summary, ability.Id, plan);
                UpdatePoints(plan);
            };
            line.AddChild(spin);
            row.AddChild(line);
            row.AddChild(summary);
            UpdateSkillSummary(summary, ability.Id, plan);
            _skills.AddChild(row);
        }

        UpdatePoints(plan);
    }

    private void UpdateSkillSummary(Label summary, string abilityId, SkillPlan plan)
    {
        var rank = plan.RankOf(abilityId);
        summary.Text = rank <= 0 || _content.AbilityAt(abilityId, rank) is not { } def
            ? "Fora da barra (NH 0)."
            : $"NH {SkillPlan.Roman(rank)}: " + Hud.AbilityText.Describe(def, _content).Replace("\n", " · ", StringComparison.Ordinal);
    }

    private void UpdatePoints(SkillPlan plan)
    {
        _points.Text = $"Pontos usados: {plan.PointsUsed} de {plan.PointBudget} (nível 15)"
            + (plan.OverBudget ? " — acima do permitido no jogo completo" : "");
        _points.AddThemeColorOverride("font_color", plan.OverBudget ? new Color(1f, 0.55f, 0.4f) : new Color(0.7f, 1f, 0.7f));
    }

    private void OnEncounterSelected()
    {
        var id = SelectedEncounter();
        if (id is not null && _content.Encounters.TryGetValue(id, out var enc))
        {
            var index = _classIds.IndexOf(MatchSetup.SuggestedClass(enc));
            if (index >= 0)
            {
                _classes.Select(index);
            }
        }

        UpdateDetails();
    }

    private void UpdateDetails()
    {
        var id = SelectedEncounter();
        if (id is null || !_content.Encounters.TryGetValue(id, out var enc))
        {
            _details.Text = "";
            return;
        }

        var enemies = string.Join(", ", enc.Enemies
            .GroupBy(e => e.Monster)
            .Select(g => (_content.Monsters.TryGetValue(g.Key, out var m) ? m.Name : g.Key) + (g.Count() > 1 ? $" ×{g.Count()}" : "")));
        var mine = _classIds.Count > 0 && _classes.Selected >= 0 ? _classIds[_classes.Selected] : null;
        var bots = enc.Players.Select(p => p.Class).ToList();
        bots.RemoveAt(Math.Max(0, mine is null ? 0 : bots.IndexOf(mine))); // a vaga do jogador (mesma regra do MatchSetup)
        var team = enc.Players.Count > 1
            ? $"Grupo: você + {bots.Count} bots ({string.Join(", ", bots.Select(ClassName))})."
            : $"Solo. Classe sugerida: {ClassName(MatchSetup.SuggestedClass(enc))}.";
        var limit = enc.Defeat.TimeLimitTicks is { } t
            ? $" Limite: {(t / (double)World.TickRate).ToString("0", CultureInfo.InvariantCulture)} s."
            : "";
        _details.Text = $"Inimigos: {enemies}. {team}{limit}";
    }

    private void StartGame()
    {
        if (_content.Encounters.Count > 0)
        {
            var id = SelectedEncounter() ?? _encounterIds[0];
            var cls = _classes.Selected >= 0 ? _classIds[_classes.Selected] : null;
            var seed = ulong.TryParse(_seed.Text.Trim(), out var s) && s > 0 ? s : MatchSettings.RandomSeed();
            MatchSettings.Current = new MatchSettings(id, cls, seed); // local (sem Online)
        }

        GetTree().ChangeSceneToFile(Main.ScenePath);
    }

    private void StartOnline()
    {
        if (OnlineSettings.Parse(_server.Text, _name.Text) is not { } online)
        {
            _details.Text = "Endereço inválido: use host:porta (ex.: 127.0.0.1:7777).";
            return;
        }

        var cls = _classes.Selected >= 0 ? _classIds[_classes.Selected] : null;
        MatchSettings.Current = new MatchSettings(SelectedEncounter() ?? _encounterIds[0], cls, MatchSettings.RandomSeed(), online);
        GetTree().ChangeSceneToFile(Main.ScenePath);
    }

    private string? SelectedEncounter()
    {
        var selected = _encounters.GetSelectedItems();
        return selected.Length > 0 ? _encounterIds[selected[0]] : null;
    }

    private string ClassName(string id) => _content.Classes.TryGetValue(id, out var c) ? c.Name : id;

    private static Label Caption(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.45f));
        return label;
    }

    /// <summary>Encontros na ordem do menu (usada também por "Próximo encontro").</summary>
    public static List<string> OrderedEncounterIds(ContentCatalog content) =>
        content.Encounters.Keys.OrderBy(EncounterOrder).ThenBy(id => id, StringComparer.Ordinal).ToList();

    /// <summary>Ordem dos cinco cenários do design (classe por classe, grupo por último).</summary>
    private static int EncounterOrder(string id) => id switch
    {
        "encounter.three_wolves" => 0,
        "encounter.wolf_and_bandit" => 1,
        "encounter.four_rats" => 2,
        "encounter.skeleton_guard" => 3,
        "encounter.forest_matriarch" => 4,
        _ => 10,
    };

    private static int ClassOrder(string id) => id switch
    {
        "class.warrior" => 0,
        "class.archer" => 1,
        "class.mage" => 2,
        "class.cleric" => 3,
        _ => 10,
    };
}
