using System.Text;
using static Game.Site.Fmt;

namespace Game.Site;

/// <summary>
/// Endereços das páginas, relativos à raiz do site. Um id dos dados vira nome de arquivo ou âncora (só no endereço: o texto
/// das páginas nunca mostra ids).
/// </summary>
public static class Url
{
    public const string Home = "index.html";
    public const string Classes = "classes/index.html";
    public const string Abilities = "habilidades/index.html";
    public const string Monsters = "monstros/index.html";
    public const string Items = "itens/index.html";
    public const string Statuses = "referencia/status.html";
    public const string Areas = "referencia/areas.html";
    public const string Credits = "creditos.html";
    public const string Download = "download.html";
    public const string Search = "busca.html";

    /// <summary>
    /// Arquivo do cliente para Windows. Não é gerado com o site: <c>scripts/deploy-download.sh</c> o publica em
    /// <c>MMO_SITE_DIR/downloads/</c>, que o Caddy serve em <c>/downloads/</c> (por isso o caminho é absoluto).
    /// </summary>
    public const string DownloadFile = "/downloads/Valdran-windows.zip";

    public static string Class(string id) => $"classes/{Slug(id)}.html";

    public static string Ability(string id) => $"habilidades/{id}.html";

    public static string Monster(string id) => $"monstros/{Slug(id)}.html";

    public static string Item(string id) => $"{Items}#{id}";

    public static string Affix(string id) => $"{Items}#{id}";

    public static string Status(string id) => $"{Statuses}#{id}";

    public static string Area(string id) => $"{Areas}#{id}";

    public static string Zone(string id) => $"{Monsters}#{id}";

    /// <summary>Parte do id depois do primeiro ponto (<c>class.warrior</c> → <c>warrior</c>).</summary>
    public static string Slug(string id) => id.Contains('.') ? id[(id.IndexOf('.') + 1)..] : id;
}

/// <summary>Uma página em construção: caminho, título, descrição e corpo HTML. Links são relativos à página.</summary>
public sealed class Page
{
    public Page(string path, string title, string description)
    {
        Path = path;
        Title = title;
        Description = description;
    }

    public string Path { get; }
    public string Title { get; }
    public string Description { get; }
    public StringBuilder Body { get; } = new();

    /// <summary>Página com a imagem de abertura no topo (a inicial).</summary>
    public bool Hero { get; set; }

    /// <summary>Caminho relativo desta página até <paramref name="target"/> (caminho a partir da raiz).</summary>
    public string Href(string target)
    {
        var depth = Path.Count(c => c == '/');
        return string.Concat(Enumerable.Repeat("../", depth)) + target;
    }

    /// <summary>Link com texto escapado.</summary>
    public string A(string target, string text) => $"<a href=\"{Esc(Href(target))}\">{Esc(text)}</a>";

    public Page Add(string html)
    {
        Body.Append(html).Append('\n');
        return this;
    }

    public string Render(string siteTitle)
    {
        string Nav(string target, string text) =>
            $"<a href=\"{Esc(Href(target))}\"{(Section(target) == Section(Path) ? " aria-current=\"page\"" : "")}>{Esc(text)}</a>";

        var fullTitle = Title == siteTitle ? siteTitle : $"{Title} — {siteTitle}";
        return $"""
            <!doctype html>
            <html lang="pt-BR">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{Esc(fullTitle)}</title>
            <meta name="description" content="{Esc(Description)}">
            <link rel="stylesheet" href="{Esc(Href("assets/style.css"))}">
            </head>
            <body{(Hero ? " class=\"home\"" : "")}>
            <header class="top">
            <a class="brand" href="{Esc(Href(Url.Home))}">{Esc(siteTitle)}</a>
            <nav>
            {Nav(Url.Classes, "Classes")}
            {Nav(Url.Abilities, "Habilidades")}
            {Nav(Url.Monsters, "Monstros")}
            {Nav(Url.Items, "Itens")}
            {Nav(Url.Statuses, "Referência")}
            {Nav(Url.Search, "Busca")}
            <a class="nav-download" href="{Esc(Href(Url.Download))}"{(Path == Url.Download ? " aria-current=\"page\"" : "")}>Download</a>
            </nav>
            </header>
            <main>
            {Body}
            </main>
            <footer>
            <p>{Esc(siteTitle)} · <a href="{Esc(Href(Url.Download))}">Download</a> · <a href="{Esc(Href(Url.Credits))}">Créditos</a></p>
            </footer>
            </body>
            </html>

            """;
    }

    private static string Section(string path) => path.Contains('/') ? path[..path.IndexOf('/')] : path;
}
