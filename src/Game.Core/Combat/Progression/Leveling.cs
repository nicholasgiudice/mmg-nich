using System.Numerics;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Progression;

/// <summary>
/// Estado salvo de um personagem com progressão (T-203): nível, XP no nível, pontos de atributo distribuídos e NH de cada
/// habilidade (ausente ou 0 = não aprendida). Os pontos livres são derivados: ganhos até o nível menos os gastos.
/// </summary>
public sealed record CharacterBuild
{
    public int Level { get; init; } = 1;
    public int Experience { get; init; }
    public AttributeSet Allocated { get; init; } = new();
    public IReadOnlyDictionary<string, int> SkillRanks { get; init; } = new Dictionary<string, int>();
}

/// <summary>
/// Níveis, XP e pontos (T-203, <c>design/progression-v0.1.md</c> §2, §3 e §7). Regras em <c>data/rules.json</c>, seção
/// <c>progression</c>. As intenções passam pelo <see cref="ProgressionSystem"/>; os métodos aqui servem também a servidor
/// e testes (criar o personagem a partir do que foi salvo, ler o que salvar).
/// </summary>
public static class Leveling
{
    /// <summary>Por que o build não vale para a classe; null se vale.</summary>
    public static string? Validate(ContentCatalog content, ClassDefinition cls, CharacterBuild build)
    {
        var rules = content.Rules.Progression;
        if (cls.Attributes is null)
        {
            return $"classe '{cls.Id}' sem atributos não tem progressão";
        }

        if (build.Level < 1 || build.Level > rules.MaxLevel)
        {
            return $"nível {build.Level} fora de 1..{rules.MaxLevel}";
        }

        var toNext = rules.XpToNext(build.Level);
        if (build.Experience < 0 || (build.Experience > 0 && build.Experience >= toNext))
        {
            return $"XP {build.Experience} inválida para o nível {build.Level}";
        }

        var a = build.Allocated;
        if (a.Str < 0 || a.Dex < 0 || a.Agi < 0 || a.Int < 0 || a.Vit < 0)
        {
            return "pontos de atributo negativos";
        }

        if (a.Str + a.Dex + a.Agi + a.Int + a.Vit > rules.AttributePointsAt(build.Level))
        {
            return "mais pontos de atributo do que o nível dá";
        }

        var spent = 0;
        foreach (var (id, rank) in build.SkillRanks)
        {
            if (rank == 0)
            {
                continue;
            }

            if (!IsLearnable(cls, id) || !content.Abilities.TryGetValue(id, out var ability))
            {
                return $"'{id}' não é habilidade da classe";
            }

            if (rank < 0 || rank > ability.HighestRank)
            {
                return $"NH {rank} fora de 0..{ability.HighestRank} em '{id}'";
            }

            if (build.Level < rules.RequiredLevel(ability.Signature, rank))
            {
                return $"NH {rank} de '{id}' exige nível {rules.RequiredLevel(ability.Signature, rank)}";
            }

            spent += rank;
        }

        return spent > rules.SkillPointsAt(build.Level) ? "mais pontos de habilidade do que o nível dá" : null;
    }

    /// <summary>
    /// Cria o personagem com progressão (nível 1 sem nada gasto se <paramref name="build"/> for null): só as habilidades com
    /// NH ≥ 1 entram na barra. Vida e Mana começam cheias. Lança <see cref="ArgumentException"/> se o build não vale.
    /// </summary>
    public static Entity SpawnCharacter(World world, ClassDefinition cls, Vector2 position, CharacterBuild? build = null)
    {
        build ??= new CharacterBuild();
        if (Validate(world.Content, cls, build) is { } error)
        {
            throw new ArgumentException($"Build inválido para {cls.Id}: {error}", nameof(build));
        }

        var rules = world.Content.Rules.Progression;
        var ranks = build.SkillRanks.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        var e = EntityFactory.SpawnPlayer(world, cls, position, ranks);
        RebuildKnown(cls, e.Get<AbilityState>());

        var stats = e.Get<CharacterStats>();
        stats.Level = build.Level;
        var progress = new CharacterProgress
        {
            Experience = build.Level >= rules.MaxLevel ? 0 : build.Experience,
            ExperienceToNext = rules.XpToNext(build.Level),
            SkillPoints = rules.SkillPointsAt(build.Level) - ranks.Values.Sum(),
        };
        foreach (var attr in Enum.GetValues<AttributeKind>())
        {
            var points = Get(build.Allocated, attr);
            progress.SetAllocated(attr, points);
            AddToStat(stats, attr, points);
        }

        progress.AttributePoints = rules.AttributePointsAt(build.Level) - progress.AllocatedTotal;
        e.Add(progress);
        e.Add(new CombatActivity());

        RefreshDerived(world, e);
        var health = e.Get<Health>();
        health.Current = health.Max;
        if (e.TryGet<ResourcePool>() is { } pool && cls.Resource.Initial >= cls.Resource.Max)
        {
            pool.Current = pool.Max;
        }

        return e;
    }

    /// <summary>O que salvar do personagem (inverso de <see cref="SpawnCharacter"/>); null se não tem progressão.</summary>
    public static CharacterBuild? BuildOf(Entity e)
    {
        if (e.TryGet<CharacterProgress>() is not { } p || e.TryGet<CharacterStats>() is not { } s || e.TryGet<AbilityState>() is not { } st)
        {
            return null;
        }

        return new CharacterBuild
        {
            Level = s.Level,
            Experience = p.Experience,
            Allocated = new AttributeSet { Str = p.AllocatedStr, Dex = p.AllocatedDex, Agi = p.AllocatedAgi, Int = p.AllocatedInt, Vit = p.AllocatedVit },
            SkillRanks = st.Known.Where(id => id != st.BasicAttackId).ToDictionary(id => id, st.RankOf, StringComparer.Ordinal),
        };
    }

    /// <summary>
    /// Dá XP ao personagem e sobe de nível quantas vezes couber (cada nível: pontos, vida e derivados). No nível máximo não
    /// acumula. Retorna a XP dada (0 se a entidade não tem progressão ou já está no máximo).
    /// </summary>
    public static int GrantExperience(World world, Entity e, int amount, EntityId monster, string sourceId)
    {
        var rules = world.Content.Rules.Progression;
        if (amount <= 0 || e.TryGet<CharacterProgress>() is not { } p || e.TryGet<CharacterStats>() is not { } s || s.Level >= rules.MaxLevel)
        {
            return 0;
        }

        p.Experience += amount;
        world.Events.Publish(new XpGained(e.Id, amount, monster, sourceId));
        while (s.Level < rules.MaxLevel && p.Experience >= p.ExperienceToNext)
        {
            p.Experience -= p.ExperienceToNext;
            s.Level++;
            p.AttributePoints += rules.AttributePointsPerLevel;
            p.SkillPoints += rules.SkillPointsPerLevel;
            p.ExperienceToNext = rules.XpToNext(s.Level);
            RefreshDerived(world, e);
            world.Events.Publish(new LevelUp(e.Id, s.Level, p.AttributePoints, p.SkillPoints));
        }

        if (s.Level >= rules.MaxLevel)
        {
            p.Experience = 0;
        }

        return amount;
    }

    /// <summary>Gasta um ponto livre no atributo. Null = feito; senão o motivo.</summary>
    public static ProgressionRejectReason? SpendAttributePoint(World world, Entity e, AttributeKind attribute)
    {
        if (e.TryGet<CharacterProgress>() is not { } p || e.TryGet<CharacterStats>() is not { } s)
        {
            return ProgressionRejectReason.NoProgression;
        }

        if (p.AttributePoints < 1)
        {
            return ProgressionRejectReason.NoAttributePoints;
        }

        p.AttributePoints--;
        p.SetAllocated(attribute, p.Allocated(attribute) + 1);
        AddToStat(s, attribute, 1);
        RefreshDerived(world, e);
        world.Events.Publish(new AttributeRaised(e.Id, attribute, Get(s, attribute)));
        return null;
    }

    /// <summary>Sobe o NH da habilidade (de 0 para 1 aprende e põe na barra). Null = feito; senão o motivo.</summary>
    public static ProgressionRejectReason? RaiseSkillRank(World world, Entity e, string abilityId)
    {
        if (e.TryGet<CharacterProgress>() is not { } p || e.TryGet<CharacterStats>() is not { } s
            || e.TryGet<AbilityState>() is not { } st || ClassOf(world, e) is not { } cls)
        {
            return ProgressionRejectReason.NoProgression;
        }

        if (!IsLearnable(cls, abilityId) || !world.Content.Abilities.TryGetValue(abilityId, out var ability))
        {
            return ProgressionRejectReason.NotLearnable;
        }

        var current = st.Known.Contains(abilityId) ? st.RankOf(abilityId) : 0;
        var next = current + 1;
        if (next > ability.HighestRank)
        {
            return ProgressionRejectReason.MaxRank;
        }

        if (p.SkillPoints < 1)
        {
            return ProgressionRejectReason.NoSkillPoints;
        }

        if (s.Level < world.Content.Rules.Progression.RequiredLevel(ability.Signature, next))
        {
            return ProgressionRejectReason.LevelTooLow;
        }

        p.SkillPoints--;
        st.Ranks[abilityId] = next;
        if (current == 0)
        {
            RebuildKnown(cls, st);
        }

        world.Events.Publish(new SkillRankRaised(e.Id, abilityId, next));
        return null;
    }

    /// <summary>
    /// Redistribuição (fase 2: grátis, só fora de combate): devolve os pontos de atributo e de habilidade; a barra fica só
    /// com o ataque básico. Null = feito; senão o motivo.
    /// </summary>
    public static ProgressionRejectReason? ResetPoints(World world, Entity e)
    {
        if (e.TryGet<CharacterProgress>() is not { } p || e.TryGet<CharacterStats>() is not { } s
            || e.TryGet<AbilityState>() is not { } st || ClassOf(world, e) is not { } cls)
        {
            return ProgressionRejectReason.NoProgression;
        }

        if (st.IsCasting || InCombat(world, e))
        {
            return ProgressionRejectReason.InCombat;
        }

        var rules = world.Content.Rules.Progression;
        foreach (var attr in Enum.GetValues<AttributeKind>())
        {
            AddToStat(s, attr, -p.Allocated(attr));
            p.SetAllocated(attr, 0);
        }

        p.AttributePoints = rules.AttributePointsAt(s.Level);
        p.SkillPoints = rules.SkillPointsAt(s.Level);
        st.Ranks.Clear();
        RebuildKnown(cls, st);
        RefreshDerived(world, e);
        world.Events.Publish(new PointsReset(e.Id, p.AttributePoints, p.SkillPoints));
        return null;
    }

    /// <summary>Causou ou recebeu dano (ou curou/protegeu quem lutava) há menos de <see cref="ProgressionRules.CombatTimeoutTicks"/>.</summary>
    public static bool InCombat(World world, Entity e) =>
        e.TryGet<CombatActivity>() is { } a && world.Tick - a.LastCombatTick < world.Content.Rules.Progression.CombatTimeoutTicks;

    /// <summary>
    /// Recalcula vida e Mana máximas a partir das estatísticas (nível, VIT, INT). O atual acompanha a diferença do máximo
    /// (subir VIT dá a vida nova; redistribuir tira), sem matar nem passar do máximo.
    /// </summary>
    public static void RefreshDerived(World world, Entity e)
    {
        if (e.TryGet<CharacterStats>() is not { } s || ClassOf(world, e) is not { } cls)
        {
            return;
        }

        var rules = world.Content.Rules.Stats;
        var health = e.Get<Health>();
        var maxHealth = (float)StatMath.MaxHealth(cls, s, rules);
        var delta = maxHealth - health.Max;
        health.Max = maxHealth;
        if (health.IsAlive)
        {
            health.Current = Math.Clamp(health.Current + delta, 1, maxHealth);
        }

        if (e.TryGet<ResourcePool>() is { Type: EntityFactory.ManaResource } pool)
        {
            var maxMana = (float)StatMath.MaxMana(s, rules);
            pool.Current = Math.Clamp(pool.Current + (maxMana - pool.Max), 0, maxMana);
            pool.Max = maxMana;
        }
    }

    /// <summary>Habilidades de classe que recebem pontos: as da classe, menos o ataque básico.</summary>
    public static bool IsLearnable(ClassDefinition cls, string abilityId) =>
        abilityId != cls.BasicAttack && cls.Abilities.Contains(abilityId);

    private static ClassDefinition? ClassOf(World world, Entity e) =>
        e.TryGet<UnitInfo>() is { } info && world.Content.Classes.TryGetValue(info.DefinitionId, out var cls) ? cls : null;

    /// <summary>Barra: ataque básico + habilidades da classe com NH ≥ 1, na ordem da classe.</summary>
    private static void RebuildKnown(ClassDefinition cls, AbilityState st)
    {
        st.Known.Clear();
        st.Known.Add(cls.BasicAttack);
        foreach (var id in cls.Abilities)
        {
            if (IsLearnable(cls, id) && st.Ranks.TryGetValue(id, out var rank) && rank >= 1 && !st.Known.Contains(id))
            {
                st.Known.Add(id);
            }
        }
    }

    private static int Get(AttributeSet a, AttributeKind attr) => attr switch
    {
        AttributeKind.Str => a.Str,
        AttributeKind.Dex => a.Dex,
        AttributeKind.Agi => a.Agi,
        AttributeKind.Int => a.Int,
        _ => a.Vit,
    };

    private static int Get(CharacterStats s, AttributeKind attr) => attr switch
    {
        AttributeKind.Str => s.Str,
        AttributeKind.Dex => s.Dex,
        AttributeKind.Agi => s.Agi,
        AttributeKind.Int => s.Int,
        _ => s.Vit,
    };

    private static void AddToStat(CharacterStats s, AttributeKind attr, int amount)
    {
        switch (attr)
        {
            case AttributeKind.Str: s.Str += amount; break;
            case AttributeKind.Dex: s.Dex += amount; break;
            case AttributeKind.Agi: s.Agi += amount; break;
            case AttributeKind.Int: s.Int += amount; break;
            default: s.Vit += amount; break;
        }
    }
}
