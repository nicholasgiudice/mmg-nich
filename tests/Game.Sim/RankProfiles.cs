using Game.Core.Combat;

namespace Game.Sim;

/// <summary>
/// NH por classe (T-063): perfis prontos e o formato <c>classe:habilidade=NH</c> da linha de comando.
/// Os perfis gastam os 15 pontos do nível 15 (§7 de design/progression-v0.1.md); os atributos continuam os do nível 1,
/// porque os encontros do simulador são os do protótipo.
/// </summary>
public static class RankProfiles
{
    /// <summary>Todas as normais no NH1, sem assinatura (o protótipo).</summary>
    public const string Nh1 = "nh1";

    /// <summary>Duas habilidades no NH5, as outras duas no NH1 e a assinatura no NH3 (5+5+1+1+3).</summary>
    public const string Focused = "focado";

    /// <summary>Pontos espalhados: as quatro normais no NH3 e a assinatura no NH3 (3+3+3+3+3).</summary>
    public const string Spread = "espalhado";

    public static IReadOnlyList<string> Names { get; } = [Nh1, Focused, Spread];

    /// <summary>As duas habilidades do perfil focado de cada classe (exemplos da §7).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> FocusedPair = new Dictionary<string, string[]>
    {
        ["class.warrior"] = ["warrior.heavy_strike", "warrior.whirlwind"],
        ["class.archer"] = ["archer.power_shot", "archer.hunters_mark"],
        ["class.mage"] = ["mage.elemental_blast", "mage.lightning_field"],
        ["class.cleric"] = ["cleric.direct_heal", "cleric.sacred_shield"],
    };

    /// <summary>NH por classe do perfil (vazio para <see cref="Nh1"/>).</summary>
    public static Dictionary<string, Dictionary<string, int>> For(string profile, ContentCatalog content)
    {
        var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        if (profile == Nh1)
        {
            return result;
        }

        if (profile != Focused && profile != Spread)
        {
            throw new ArgumentException($"Perfil de NH desconhecido '{profile}'. Disponíveis: {string.Join(", ", Names)}.");
        }

        foreach (var cls in content.Classes.Values)
        {
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in cls.Abilities)
            {
                if (content.Abilities[id].Signature || profile == Spread)
                {
                    ranks[id] = 3;
                }
                else
                {
                    ranks[id] = FocusedPair.TryGetValue(cls.Id, out var pair) && pair.Contains(id) ? 5 : 1;
                }
            }

            result[cls.Id] = ranks;
        }

        return result;
    }

    /// <summary>Acrescenta <c>classe:habilidade=NH</c> (ex.: <c>class.archer:archer.power_shot=5</c>).</summary>
    public static void AddPair(Dictionary<string, Dictionary<string, int>> into, string spec)
    {
        var colon = spec.IndexOf(':', StringComparison.Ordinal);
        var eq = spec.IndexOf('=', StringComparison.Ordinal);
        if (colon <= 0 || eq <= colon + 1 || !int.TryParse(spec[(eq + 1)..], out var rank))
        {
            throw new ArgumentException($"--rank espera classe:habilidade=NH, recebeu '{spec}'.");
        }

        var cls = spec[..colon];
        if (!into.TryGetValue(cls, out var ranks))
        {
            into[cls] = ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        ranks[spec[(colon + 1)..eq]] = rank;
    }
}
