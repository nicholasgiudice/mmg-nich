using System.Globalization;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Site;

/// <summary>Formatação de números e textos em português (vírgula decimal, segundos a partir de ticks).</summary>
public static class Fmt
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Número com até 3 casas, sem zeros à direita e sem separador de milhar (ex.: 2,5; 27280).</summary>
    public static string Num(double value) => Math.Round(value, 3).ToString("0.###", PtBr);

    /// <summary>Segundos a partir de ticks de simulação.</summary>
    public static string Seconds(int ticks) => Num((double)ticks / TickConversion.TicksPerSecond) + " s";

    /// <summary>Fração como porcentagem (0,25 → 25%).</summary>
    public static string Pct(double fraction) => Num(fraction * 100) + "%";

    /// <summary>Multiplicador (× 1,25).</summary>
    public static string Mult(double value) => "× " + Num(value);

    /// <summary>Escapa só o necessário em HTML (acentos ficam legíveis no código-fonte da página).</summary>
    public static string Esc(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);

    public static string Code(string id) => $"<code>{Esc(id)}</code>";

    /// <summary>Valor com escala: base + coeficiente × atributo (ex.: 15 + 1 × ARMA + 2,5 × FOR).</summary>
    public static string Scaled(double baseValue, IReadOnlyDictionary<string, double> scaling)
    {
        var parts = new List<string>();
        if (baseValue != 0 || scaling.Count == 0)
        {
            parts.Add(Num(baseValue));
        }

        foreach (var key in ScalingOrder.Where(scaling.ContainsKey))
        {
            parts.Add($"{Num(scaling[key])} × {ScalingLabel(key)}");
        }

        foreach (var (key, coef) in scaling.Where(kv => !ScalingOrder.Contains(kv.Key)).OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            parts.Add($"{Num(coef)} × {key.ToUpperInvariant()}");
        }

        return string.Join(" + ", parts);
    }

    private static readonly string[] ScalingOrder = ["weapon", "str", "dex", "int", "vit"];

    public static string ScalingLabel(string key) => key switch
    {
        "weapon" => "ARMA",
        "str" => "FOR",
        "dex" => "DES",
        "agi" => "AGI",
        "int" => "INT",
        "vit" => "VIT",
        _ => key.ToUpperInvariant(),
    };

    public static string School(DamageSchool school) => school switch
    {
        DamageSchool.Physical => "físico",
        DamageSchool.Arcane => "arcano",
        DamageSchool.Fire => "de fogo",
        DamageSchool.Frost => "de gelo",
        DamageSchool.Holy => "sagrado",
        DamageSchool.Nature => "de natureza",
        _ => school.ToString().ToLowerInvariant(),
    };

    public static string Rank(MonsterRank rank) => rank switch
    {
        MonsterRank.Elite => "elite",
        MonsterRank.Boss => "chefe",
        _ => "normal",
    };

    public static string Rarity(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => "comum",
        ItemRarity.Uncommon => "incomum",
        ItemRarity.Rare => "raro",
        _ => "épico",
    };

    public static string Slot(EquipSlot slot) => slot switch
    {
        EquipSlot.Weapon => "arma",
        EquipSlot.Head => "cabeça",
        EquipSlot.Chest => "peito",
        EquipSlot.Hands => "mãos",
        EquipSlot.Feet => "pés",
        _ => "acessório",
    };

    public static string ArmorType(ArmorType type) => type switch
    {
        Core.Definitions.ArmorType.Heavy => "pesada",
        Core.Definitions.ArmorType.Medium => "média",
        _ => "leve",
    };

    public static string Stat(ItemStat stat) => stat switch
    {
        ItemStat.WeaponDamage => "dano da arma",
        ItemStat.Armor => "armadura",
        ItemStat.Resistance => "resistência",
        ItemStat.Str => "FOR",
        ItemStat.Dex => "DES",
        ItemStat.Agi => "AGI",
        ItemStat.Int => "INT",
        ItemStat.Vit => "VIT",
        ItemStat.Health => "vida",
        ItemStat.Crit => "chance de crítico",
        _ => "Mana por segundo",
    };

    /// <summary>Valor de estatística de item; crítico em porcentagem.</summary>
    public static string StatValue(ItemStat stat, double value) => stat == ItemStat.Crit ? Pct(value) : Num(value);

    public static string Faction(FactionFilter f, bool plural = false) => f switch
    {
        FactionFilter.Enemy => plural ? "inimigos" : "inimigo",
        FactionFilter.Ally => plural ? "aliados" : "aliado",
        _ => plural ? "unidades" : "unidade",
    };

    public static string Resource(string type) => type == "mana" ? "Mana" : type;

    /// <summary>Lista de NH compacta: [3,4,5] → "NH3–5"; [1,3] → "NH1, 3".</summary>
    public static string Ranks(IReadOnlyCollection<int> ranks)
    {
        var sorted = ranks.Distinct().OrderBy(r => r).ToList();
        var groups = new List<string>();
        for (var i = 0; i < sorted.Count;)
        {
            var j = i;
            while (j + 1 < sorted.Count && sorted[j + 1] == sorted[j] + 1)
            {
                j++;
            }

            groups.Add(i == j ? $"{sorted[i]}" : $"{sorted[i]}–{sorted[j]}");
            i = j + 1;
        }

        return "NH" + string.Join(", ", groups);
    }
}
