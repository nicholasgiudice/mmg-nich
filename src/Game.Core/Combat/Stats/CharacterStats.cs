using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Stats;

/// <summary>
/// Estatísticas de um personagem (T-201, <c>design/progression-v0.1.md</c> §3–§4): atributos finais (perfil + pontos +
/// equipamento), dano de arma, armadura, resistência e nível. As derivadas (crítico, velocidade de ataque) saem daqui
/// com as <see cref="StatRules"/> (<see cref="StatMath"/>). Replicável: o cliente mostra.
/// </summary>
public sealed class CharacterStats : IComponent, IReplicated
{
    public int Level { get; set; } = 1;
    public int Str { get; set; }
    public int Dex { get; set; }
    public int Agi { get; set; }
    public int Int { get; set; }
    public int Vit { get; set; }

    /// <summary>Dano da arma equipada (ARMA na escala das habilidades).</summary>
    public float WeaponDamage { get; set; }

    /// <summary>Reduz dano físico.</summary>
    public float Armor { get; set; }

    /// <summary>Reduz dano das outras escolas (arcano, fogo, gelo, natureza, sagrado).</summary>
    public float Resistance { get; set; }

    public IComponent Clone() => new CharacterStats
    {
        Level = Level,
        Str = Str,
        Dex = Dex,
        Agi = Agi,
        Int = Int,
        Vit = Vit,
        WeaponDamage = WeaponDamage,
        Armor = Armor,
        Resistance = Resistance,
    };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Level);
        hasher.Add(Str);
        hasher.Add(Dex);
        hasher.Add(Agi);
        hasher.Add(Int);
        hasher.Add(Vit);
        hasher.Add(WeaponDamage);
        hasher.Add(Armor);
        hasher.Add(Resistance);
    }
}

/// <summary>Contas das estatísticas (T-201). Tudo determinístico; o crítico usa o RNG da simulação.</summary>
public static class StatMath
{
    /// <summary>Estatísticas no nível 1 a partir do perfil da classe e da arma inicial; null se a classe não tem atributos.</summary>
    public static CharacterStats? FromClass(ClassDefinition cls)
    {
        if (cls.Attributes is not { } a)
        {
            return null;
        }

        return new CharacterStats
        {
            Str = a.Str,
            Dex = a.Dex,
            Agi = a.Agi,
            Int = a.Int + (cls.StartingWeapon?.Int ?? 0),
            Vit = a.Vit,
            WeaponDamage = (float)(cls.StartingWeapon?.Damage ?? 0),
        };
    }

    public static double MaxHealth(ClassDefinition cls, CharacterStats s, StatRules r) =>
        cls.BaseHealth + (s.Vit * r.HealthPerVit) + ((s.Level - 1) * cls.HealthGrowth);

    public static double MaxMana(CharacterStats s, StatRules r) => r.ManaBase + (r.ManaPerInt * s.Int);

    public static double ManaRegenBonus(CharacterStats s, StatRules r) => s.Int * r.ManaRegenPerInt;

    public static double CritChance(CharacterStats s, StatRules r) => Math.Min(r.CritMax, r.CritBase + (s.Agi * r.CritPerAgi));

    /// <summary>Divisor da recarga do ataque básico: 1 + min(AGI × porPonto, máximo).</summary>
    public static double AttackSpeedFactor(CharacterStats s, StatRules r) => 1 + Math.Min(r.AttackSpeedMax, s.Agi * r.AttackSpeedPerAgi);

    /// <summary>Valor de uma chave de escala (<see cref="ScalingKeys"/>) para as estatísticas.</summary>
    public static double Stat(CharacterStats s, string key) => key switch
    {
        "weapon" => s.WeaponDamage,
        "str" => s.Str,
        "dex" => s.Dex,
        "int" => s.Int,
        "vit" => s.Vit,
        _ => 0,
    };

    /// <summary>base + Σ coeficiente × estatística da fonte; fonte sem estatísticas (monstro, sem fonte) = só a base.</summary>
    public static double Scaled(double baseValue, IReadOnlyDictionary<string, double> scaling, Entity? source)
    {
        if (scaling.Count == 0 || source?.TryGet<CharacterStats>() is not { } s)
        {
            return baseValue;
        }

        var value = baseValue;
        foreach (var (key, coef) in scaling)
        {
            value += coef * Stat(s, key);
        }

        return value;
    }

    /// <summary>Sorteia crítico para a fonte (só quem tem estatísticas pode critar). Consome o RNG só nesse caso.</summary>
    public static bool RollCrit(World world, Entity? source) =>
        source?.TryGet<CharacterStats>() is { } s && world.Random.NextFloat() < CritChance(s, world.Content.Rules.Stats);

    /// <summary>Nível de quem ataca (personagem: <see cref="CharacterStats.Level"/>; resto: 1 até a T-203).</summary>
    public static int LevelOf(Entity? e) => e?.TryGet<CharacterStats>()?.Level ?? 1;

    /// <summary>Fração do dano que a defesa do alvo tira: D / (D + base + porNível × nível do atacante), teto da regra.</summary>
    public static double DefenseReduction(World world, Entity target, Entity? attacker, DamageSchool school)
    {
        var physical = school == DamageSchool.Physical;
        var defense = target.TryGet<CharacterStats>() is { } s
            ? (physical ? s.Armor : s.Resistance)
            : target.TryGet<UnitInfo>() is { } u ? (physical ? u.Armor : u.Resistance) : 0;
        if (defense <= 0)
        {
            return 0;
        }

        var r = world.Content.Rules.Stats;
        var reduction = defense / (defense + r.DefenseBase + (r.DefensePerAttackerLevel * LevelOf(attacker)));
        return Math.Min(r.DefenseMax, reduction);
    }
}
