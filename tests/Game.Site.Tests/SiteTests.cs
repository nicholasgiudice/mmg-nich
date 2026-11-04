using System.Text.Json;
using System.Text.RegularExpressions;

namespace Game.Site.Tests;

/// <summary>Gera o site uma vez, com os dados reais de <c>data/</c>, numa pasta temporária.</summary>
public sealed class SiteFixture : IDisposable
{
    public SiteFixture()
    {
        Root = FindRoot();
        Out = Path.Combine(Path.GetTempPath(), "valdran-site-" + Guid.NewGuid().ToString("N"));
        Result = SiteGenerator.Generate(new SiteOptions
        {
            DataDir = Path.Combine(Root, "data"),
            OutDir = Out,
            ContentDir = Path.Combine(Root, "site", "content"),
            ArtDir = Path.Combine(Root, "client", "assets", "art"),
            CreditsFile = Path.Combine(Root, "CREDITS.md"),
        });
        Html = Directory.Exists(Out)
            ? Directory.EnumerateFiles(Out, "*.html", SearchOption.AllDirectories)
                .ToDictionary(f => Path.GetRelativePath(Out, f).Replace('\\', '/'), File.ReadAllText)
            : [];
    }

    public string Root { get; }
    public string Out { get; }
    public SiteResult Result { get; }

    /// <summary>Páginas geradas: caminho relativo (com /) → HTML.</summary>
    public Dictionary<string, string> Html { get; }

    public static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "MmoRpg.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir ?? throw new DirectoryNotFoundException("raiz do repositório (MmoRpg.sln) não encontrada");
    }

    public JsonElement Json(string file) => JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "data", file))).RootElement;

    public void Dispose()
    {
        if (Directory.Exists(Out))
        {
            Directory.Delete(Out, recursive: true);
        }
    }
}

public sealed partial class SiteTests : IClassFixture<SiteFixture>
{
    private readonly SiteFixture _site;

    public SiteTests(SiteFixture site) => _site = site;

    [Fact]
    public void Generates_without_errors()
    {
        Assert.True(_site.Result.Success, string.Join("\n", _site.Result.Errors));
        Assert.Contains("index.html", _site.Html.Keys);
        Assert.True(File.Exists(Path.Combine(_site.Out, "assets", "style.css")));
    }

    [Fact]
    public void Every_id_appears_on_some_page()
    {
        var all = string.Concat(_site.Html.Values);
        var ids = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_site.Root, "data"), "*.json"))
        {
            var root = JsonDocument.Parse(File.ReadAllText(file)).RootElement;
            foreach (var section in new[] { "classes", "abilities", "monsters", "items", "statuses", "areas" })
            {
                if (root.TryGetProperty(section, out var list) && list.ValueKind == JsonValueKind.Array)
                {
                    ids.AddRange(list.EnumerateArray().Select(e => e.GetProperty("id").GetString()!));
                }
            }

            if (root.TryGetProperty("rules", out var rules) && rules.TryGetProperty("items", out var items) && items.TryGetProperty("affixes", out var affixes))
            {
                ids.AddRange(affixes.EnumerateArray().Select(e => e.GetProperty("id").GetString()!));
            }
        }

        Assert.True(ids.Count > 50, "esperava ids de todas as seções");
        var missing = ids.Where(id => !all.Contains($">{id}<", StringComparison.Ordinal) && !all.Contains($"id=\"{id}\"", StringComparison.Ordinal)).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Monster_health_matches_json()
    {
        var wolf = _site.Json("monsters.json").GetProperty("monsters").EnumerateArray().First(m => m.GetProperty("id").GetString() == "monster.grey_wolf");
        var health = wolf.GetProperty("health").GetDouble();
        Assert.Contains($"data-k=\"health\">{Fmt.Num(health)}<", _site.Html["monstros/grey_wolf.html"], StringComparison.Ordinal);
    }

    [Fact]
    public void Inherited_rank_cost_matches_json()
    {
        // Primeira habilidade do Guerreiro cujo NH2 não declara custo: o NH2 herda o custo do NH1.
        var ability = _site.Json("abilities_warrior.json").GetProperty("abilities").EnumerateArray().First(a =>
            a.TryGetProperty("cost", out _) && a.TryGetProperty("ranks", out var r) && r.EnumerateArray().Any(x => x.GetProperty("rank").GetInt32() == 2 && !x.TryGetProperty("cost", out _)));
        var id = ability.GetProperty("id").GetString()!;
        var amount = ability.GetProperty("cost").GetProperty("amount").GetDouble();

        var html = _site.Html[$"habilidades/{id}.html"];
        var row = RankRow(html, 2);
        Assert.Contains($"data-k=\"cost\">{Fmt.Num(amount)} Mana<", row, StringComparison.Ordinal);
    }

    [Fact]
    public void Item_attribute_matches_json()
    {
        var sword = _site.Json("items.json").GetProperty("items").EnumerateArray().First(i => i.GetProperty("id").GetString() == "weapon.sword");
        var stat = sword.GetProperty("stats")[0];
        var html = _site.Html["itens/index.html"];
        var row = Regex.Match(html, "<tr id=\"weapon.sword\".*?</tr>", RegexOptions.Singleline).Value;
        Assert.NotEmpty(row);
        Assert.Contains($"data-k=\"base\">{Fmt.Num(stat.GetProperty("base").GetDouble())}<", row, StringComparison.Ordinal);
        Assert.Contains($"data-k=\"perLevel\">{Fmt.Num(stat.GetProperty("perLevel").GetDouble())}<", row, StringComparison.Ordinal);
    }

    [Fact]
    public void No_broken_internal_links()
    {
        var broken = new List<string>();
        var checkedLinks = 0;
        foreach (var (page, html) in _site.Html)
        {
            var dir = Path.GetDirectoryName(Path.Combine(_site.Out, page))!;
            foreach (Match m in LinkRegex().Matches(html))
            {
                var target = System.Net.WebUtility.HtmlDecode(m.Groups["u"].Value);
                if (target.StartsWith("http://", StringComparison.Ordinal) || target.StartsWith("https://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal))
                {
                    continue;
                }

                checkedLinks++;
                var hash = target.IndexOf('#');
                var file = hash >= 0 ? target[..hash] : target;
                var fragment = hash >= 0 ? target[(hash + 1)..] : null;
                var path = file.Length == 0 ? Path.Combine(_site.Out, page) : Path.GetFullPath(Path.Combine(dir, file));
                if (!path.StartsWith(_site.Out, StringComparison.Ordinal) || !File.Exists(path))
                {
                    broken.Add($"{page}: {target}");
                    continue;
                }

                if (fragment is { Length: > 0 } && !File.ReadAllText(path).Contains($"id=\"{fragment}\"", StringComparison.Ordinal))
                {
                    broken.Add($"{page}: {target} (âncora)");
                }
            }
        }

        Assert.Empty(broken);
        Assert.True(checkedLinks > 500, $"poucos links conferidos: {checkedLinks}");
    }

    [Fact]
    public void Unused_ability_stays_out_of_class_pages_and_is_reported()
    {
        // Armadilha de Contenção existe em abilities_archer.json mas não na lista do Arqueiro (ponto em aberto).
        var archer = _site.Json("classes.json").GetProperty("classes").EnumerateArray().First(c => c.GetProperty("id").GetString() == "class.archer");
        var listed = archer.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()).Append(archer.GetProperty("basicAttack").GetString()).ToHashSet();
        var unused = _site.Json("abilities_archer.json").GetProperty("abilities").EnumerateArray()
            .Select(a => a.GetProperty("id").GetString()!)
            .Where(id => !listed.Contains(id))
            .ToList();

        foreach (var id in unused)
        {
            Assert.DoesNotContain(id, _site.Html["classes/archer.html"], StringComparison.Ordinal);
            Assert.DoesNotContain($"habilidades/{id}.html", _site.Html["habilidades/index.html"], StringComparison.Ordinal);
            Assert.Contains(id, _site.Html["em-aberto.html"], StringComparison.Ordinal);
            Assert.Contains(_site.Result.Warnings, w => w.Contains(id, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Credits_page_comes_from_credits_file()
    {
        Assert.Contains("Liberated Pixel Cup", _site.Html["creditos.html"], StringComparison.Ordinal);
        Assert.Contains("OGA-BY", _site.Html["creditos.html"], StringComparison.Ordinal);
    }

    [Fact]
    public void Icons_from_the_game_appear_on_ability_and_item_pages()
    {
        var sheet = Path.Combine(_site.Out, "assets", "icons", "icons.png");
        Assert.True(File.Exists(sheet), "icons.png não foi copiado");
        var icons = JsonDocument.Parse(File.ReadAllText(Path.Combine(_site.Root, "client", "assets", "art", "icons", "icons.json")))
            .RootElement.GetProperty("icons");

        // Toda habilidade das classes tem ícone no icons.json e o mostra, na posição dele, na própria página.
        foreach (var cls in _site.Json("classes.json").GetProperty("classes").EnumerateArray())
        {
            var basic = cls.GetProperty("basicAttack").GetString()!;
            foreach (var id in cls.GetProperty("abilities").EnumerateArray().Select(x => x.GetString()!).Append(basic))
            {
                Assert.True(icons.TryGetProperty(id, out var icon), $"{id} sem ícone em icons.json");
                var html = _site.Html[$"habilidades/{id}.html"];
                // No topo da página o ícone aparece ampliado 2×, então a posição na folha também dobra.
                Assert.Contains($"background-position:-{icon.GetProperty("x").GetInt32() * 2}px -{icon.GetProperty("y").GetInt32() * 2}px", html, StringComparison.Ordinal);
                Assert.Contains("assets/icons/icons.png", html, StringComparison.Ordinal);
            }
        }

        // Toda linha de item da página de itens tem ícone.
        var items = _site.Html["itens/index.html"];
        foreach (Match row in Regex.Matches(items, "<tr id=\"[^\"]+\" data-item=\"(?<id>[^\"]+)\"><td>(?<cell>.*?)</td>"))
        {
            Assert.Contains("class=\"icon\"", row.Groups["cell"].Value, StringComparison.Ordinal);
        }

        Assert.Contains("class=\"icon\"", _site.Html["classes/warrior.html"], StringComparison.Ordinal);
    }

    [Fact]
    public void Changing_a_number_in_data_changes_the_site()
    {
        var data = Path.Combine(Path.GetTempPath(), "valdran-data-" + Guid.NewGuid().ToString("N"));
        var output = data + "-out";
        try
        {
            CopyDirectory(Path.Combine(_site.Root, "data"), data);
            var monsters = Path.Combine(data, "monsters.json");
            var json = File.ReadAllText(monsters);
            var changed = Regex.Replace(json, "(\"id\": \"monster.grey_wolf\"[^}]*?\"health\": )\\d+", "${1}12345");
            Assert.NotEqual(json, changed);
            File.WriteAllText(monsters, changed);

            var result = SiteGenerator.Generate(new SiteOptions { DataDir = data, OutDir = output });
            Assert.True(result.Success, string.Join("\n", result.Errors));
            Assert.Contains("data-k=\"health\">12345<", File.ReadAllText(Path.Combine(output, "monstros", "grey_wolf.html")), StringComparison.Ordinal);
        }
        finally
        {
            foreach (var d in new[] { data, output }.Where(Directory.Exists))
            {
                Directory.Delete(d, recursive: true);
            }
        }
    }

    [Fact]
    public void Pages_have_title_description_and_viewport()
    {
        foreach (var (page, html) in _site.Html)
        {
            Assert.Matches("<title>[^<]+</title>", html);
            Assert.Contains("<meta name=\"description\" content=\"", html, StringComparison.Ordinal);
            Assert.Contains("name=\"viewport\"", html, StringComparison.Ordinal);
            Assert.DoesNotContain("content=\"\"", html, StringComparison.Ordinal);
            _ = page;
        }
    }

    private static string RankRow(string html, int rank) =>
        Regex.Match(html, $"<tr data-rank=\"{rank}\">.*?</tr>", RegexOptions.Singleline).Value;

    private static void CopyDirectory(string from, string to)
    {
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    [GeneratedRegex("(?:href|src)=\"(?<u>[^\"]+)\"|url\\('(?<u>[^']+)'\\)")]
    private static partial Regex LinkRegex();
}
