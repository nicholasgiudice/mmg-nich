using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Progression;

/// <summary>
/// Progressão de um personagem (T-203): XP no nível atual, XP para o próximo, pontos livres e os pontos de atributo já
/// distribuídos. O nível fica em <see cref="Stats.CharacterStats.Level"/>; os NH em <see cref="AbilityState.Ranks"/>.
/// Só quem tem este componente ganha XP e aceita as intenções de pontos. Replicável: o cliente mostra.
/// </summary>
public sealed class CharacterProgress : IComponent, IReplicated
{
    /// <summary>XP acumulada dentro do nível atual (0 no nível máximo).</summary>
    public int Experience { get; set; }

    /// <summary>XP que falta acumular para subir (0 no nível máximo).</summary>
    public int ExperienceToNext { get; set; }

    public int AttributePoints { get; set; }
    public int SkillPoints { get; set; }

    /// <summary>Pontos de atributo distribuídos (somados ao perfil da classe em <see cref="Stats.CharacterStats"/>).</summary>
    public int AllocatedStr { get; set; }
    public int AllocatedDex { get; set; }
    public int AllocatedAgi { get; set; }
    public int AllocatedInt { get; set; }
    public int AllocatedVit { get; set; }

    public int AllocatedTotal => AllocatedStr + AllocatedDex + AllocatedAgi + AllocatedInt + AllocatedVit;

    public int Allocated(AttributeKind a) => a switch
    {
        AttributeKind.Str => AllocatedStr,
        AttributeKind.Dex => AllocatedDex,
        AttributeKind.Agi => AllocatedAgi,
        AttributeKind.Int => AllocatedInt,
        _ => AllocatedVit,
    };

    public void SetAllocated(AttributeKind a, int value)
    {
        switch (a)
        {
            case AttributeKind.Str: AllocatedStr = value; break;
            case AttributeKind.Dex: AllocatedDex = value; break;
            case AttributeKind.Agi: AllocatedAgi = value; break;
            case AttributeKind.Int: AllocatedInt = value; break;
            default: AllocatedVit = value; break;
        }
    }

    public IComponent Clone() => new CharacterProgress
    {
        Experience = Experience,
        ExperienceToNext = ExperienceToNext,
        AttributePoints = AttributePoints,
        SkillPoints = SkillPoints,
        AllocatedStr = AllocatedStr,
        AllocatedDex = AllocatedDex,
        AllocatedAgi = AllocatedAgi,
        AllocatedInt = AllocatedInt,
        AllocatedVit = AllocatedVit,
    };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Experience);
        hasher.Add(ExperienceToNext);
        hasher.Add(AttributePoints);
        hasher.Add(SkillPoints);
        hasher.Add(AllocatedStr);
        hasher.Add(AllocatedDex);
        hasher.Add(AllocatedAgi);
        hasher.Add(AllocatedInt);
        hasher.Add(AllocatedVit);
    }
}

/// <summary>
/// Atividade de combate de um personagem com progressão (T-203, interno ao servidor): último tick em que causou ou
/// recebeu dano e quem ele curou/protegeu por último (chave = id da entidade), para "fora de combate" e participação na XP.
/// </summary>
public sealed class CombatActivity : IComponent
{
    public const long Never = long.MinValue / 2;

    public long LastCombatTick { get; set; } = Never;

    public SortedDictionary<int, long> SupportedAt { get; } = new();

    public IComponent Clone()
    {
        var c = new CombatActivity { LastCombatTick = LastCombatTick };
        foreach (var kv in SupportedAt)
        {
            c.SupportedAt[kv.Key] = kv.Value;
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(LastCombatTick);
        foreach (var kv in SupportedAt)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }
    }
}

/// <summary>Personagens com progressão que causaram dano a um monstro (chave = id da entidade, valor = último tick). T-203, interno.</summary>
public sealed class XpContributors : IComponent
{
    public SortedDictionary<int, long> LastDamageAt { get; } = new();

    public IComponent Clone()
    {
        var c = new XpContributors();
        foreach (var kv in LastDamageAt)
        {
            c.LastDamageAt[kv.Key] = kv.Value;
        }

        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        foreach (var kv in LastDamageAt)
        {
            hasher.Add(kv.Key);
            hasher.Add(kv.Value);
        }
    }
}
