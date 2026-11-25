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
        foreach (var name in new[] { "style.css", "search.js", "download.js" })
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
            UiArt.Copy(art, options.OutDir, warnings);
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
        _d = new Describe(db, _usage, content.Tags);
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
            _warnings.Add($"habilidade {a.Id} ({a.Name}) não é usada por nenhuma classe nem monstro: fica fora do site");
        }

        foreach (var s in _usage.UnusedStatuses)
        {
            _warnings.Add($"status {s.Id} ({s.Name}) não é aplicado por nada usado no jogo: fica fora do site");
        }

        foreach (var a in _usage.UnusedAreas)
        {
            _warnings.Add($"área {a.Id} não é criada por nada usado no jogo: fica fora do site");
        }

        foreach (var c in _db.Classes.Keys.Concat(_usage.SecondClasses.Keys).Where(k => !_content.Classes.ContainsKey(k)))
        {
            _warnings.Add($"classe {c} sem texto em site/content/site.json");
        }

        foreach (var m in _db.Monsters.Keys.Where(k => !_content.Monsters.ContainsKey(k)))
        {
            _warnings.Add($"monstro {m} sem descrição em site/content/site.json");
        }

        foreach (var t in UsedTags().Where(t => !_content.Tags.ContainsKey(t)))
        {
            _warnings.Add($"tag {t} sem rótulo em site/content/site.json: não aparece no site");
        }

        BuildHome();
        BuildClasses();
        BuildAbilities();
        BuildMonsters();
        BuildItems();
        BuildStatuses();
        BuildAreas();
        BuildDownload();
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

    /// <summary>Tags que o site mostra: tipos de monstro, tags de status e as citadas em efeitos.</summary>
    private IEnumerable<string> UsedTags() =>
        _db.Monsters.Values.SelectMany(m => m.Tags)
            .Concat(_db.Statuses.Values.SelectMany(s => s.Tags.Concat(s.ImmuneToTags)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    /// <summary>Zonas que aparecem no site (sem as escondidas em <c>site.json</c>).</summary>
    private IEnumerable<Core.Definitions.ZoneDefinition> VisibleZones =>
        _db.Zones.Values.Where(z => !_content.HiddenZones.Contains(z.Id));

    private string DownloadButton(Page p, string cls = "") =>
        $"<a class=\"button{(cls.Length > 0 ? " " + cls : "")}\" href=\"{Esc(p.Href(Url.Download))}\">Baixar o jogo</a>";

    private void BuildHome()
    {
        var p = NewPage(Url.Home, _content.Title, _content.Tagline.Length > 0 ? _content.Tagline : "Informações do jogo.");
        p.Hero = true;
        p.Add($"<section class=\"hero\"><div class=\"hero-text\"><h1>{Esc(_content.Title)}</h1><p class=\"lead\">{Esc(_content.Tagline)}</p>"
            + $"<p class=\"hero-actions\">{DownloadButton(p, "big")} {p.A(Url.Classes, "Conheça as classes")}</p></div></section>");
        p.Add("<section class=\"intro panel\">");
        foreach (var para in _content.Intro)
        {
            p.Add($"<p>{Esc(para)}</p>");
        }

        p.Add("</section>");
        p.Add("<h2>Classes</h2><div class=\"cards\">");
        foreach (var c in ClassesInOrder)
        {
            var text = _content.Classes.GetValueOrDefault(c.Id);
            p.Add($"<a class=\"card\" href=\"{Esc(p.Href(Url.Class(c.Id)))}\">{Sprite(p, c.Id, c.Name)}<strong>{Esc(c.Name)}</strong><span>{Esc(text?.Summary ?? "")}</span></a>");
        }

        p.Add("</div>");
        if (_usage.SecondClasses.Count > 0)
        {
            // T-075: as segundas classes numa linha, para não dobrar a página inicial.
            var links = ClassesInOrder.Where(c => c.SecondClasses.Count > 0).Select(c =>
                $"{Esc(c.Name)}: {string.Join(" ou ", c.SecondClasses.Select(sc => p.A(Url.Class(sc.Id), sc.Name)))}");
            p.Add($"<p>No nível {Num(_db.Rules.Progression.SecondClassLevel)}, cada classe escolhe uma {p.A($"{Url.Classes}#segundas-classes", "segunda classe")}. "
                + string.Join("; ", links) + ".</p>");
        }

        var shownAbilities = _db.Abilities.Keys.Count(_usage.IsUsed);
        p.Add("<h2>Explore</h2><div class=\"cards sections\">");
        void Section(string url, string title, string text) =>
            p.Add($"<a class=\"card\" href=\"{Esc(p.Href(url))}\"><strong>{Esc(title)}</strong><span>{text}</span></a>");
        Section(Url.Classes, "Classes", _usage.SecondClasses.Count > 0
            ? $"{_db.Classes.Count} classes e {_usage.SecondClasses.Count} segundas classes, atributos, equipamento e habilidades."
            : $"{_db.Classes.Count} classes, atributos, equipamento e habilidades.");
        Section(Url.Abilities, "Habilidades", $"{shownAbilities} habilidades de personagens e monstros, por nível de habilidade.");
        Section(Url.Monsters, "Monstros", $"{_db.Monsters.Count} monstros, onde aparecem e o que deixam cair.");
        Section(Url.Items, "Itens", $"{_db.Items.Count} itens base, raridades, afixos e quedas.");
        Section(Url.Statuses, "Status e áreas", "Referência dos efeitos que ficam no personagem e no chão.");
        Section(Url.Download, "Download", "Baixe o cliente para Windows e entre no mundo.");
        p.Add("</div>");
    }

    private void BuildDownload()
    {
        var d = _content.Download;
        var p = NewPage(Url.Download, "Download", $"Baixe {_content.Title} para Windows.");
        p.Add("<h1>Download</h1>");
        if (d.Lead.Length > 0)
        {
            p.Add($"<p class=\"lead\">{Esc(d.Lead)}</p>");
        }

        // O arquivo não faz parte do site: scripts/deploy-download.sh o publica em /downloads/, ao lado das versões do site.
        // download.js lê /downloads/latest.json e mostra versão, tamanho e data; sem ele o botão aponta para o nome fixo.
        p.Add("<section class=\"download panel\" id=\"download\">");
        p.Add($"<div class=\"download-info\"><h2>{Esc(_content.Title)} para Windows</h2>"
            + "<p class=\"muted\" data-dl=\"meta\">Cliente para Windows (.zip).</p>"
            + $"<p class=\"note\" data-dl=\"unavailable\" hidden>{Esc(d.Unavailable)}</p></div>");
        p.Add($"<a class=\"button big\" data-dl=\"link\" href=\"{Url.DownloadFile}\" download>Baixar para Windows</a>");
        p.Add("<p class=\"muted small\" data-dl=\"sha\" hidden></p>");
        p.Add("</section>");
        if (d.Requirements.Count > 0)
        {
            p.Add("<h2>Requisitos</h2><ul>" + string.Concat(d.Requirements.Select(r => $"<li>{Esc(r)}</li>")) + "</ul>");
        }

        if (d.Steps.Count > 0)
        {
            p.Add("<h2>Como instalar</h2><ol class=\"steps\">" + string.Concat(d.Steps.Select(st => $"<li>{Esc(st)}</li>")) + "</ol>");
        }

        p.Add($"<script src=\"{Esc(p.Href("assets/download.js"))}\"></script>");
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

        p.Add("<p>As imagens deste site vêm da arte do jogo. Abaixo, autores e licenças de cada uma.</p>");
        // O título do arquivo ("# Créditos") já é o h1 da página.
        var body = string.Join('\n', _credits.Replace("\r\n", "\n").Split('\n').SkipWhile(l => !l.StartsWith("# ", StringComparison.Ordinal)).Skip(1));
        p.Add(Markdown.ToHtml(body));
    }

    private void BuildSearch()
    {
        var p = NewPage(Url.Search, "Busca", "Busca por nome em classes, habilidades, monstros, itens, status, áreas e zonas.");
        p.Add("<h1>Busca</h1>");
        p.Add("<p><label for=\"q\">Filtrar por nome:</label> <input id=\"q\" type=\"search\" autocomplete=\"off\" placeholder=\"ex.: lobo, veneno, espada\"></p>");
        var entries = new List<(string Kind, string Name, string Id, string Url)>();
        entries.AddRange(_db.Classes.Values.Select(c => ("Classe", c.Name, c.Id, Url.Class(c.Id))));
        entries.AddRange(_usage.SecondClasses.Values.Select(sc => ("Segunda classe", sc.Def.Name, sc.Def.Id, Url.Class(sc.Def.Id))));
        entries.AddRange(_db.Abilities.Values.Where(a => _usage.IsUsed(a.Id)).Select(a => ("Habilidade", a.Name, a.Id, Url.Ability(a.Id))));
        entries.AddRange(_db.Monsters.Values.Select(m => ("Monstro", m.Name, m.Id, Url.Monster(m.Id))));
        entries.AddRange(_db.Items.Values.Select(i => ("Item", i.Name, i.Id, Url.Item(i.Id))));
        entries.AddRange(_db.Rules.Items.Affixes.Select(a => ("Afixo", a.Name, a.Id, Url.Affix(a.Id))));
        entries.AddRange(_db.Statuses.Values.Where(s => _usage.IsStatusUsed(s.Id)).Select(s => ("Status", _d.StatusLabel(s), s.Id, Url.Status(s.Id))));
        entries.AddRange(_db.Areas.Values.Where(a => _usage.IsAreaUsed(a.Id)).Select(a => ("Área", _d.AreaLabel(a.Id), a.Id, Url.Area(a.Id))));
        entries.AddRange(VisibleZones.Select(z => ("Zona", z.Name, z.Id, Url.Zone(z.Id))));
        p.Add("<ul id=\"results\" class=\"search\">");
        foreach (var e in entries.OrderBy(e => e.Name, StringComparer.CurrentCulture))
        {
            p.Add($"<li data-s=\"{Esc((e.Name + " " + e.Kind).ToLowerInvariant())}\">{Icon(p, e.Id)}<a href=\"{Esc(p.Href(e.Url))}\">{Esc(e.Name)}</a> <span class=\"muted\">{e.Kind}</span></li>");
        }

        p.Add("</ul>");
        p.Add($"<script src=\"{Esc(p.Href("assets/search.js"))}\"></script>");
    }
}
