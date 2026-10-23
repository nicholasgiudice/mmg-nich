using Game.Core.CombatLog;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Progression;

/// <summary>
/// Progressão (T-203, ordem <see cref="SystemOrder.Progression"/>, depois da morte): lê o dano, a cura e o escudo do tick
/// para saber quem lutou e quem participou de cada monstro, dá XP pelas mortes e trata as intenções de pontos.
/// Só personagens com <see cref="CharacterProgress"/> entram: encontros sem progressão não mudam em nada.
/// </summary>
public sealed class ProgressionSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var ev in world.Events.Current.ToList())
        {
            switch (ev)
            {
                case DamageDealt { Amount: > 0 } d:
                    TrackDamage(world, d);
                    break;
                case HealApplied { Amount: > 0 } h:
                    TrackSupport(world, h.Source, h.Target);
                    break;
                case ShieldApplied { Amount: > 0 } s:
                    TrackSupport(world, s.Source, s.Target);
                    break;
                case Events.EntityDied died:
                    AwardKill(world, died.Entity);
                    break;
            }
        }

        foreach (var intent in world.TickIntents)
        {
            switch (intent)
            {
                case SpendAttributePointIntent spend:
                    Handle(world, spend.Entity, spend.Attribute.ToString().ToLowerInvariant(), e => Leveling.SpendAttributePoint(world, e, spend.Attribute));
                    break;
                case RaiseSkillRankIntent raise:
                    Handle(world, raise.Entity, raise.AbilityId, e => Leveling.RaiseSkillRank(world, e, raise.AbilityId));
                    break;
                case ResetPointsIntent reset:
                    Handle(world, reset.Entity, "", e => Leveling.ResetPoints(world, e));
                    break;
            }
        }
    }

    private static void Handle(World world, EntityId id, string what, Func<Entity, ProgressionRejectReason?> action)
    {
        if (world.Find(id) is { } e && action(e) is { } reason)
        {
            world.Events.Publish(new ProgressionRejected(id, what, reason));
        }
    }

    private static void TrackDamage(World world, DamageDealt d)
    {
        var source = world.Find(d.Source);
        var target = world.Find(d.Target);
        if (source?.TryGet<CombatActivity>() is { } dealer)
        {
            dealer.LastCombatTick = world.Tick;
            if (target?.TryGet<FactionTag>() is { Faction: Faction.Monster })
            {
                var contributors = target.TryGet<XpContributors>();
                if (contributors is null)
                {
                    contributors = new XpContributors();
                    target.Add(contributors);
                }

                contributors.LastDamageAt[source.Id.Value] = world.Tick;
            }
        }

        if (target?.TryGet<CombatActivity>() is { } victim)
        {
            victim.LastCombatTick = world.Tick;
        }
    }

    /// <summary>Cura/escudo em outro personagem: conta para a XP dos monstros em que ele bateu; se ele está lutando, quem ajudou também está.</summary>
    private static void TrackSupport(World world, EntityId sourceId, EntityId targetId)
    {
        if (sourceId == targetId || world.Find(sourceId)?.TryGet<CombatActivity>() is not { } helper
            || world.Find(targetId) is not { } target || !target.Has<CharacterProgress>())
        {
            return;
        }

        var window = world.Content.Rules.Progression.ParticipationWindowTicks;
        foreach (var stale in helper.SupportedAt.Where(kv => world.Tick - kv.Value > window).Select(kv => kv.Key).ToList())
        {
            helper.SupportedAt.Remove(stale);
        }

        helper.SupportedAt[targetId.Value] = world.Tick;
        if (Leveling.InCombat(world, target))
        {
            helper.LastCombatTick = world.Tick;
        }
    }

    /// <summary>
    /// XP do monstro morto para quem causou dano nele dentro da janela e para quem curou/protegeu um desses na janela,
    /// dividida entre todos com o bônus de grupo. Cada um recebe com o modificador do próprio nível.
    /// </summary>
    private static void AwardKill(World world, EntityId monsterId)
    {
        if (world.Find(monsterId) is not { } monster || monster.TryGet<XpContributors>() is not { } contributors
            || monster.TryGet<UnitInfo>() is not { } info)
        {
            return;
        }

        monster.Remove<XpContributors>();
        var rules = world.Content.Rules.Progression;
        var from = world.Tick - rules.ParticipationWindowTicks;
        var damagers = contributors.LastDamageAt.Where(kv => kv.Value >= from).Select(kv => kv.Key).ToHashSet();
        var participants = new SortedSet<int>(damagers);
        foreach (var e in world.Entities)
        {
            if (e.TryGet<CombatActivity>() is { } a && a.SupportedAt.Any(kv => kv.Value >= from && damagers.Contains(kv.Key)))
            {
                participants.Add(e.Id.Value);
            }
        }

        var members = participants
            .Select(id => world.Find(new EntityId(id)))
            .Where(e => e is { IsRemoved: false } && e.Has<CharacterProgress>())
            .Select(e => e!)
            .ToList();
        foreach (var e in members)
        {
            var level = Stats.StatMath.LevelOf(e);
            var xp = rules.Share(rules.MonsterXp(info.Level, info.Rank, level), members.Count);
            Leveling.GrantExperience(world, e, xp, monster.Id, info.DefinitionId);
        }
    }
}

/// <summary>Formatadores de log dos eventos de progressão (T-203).</summary>
public static class ProgressionLog
{
    public static CombatLogFormatters Register(CombatLogFormatters f)
    {
        f.Register<XpGained>((c, e) => c.Entry(e, LogCategory.Progressao, $"{c.Name(e.Entity)} ganhou {e.Amount} de XP", e.Entity, e.Monster));
        f.Register<LevelUp>((c, e) => c.Entry(
            e,
            LogCategory.Progressao,
            $"{c.Name(e.Entity)} subiu para o nível {e.Level} ({e.AttributePoints} ponto(s) de atributo, {e.SkillPoints} de habilidade)",
            e.Entity));
        f.Register<AttributeRaised>((c, e) => c.Entry(
            e, LogCategory.Progressao, $"{c.Name(e.Entity)}: {AttributeName(e.Attribute)} {e.Value}", e.Entity));
        f.Register<SkillRankRaised>((c, e) => c.Entry(
            e,
            LogCategory.Progressao,
            e.Rank == 1 ? $"{c.Name(e.Entity)} aprendeu {c.SourceLabel(e.AbilityId)}" : $"{c.Name(e.Entity)}: {c.SourceLabel(e.AbilityId)} NH{e.Rank}",
            e.Entity));
        f.Register<PointsReset>((c, e) => c.Entry(e, LogCategory.Progressao, $"{c.Name(e.Entity)} redistribuiu os pontos", e.Entity));
        f.Register<ProgressionRejected>((c, e) => c.Entry(e, LogCategory.Progressao, $"{c.Name(e.Entity)}: {RejectText(e.Reason)}", e.Entity));
        return f;
    }

    public static string AttributeName(Definitions.AttributeKind a) => a switch
    {
        Definitions.AttributeKind.Str => "Força",
        Definitions.AttributeKind.Dex => "Destreza",
        Definitions.AttributeKind.Agi => "Agilidade",
        Definitions.AttributeKind.Int => "Inteligência",
        _ => "Vitalidade",
    };

    private static string RejectText(ProgressionRejectReason r) => r switch
    {
        ProgressionRejectReason.NoProgression => "sem progressão",
        ProgressionRejectReason.NoAttributePoints => "sem pontos de atributo",
        ProgressionRejectReason.NoSkillPoints => "sem pontos de habilidade",
        ProgressionRejectReason.NotLearnable => "habilidade não pode receber pontos",
        ProgressionRejectReason.MaxRank => "habilidade já no nível máximo",
        ProgressionRejectReason.LevelTooLow => "nível insuficiente para o próximo NH",
        _ => "não dá para redistribuir em combate",
    };
}
