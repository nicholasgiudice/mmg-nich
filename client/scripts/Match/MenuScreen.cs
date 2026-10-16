using System.Globalization;
using Game.Core;
using Game.Core.Combat;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Data;

namespace MmoRpgClient.Match;

/// <summary>
/// Tela inicial: lista os encontros de <c>data/</c>, escolhe a classe do jogador e a seed (vazia = aleatória).
/// Com argumentos de depuração na linha de comando (<c>--encounter</c>, <c>--fake-authority</c>...), pula direto para o jogo.
/// </summary>
public partial class MenuScreen : Control
{
    public const string ScenePath = "res://scenes/Menu.tscn";

    private static readonly string[] DirectArgs = ["encounter", "class", "fake-authority", "demo-input", "auto-move"];

    private readonly ItemList _encounters = new();
    private readonly OptionButton _classes = new();
    private readonly LineEdit _seed = new();
    private readonly Label _details = new();
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

        _classes.ItemSelected += _ => UpdateDetails();
        root.AddChild(_classes);

        root.AddChild(Caption("Seed (vazio = aleatória)"));
        _seed.PlaceholderText = "ex.: 12345";
        _seed.CustomMinimumSize = new Vector2(200, 0);
        root.AddChild(_seed);

        var play = new Button { Text = "Jogar", CustomMinimumSize = new Vector2(160, 40) };
        play.Pressed += StartGame;
        root.AddChild(play);

        root.AddChild(new Label
        {
            Text = "Em jogo: WASD mover · clique atacar · 1-4 habilidades · Q poção · Tab alvo · R reiniciar · Esc pausa",
            Modulate = new Color(1, 1, 1, 0.6f),
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
            MatchSettings.Current = new MatchSettings(id, cls, seed);
        }

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
