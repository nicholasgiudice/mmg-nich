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

    /// <summary>Ids de classes, habilidades, monstros, itens, status, áreas e afixos em <c>data/*.json</c>.</summary>
    private List<string> DataIds()
    {
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

            // T-075: segundas classes, dentro de cada classe base.
            if (root.TryGetProperty("classes", out var classes) && classes.ValueKind == JsonValueKind.Array)
            {
                ids.AddRange(classes.EnumerateArray()
                    .SelectMany(c => c.TryGetProperty("secondClasses", out var sc) ? sc.EnumerateArray() : Enumerable.Empty<JsonElement>())
                    .Select(e => e.GetProperty("id").GetString()!));
            }

            if (root.TryGetProperty("rules", out var rules) && rules.TryGetProperty("items", out var items) && items.TryGetProperty("affixes", out var affixes))
            {
                ids.AddRange(affixes.EnumerateArray().Select(e => e.GetProperty("id").GetString()!));
            }
        }

        Assert.True(ids.Count > 50, "esperava ids de todas as seções");
        return ids;
    }

    /// <summary>O que o gerador deixou fora do site por não ser usado (avisa no console).</summary>
    private bool IsLeftOut(string id) =>
        _site.Result.Warnings.Any(w => w.Contains($" {id} ", StringComparison.Ordinal) && w.EndsWith("fica fora do site", StringComparison.Ordinal));

    [Fact]
    public void Every_used_id_has_a_page_or_an_anchor()
    {
        var all = string.Concat(_site.Html.Values);
        var missing = DataIds()
            .Where(id => !IsLeftOut(id))
            .Where(id => !all.Contains($"id=\"{id}\"", StringComparison.Ordinal)
                && !_site.Html.ContainsKey($"habilidades/{id}.html")
                && !_site.Html.Keys.Any(k => k.EndsWith("/" + Url.Slug(id) + ".html", StringComparison.Ordinal)))
            .ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Ids_never_show_up_in_the_text()
    {
        var ids = DataIds().Concat(_site.Json("zones.json").GetProperty("zones").EnumerateArray().Select(z => z.GetProperty("id").GetString()!)).ToList();
        var leaks = new List<string>();
        foreach (var (page, html) in _site.Html.Where(kv => kv.Key != "creditos.html"))
        {
            // Texto visível: sem scripts, sem tags (atributos como href e id podem ter ids).
            var text = Regex.Replace(Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.Singleline), "<[^>]+>", " ");
            leaks.AddRange(ids.Where(id => Regex.IsMatch(text, $"(?<![\\w.]){Regex.Escape(id)}(?![\\w.])")).Select(id => $"{page}: {id}"));
            leaks.AddRange(Regex.Matches(text, "\\b(?:ai|phase|zone|encounter|map|npc)\\.[a-z_]+").Select(m => $"{page}: {m.Value}"));
        }

        Assert.Empty(leaks);
    }

    [Fact]
    public void Hidden_zones_and_test_encounters_stay_out()
    {
        var all = string.Concat(_site.Html.Values);
        Assert.DoesNotContain("Covil de Teste", all, StringComparison.Ordinal);
        Assert.DoesNotContain("Encontros de teste", all, StringComparison.Ordinal);
        Assert.Contains("Ruínas da Estrada Antiga", _site.Html["monstros/index.html"], StringComparison.Ordinal);
    }

    [Fact]
    public void Download_page_links_to_the_client_and_reads_latest_json()
    {
        var html = _site.Html["download.html"];
        Assert.Contains($"href=\"{Url.DownloadFile}\"", html, StringComparison.Ordinal);
        Assert.Contains("assets/download.js", html, StringComparison.Ordinal);
        Assert.Contains("Valdran.exe", html, StringComparison.Ordinal);
        Assert.Contains("/downloads/latest.json", File.ReadAllText(Path.Combine(_site.Out, "assets", "download.js")), StringComparison.Ordinal);
        Assert.All(_site.Html.Values, page => Assert.Contains("download.html\"", page, StringComparison.Ordinal));
    }

    [Fact]
    public void Interface_art_from_the_game_is_copied_and_used()
    {
        foreach (var file in UiArt.Files)
        {
            Assert.True(File.Exists(Path.Combine(_site.Out, UiArt.PublishedDir, file)), $"{file} não foi copiado");
        }

        var css = File.ReadAllText(Path.Combine(_site.Out, "assets", "style.css"));
        foreach (var file in UiArt.Files.Where(f => f is not "button_hover.png" and not "button_pressed.png"))
        {
            Assert.Contains($"url(\"ui/{file}\")", css, StringComparison.Ordinal);
        }

        Assert.Contains("class=\"hero\"", _site.Html["index.html"], StringComparison.Ordinal);
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
                // /downloads/ não é gerado com o site: scripts/deploy-download.sh publica lá, e o Caddy serve.
                if (target.StartsWith("http://", StringComparison.Ordinal) || target.StartsWith("https://", StringComparison.Ordinal) || target.StartsWith("mailto:", StringComparison.Ordinal)
                    || target.StartsWith("/downloads/", StringComparison.Ordinal))
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
    public void Unused_ability_stays_out_of_the_site_and_is_reported()
    {
        // Armadilha de Contenção existe em abilities_archer.json mas não na lista do Arqueiro: não é do jogo atual.
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
            Assert.DoesNotContain($"habilidades/{id}.html", _site.Html.Keys);
            Assert.DoesNotContain($"habilidades/{id}.html", _site.Html["busca.html"], StringComparison.Ordinal);
            Assert.True(IsLeftOut(id), $"{id}: esperava o aviso de que fica fora do site");
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

    /// <summary>Segundas classes de <c>classes.json</c>: classe base, id e habilidades.</summary>
    private List<(string Base, string Id, List<string> Abilities)> SecondClasses() =>
        _site.Json("classes.json").GetProperty("classes").EnumerateArray()
            .SelectMany(c => c.GetProperty("secondClasses").EnumerateArray().Select(sc => (
                c.GetProperty("id").GetString()!,
                sc.GetProperty("id").GetString()!,
                sc.GetProperty("abilities").EnumerateArray().Select(a => a.GetString()!).ToList())))
            .ToList();

    /// <summary>Habilidades de todos os <c>abilities_*.json</c>, por id.</summary>
    private Dictionary<string, JsonElement> AbilitiesJson() =>
        Directory.EnumerateFiles(Path.Combine(_site.Root, "data"), "abilities_*.json")
            .SelectMany(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement.GetProperty("abilities").EnumerateArray())
            .ToDictionary(a => a.GetProperty("id").GetString()!, a => a);

    [Fact]
    public void Every_second_class_and_its_abilities_are_on_the_site()
    {
        var list = SecondClasses();
        Assert.Equal(8, list.Count);
        var index = _site.Html["habilidades/index.html"];
        foreach (var (baseId, id, abilities) in list)
        {
            var page = Url.Class(id);
            Assert.True(_site.Html.ContainsKey(page), $"{id}: sem página {page}");
            // Links relativos: "../classes/x.html" a partir de classes/, "classes/x.html" a partir da raiz.
            var href = page + "\"";
            Assert.Contains(href, _site.Html[Url.Class(baseId)], StringComparison.Ordinal);
            Assert.Contains(href, _site.Html[Url.Classes], StringComparison.Ordinal);
            Assert.Contains($"href=\"{href}", _site.Html[Url.Home], StringComparison.Ordinal);
            Assert.Contains($"id=\"{id}\"", index, StringComparison.Ordinal);
            Assert.False(IsLeftOut(id));
            Assert.Equal(5, abilities.Count);
            foreach (var ability in abilities)
            {
                Assert.False(IsLeftOut(ability), $"{ability}: ficou fora do site");
                Assert.True(_site.Html.ContainsKey(Url.Ability(ability)), $"{ability}: sem página");
                Assert.Contains($"href=\"../{Url.Ability(ability)}\"", _site.Html[page], StringComparison.Ordinal);
                Assert.Contains($"href=\"../{Url.Ability(ability)}\"", index, StringComparison.Ordinal);
            }
        }

        // Nada das segundas classes (nem as etiquetas novas) fica sem texto.
        Assert.DoesNotContain(_site.Result.Warnings, w => w.Contains("sem texto", StringComparison.Ordinal)
            || w.Contains("sem rótulo", StringComparison.Ordinal) || w.Contains("sem descrição", StringComparison.Ordinal));
    }

    [Fact]
    public void Second_class_inherited_rank_cost_and_required_level_match_json()
    {
        // Primeira habilidade de segunda classe cujo NH2 não declara custo: o NH2 herda o custo do NH1.
        var json = AbilitiesJson();
        var (id, ability) = SecondClasses().SelectMany(sc => sc.Abilities).Select(a => (a, json[a])).First(x =>
            x.Item2.TryGetProperty("cost", out _) && x.Item2.TryGetProperty("ranks", out var r)
            && r.EnumerateArray().Any(y => y.GetProperty("rank").GetInt32() == 2 && !y.TryGetProperty("cost", out _)));
        var amount = ability.GetProperty("cost").GetProperty("amount").GetDouble();
        var html = _site.Html[Url.Ability(id)];
        Assert.Contains($"data-k=\"cost\">{Fmt.Num(amount)} Mana<", RankRow(html, 2), StringComparison.Ordinal);

        // Nível exigido: as listas da segunda classe em rules.json, não as da base.
        var prog = _site.Json("rules.json").GetProperty("rules").GetProperty("progression");
        var signature = ability.TryGetProperty("signature", out var sig) && sig.GetBoolean();
        var levels = prog.GetProperty(signature ? "secondClassSignatureRankLevels" : "secondClassRankLevels");
        Assert.Contains($"data-k=\"level\">{levels[0].GetInt32()}<", RankRow(html, 1), StringComparison.Ordinal);
        Assert.Contains($"data-k=\"level\">{levels[1].GetInt32()}<", RankRow(html, 2), StringComparison.Ordinal);

        // Uma passiva também segue os níveis da segunda classe.
        var passive = SecondClasses().SelectMany(sc => sc.Abilities).First(a => json[a].TryGetProperty("passive", out var p) && p.GetBoolean());
        Assert.Contains($"data-k=\"level\">{prog.GetProperty("secondClassRankLevels")[0].GetInt32()}<", RankRow(_site.Html[Url.Ability(passive)], 1), StringComparison.Ordinal);
    }

    [Fact]
    public void Channel_pulses_match_json()
    {
        var json = AbilitiesJson();
        var (id, ability) = json.First(kv => kv.Value.TryGetProperty("channel", out _) && kv.Value.TryGetProperty("ranks", out _));
        var pulses = ability.GetProperty("channel").GetProperty("pulses").GetInt32();
        Assert.Contains($"data-k=\"channel\">{pulses} pulsos, um a cada", RankRow(_site.Html[Url.Ability(id)], 1), StringComparison.Ordinal);
    }

    [Fact]
    public void No_raw_field_or_type_name_in_the_text()
    {
        // Nomes de tipo do Core (ex.: CurrentTargetEffect) e chaves cruas dos dados (ex.: allyHeal, copy_status) nunca
        // aparecem no texto: todo campo conhecido tem descrição em português.
        var types = typeof(Core.Definitions.EffectDefinition).Assembly.GetTypes()
            .Where(t => t.Namespace == "Game.Core.Definitions" && t.IsPublic && t.Name.Length > 6)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_site.Root, "data"), "*.json"))
        {
            var root = JsonDocument.Parse(File.ReadAllText(file)).RootElement;
            foreach (var section in new[] { "abilities", "statuses", "areas" })
            {
                if (root.TryGetProperty(section, out var list))
                {
                    CollectKeys(list, keys);
                }
            }
        }

        Assert.Contains("allyHeal", keys);
        Assert.Contains("current_target", keys);
        var words = types.Concat(keys.Where(k => k.Any(char.IsUpper) || k.Contains('_', StringComparison.Ordinal))).ToList();
        var leaks = new List<string>();
        foreach (var (page, html) in _site.Html.Where(kv => kv.Key != "creditos.html"))
        {
            var text = Regex.Replace(Regex.Replace(html, "<script.*?</script>", " ", RegexOptions.Singleline), "<[^>]+>", " ");
            leaks.AddRange(words.Where(w => Regex.IsMatch(text, $"\\b{Regex.Escape(w)}\\b")).Select(w => $"{page}: {w}"));
        }

        Assert.Empty(leaks);
    }

    /// <summary>Chaves de objetos e valores de <c>type</c> (discriminadores de efeito e alvo), em toda a árvore.</summary>
    private static void CollectKeys(JsonElement e, HashSet<string> keys)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var p in e.EnumerateObject())
                {
                    keys.Add(p.Name);
                    if (p.Name == "type" && p.Value.ValueKind == JsonValueKind.String)
                    {
                        keys.Add(p.Value.GetString()!);
                    }

                    // Chaves de dicionário (escala, bônus por tag) são dados, não campos.
                    if (p.Name is not "scaling" and not "bonusVsTags" and not "damageTakenBySchool" and not "attributes")
                    {
                        CollectKeys(p.Value, keys);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var x in e.EnumerateArray())
                {
                    CollectKeys(x, keys);
                }

                break;
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
