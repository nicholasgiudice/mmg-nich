using System.Reflection;
using System.Text;
using Game.Core.Combat;
using Game.Data;
using static Game.Site.Fmt;

namespace Game.Site;

/// <summary>Onde ler os dados e textos e onde escrever o site.</summary>
public sealed record SiteOptions
{
    public required string DataDir { get; init; }
    public required string OutDir { get; init; }

    /// <summary>Textos que não são regra (<c>site/content</c>). Null ou ausente = sem textos.</summary>
    public string? ContentDir { get; init; }

    /// <summary>Arte do cliente (<c>client/assets/art</c>) para as imagens. Null ou ausente = sem imagens.</summary>
    public string? ArtDir { get; init; }

    /// <summary><c>CREDITS.md</c>. Obrigatório se houver imagens (atribuição das licenças).</summary>
    public string? CreditsFile { get; init; }
}

public sealed record SiteResult(IReadOnlyList<string> Pages, IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors)
{
    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Gerador do site (T-071): carrega <c>data/</c> pelo <see cref="GameDataLoader"/> (mesma validação e herança de NH do
/// jogo) e escreve HTML estático. Nenhum valor de jogo vive aqui; só rótulos e a forma de mostrar.
/// </summary>
public static class SiteGenerator
{
    public static SiteResult Generate(SiteOptions options)
    {
        var load = GameDataLoader.LoadDirectory(options.DataDir);
        if (!load.Success)
        {
            return new SiteResult([], [], load.Errors.Select(e => e.ToString()).ToList());
        }

        var warnings = new List<string>();
        var content = SiteContent.Load(options.ContentDir);
        var sprites = Sprites.Load(options.ArtDir, warnings);
        var icons = Icons.Load(options.ArtDir, warnings);
        string? credits = null;
        if (options.CreditsFile is { } cf && File.Exists(cf))
        {
            credits = File.ReadAllText(cf);
        }
        else if (sprites.Count > 0 || icons.Count > 0)
        {
            return new SiteResult([], warnings, ["o site usa sprites e ícones e precisa de CREDITS.md (atribuição das licenças CC-BY, CC-BY-SA, OGA-BY e CC0)"]);
        }

        var builder = new SiteBuilder(load.Database!, content, sprites, icons, credits, warnings);
        builder.Build();

        if (Directory.Exists(options.OutDir))
        {
            Directory.Delete(options.OutDir, recursive: true);
        }

        Directory.CreateDirectory(options.OutDir);
        foreach (var page in builder.Pages)
        {
            var path = Path.Combine(options.OutDir, page.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, page.Render(content.Title), new UTF8Encoding(false));
        }

        var assets = Path.Combine(options.OutDir, "assets");
        Directory.CreateDirectory(assets);
        foreach (var name in new[] { "style.css", "search.js" })
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"recurso {name} não embutido");
            using var file = File.Create(Path.Combine(assets, name));
            stream.CopyTo(file);
        }

        if (options.ArtDir is { } art)
        {
            Sprites.Copy(art, options.OutDir, sprites.Values);
            Icons.Copy(art, options.OutDir, icons.Values);
        }

        return new SiteResult(builder.Pages.Select(p => p.Path).ToList(), warnings, []);
    }
}

/// <summary>Monta as páginas em memória (uma por classe, habilidade e monstro; referências por seção).</summary>
internal sealed partial class SiteBuilder
{
    private readonly GameDatabase _db;
    private readonly ContentCatalog _catalog;
    private readonly SiteContent _content;
    private readonly IReadOnlyDictionary<string, SpriteFrame> _sprites;
    private readonly IReadOnlyDictionary<string, SpriteFrame> _icons;
    private readonly string? _credits;
    private readonly List<string> _warnings;
    private readonly Usage _usage;
    private readonly Describe _d;

    public List<Page> Pages { get; } = [];

    public SiteBuilder(GameDatabase db, SiteContent content, IReadOnlyDictionary<string, SpriteFrame> sprites,
        IReadOnlyDictionary<string, SpriteFrame> icons, string? credits, List<string> warnings)
    {
        _db = db;
        _content = content;
        _sprites = sprites;
        _icons = icons;
        _credits = credits;
        _warnings = warnings;
        _usage = new Usage(db);
        _d = new Describe(db, _usage);
        _catalog = new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Zones = db.Zones,
        };
    }

    public void Build()
    {
        foreach (var a in _usage.UnusedAbilities)
        {
            _warnings.Add($"habilidade {a.Id} ({a.Name}) não é usada por nenhuma classe nem monstro: fica só em \"Em aberto\"");
        }

        foreach (var s in _usage.UnusedStatuses)
        {
            _warnings.Add($"status {s.Id} ({s.Name}) não é aplicado por nada usado no jogo: fica só em \"Em aberto\"");
        }

        foreach (var a in _usage.UnusedAreas)
        {
            _warnings.Add($"área {a.Id} não é criada por nada usado no jogo: fica só em \"Em aberto\"");
        }

        foreach (var c in _db.Classes.Keys.Where(k => !_content.Classes.ContainsKey(k)))
        {
            _warnings.Add($"classe {c} sem texto em site/content/site.json");
        }

        foreach (var m in _db.Monsters.Keys.Where(k => !_content.Monsters.ContainsKey(k)))
        {
            _warnings.Add($"monstro {m} sem descrição em site/content/site.json");
        }

        BuildHome();
        BuildClasses();
        BuildAbilities();
        BuildMonsters();
        BuildItems();
        BuildStatuses();
        BuildAreas();
        BuildOpen();
        BuildCredits();
        BuildSearch();
    }

    private Page NewPage(string path, string title, string description)
    {
        var page = new Page(path, title, description);
        Pages.Add(page);
        return page;
    }

    private string Sprite(Page p, string id, string alt, int scale = 2) =>
        _sprites.TryGetValue(id, out var f) ? Sprites.Html(p, f, alt, scale) : "";

    /// <summary>Ícone decorativo (T-080) de uma habilidade, item, atributo ou <c>ui.*</c>; vazio se o id não tem ícone.</summary>
    private string Icon(Page p, string id, int scale = 1) =>
        _icons.TryGetValue(id, out var f) ? Sprites.Html(p, f, null, scale, "icon") : "";

    /// <summary>Ícones e siglas dos cinco atributos, na ordem do jogo.</summary>
    private string Attributes(Page p, Core.Definitions.AttributeSet? a = null) =>
        string.Join(" · ", new (string Id, string Label, int? Value)[]
        {
            ("attribute.str", "FOR", a?.Str), ("attribute.dex", "DES", a?.Dex), ("attribute.agi", "AGI", a?.Agi),
            ("attribute.int", "INT", a?.Int), ("attribute.vit", "VIT", a?.Vit),
        }.Select(x => $"<span class=\"attr\">{Icon(p, x.Id)}{x.Label}{(x.Value is { } v ? $" {v}" : "")}</span>"));

    private IEnumerable<Core.Definitions.ClassDefinition> ClassesInOrder => _db.Classes.Values;

    private static string Dl(IEnumerable<(string Term, string Value)> rows) =>
        "<dl class=\"stats\">" + string.Concat(rows.Select(r => $"<dt>{r.Term}</dt><dd>{r.Value}</dd>")) + "</dl>";

    private static string Table(string[] head, IEnumerable<string[]> rows, string? cls = null) =>
        $"<div class=\"table-wrap\"><table{(cls is null ? "" : $" class=\"{cls}\"")}><thead><tr>"
        + string.Concat(head.Select(h => $"<th>{h}</th>"))
        + "</tr></thead><tbody>"
        + string.Concat(rows.Select(r => "<tr>" + string.Concat(r.Select(c => $"<td>{c}</td>")) + "</tr>"))
        + "</tbody></table></div>";

    private void BuildHome()
    {
        var p = NewPage(Url.Home, _content.Title, _content.Tagline.Length > 0 ? _content.Tagline : "Informações do jogo.");
        p.Add($"<section class=\"hero\"><h1>{Esc(_content.Title)}</h1><p class=\"lead\">{Esc(_content.Tagline)}</p></section>");
        foreach (var para in _content.Intro)
        {
            p.Add($"<p>{Esc(para)}</p>");
        }

        p.Add("<h2>Classes</h2><div class=\"cards\">");
        foreach (var c in ClassesInOrder)
        {
            var text = _content.Classes.GetValueOrDefault(c.Id);
            p.Add($"<a class=\"card\" href=\"{Esc(p.Href(Url.Class(c.Id)))}\">{Sprite(p, c.Id, c.Name)}<strong>{Esc(c.Name)}</strong><span>{Esc(text?.Summary ?? "")}</span></a>");
        }

        p.Add("</div>");
        var shownAbilities = _db.Abilities.Keys.Count(_usage.IsUsed);
        p.Add("<h2>Seções</h2><ul class=\"sections\">");
        p.Add($"<li>{p.A(Url.Classes, "Classes")} — {_db.Classes.Count} classes, atributos, equipamento e habilidades.</li>");
        p.Add($"<li>{p.A(Url.Abilities, "Habilidades")} — {shownAbilities} habilidades de personagens e monstros, por nível de habilidade.</li>");
        p.Add($"<li>{p.A(Url.Monsters, "Monstros")} — {_db.Monsters.Count} monstros, onde aparecem e o que deixam cair.</li>");
        p.Add($"<li>{p.A(Url.Items, "Itens")} — {_db.Items.Count} itens base, raridades, afixos e quedas.</li>");
        p.Add($"<li>{p.A(Url.Statuses, "Status")} e {p.A(Url.Areas, "áreas")} — referência dos efeitos.</li>");
        p.Add($"<li>{p.A(Url.Search, "Busca")} — tudo numa lista só.</li>");
        p.Add("</ul>");
    }

    private void BuildOpen()
    {
        var p = NewPage(Url.Open, "Em aberto", "Conteúdo que existe nos dados mas não está em uso no jogo atual.");
        p.Add("<h1>Em aberto</h1>");
        p.Add("<p class=\"note\">Estes itens existem nos dados, mas <strong>nenhum personagem nem monstro os usa hoje</strong>. Eles não fazem parte do jogo atual e podem mudar ou sair.</p>");
        var abilities = _usage.UnusedAbilities.ToList();
        p.Add("<h2>Habilidades sem uso</h2>");
        p.Add(abilities.Count == 0 ? "<p>Nenhuma.</p>" : "<ul>" + string.Concat(abilities.Select(a => $"<li>{p.A(Url.Ability(a.Id), a.Name)} {Code(a.Id)}</li>")) + "</ul>");
        var statuses = _usage.UnusedStatuses.ToList();
        p.Add("<h2>Status sem uso</h2>");
        p.Add(statuses.Count == 0 ? "<p>Nenhum.</p>" : "<ul>" + string.Concat(statuses.Select(s => $"<li>{p.A(Url.Status(s.Id), _d.StatusLabel(s))} {Code(s.Id)}</li>")) + "</ul>");
        var areas = _usage.UnusedAreas.ToList();
        p.Add("<h2>Áreas sem uso</h2>");
        p.Add(areas.Count == 0 ? "<p>Nenhuma.</p>" : "<ul>" + string.Concat(areas.Select(a => $"<li>{p.A(Url.Area(a.Id), _d.AreaLabel(a.Id))} {Code(a.Id)}</li>")) + "</ul>");
    }

    private void BuildCredits()
    {
        var p = NewPage(Url.Credits, "Créditos", "Créditos e licenças da arte usada no jogo e neste site.");
        p.Add("<h1>Créditos</h1>");
        if (_credits is null)
        {
            p.Add("<p>Sem créditos de terceiros.</p>");
            return;
        }

        p.Add("<p>As imagens deste site são quadros das folhas de sprites e ícones do jogo. A atribuição abaixo vem de <code>CREDITS.md</code>.</p>");
        // O título do arquivo ("# Créditos") já é o h1 da página.
        var body = string.Join('\n', _credits.Replace("\r\n", "\n").Split('\n').SkipWhile(l => !l.StartsWith("# ", StringComparison.Ordinal)).Skip(1));
        p.Add(Markdown.ToHtml(body));
    }

    private void BuildSearch()
    {
        var p = NewPage(Url.Search, "Busca", "Busca por nome ou id em classes, habilidades, monstros, itens, status e áreas.");
        p.Add("<h1>Busca</h1>");
        p.Add("<p><label for=\"q\">Filtrar por nome ou id:</label> <input id=\"q\" type=\"search\" autocomplete=\"off\" placeholder=\"ex.: lobo, veneno, warrior\"></p>");
        var entries = new List<(string Kind, string Name, string Id, string Url)>();
        entries.AddRange(_db.Classes.Values.Select(c => ("Classe", c.Name, c.Id, Url.Class(c.Id))));
        entries.AddRange(_db.Abilities.Values.Select(a => ("Habilidade", a.Name, a.Id, Url.Ability(a.Id))));
        entries.AddRange(_db.Monsters.Values.Select(m => ("Monstro", m.Name, m.Id, Url.Monster(m.Id))));
        entries.AddRange(_db.Items.Values.Select(i => ("Item", i.Name, i.Id, Url.Item(i.Id))));
        entries.AddRange(_db.Rules.Items.Affixes.Select(a => ("Afixo", a.Name, a.Id, Url.Affix(a.Id))));
        entries.AddRange(_db.Statuses.Values.Select(s => ("Status", _d.StatusLabel(s), s.Id, Url.Status(s.Id))));
        entries.AddRange(_db.Areas.Values.Select(a => ("Área", _d.AreaLabel(a.Id), a.Id, Url.Area(a.Id))));
        entries.AddRange(_db.Zones.Values.Select(z => ("Zona", z.Name, z.Id, Url.Zone(z.Id))));
        p.Add("<ul id=\"results\" class=\"search\">");
        foreach (var e in entries.OrderBy(e => e.Name, StringComparer.CurrentCulture))
        {
            p.Add($"<li data-s=\"{Esc((e.Name + " " + e.Id).ToLowerInvariant())}\">{Icon(p, e.Id)}<a href=\"{Esc(p.Href(e.Url))}\">{Esc(e.Name)}</a> <span class=\"muted\">{e.Kind}</span> {Code(e.Id)}</li>");
        }

        p.Add("</ul>");
        p.Add($"<script src=\"{Esc(p.Href("assets/search.js"))}\"></script>");
    }
}
