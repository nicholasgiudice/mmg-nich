using System.Numerics;
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
    /// dividida entre todos com o bônus de grupo. Cada um recebe com o modificador do próprio nível. Os mesmos
    /// participantes têm direito à queda (T-204). T-310: os membros do grupo de algum participante que estão perto também
    /// entram na divisão e no saque.
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
        members = WithParty(world, monster, members);
        foreach (var e in members)
        {
            var level = Stats.StatMath.LevelOf(e);
            var xp = rules.Share(rules.MonsterXp(info.Level, info.Rank, level), members.Count);
            Leveling.GrantExperience(world, e, xp, monster.Id, info.DefinitionId);
        }

        Items.Gear.DropLoot(world, monster, members);
        DropGold(world, monster, info, members);
    }

    /// <summary>
    /// T-320: ouro da morte = porNível × nível × categoria, com variação de ± <c>goldVariance</c> (um sorteio do RNG da
    /// simulação, depois da queda de itens). Dividido igualmente entre quem recebeu a XP, arredondando para baixo; o resto
    /// se perde. Vai direto para a <see cref="Items.Wallet"/>. Sem ninguém (ou sem ouro configurado) não consome o RNG.
    /// </summary>
    private static void DropGold(World world, Entity monster, UnitInfo info, IReadOnlyList<Entity> members)
    {
        var rules = world.Content.Rules.Progression;
        var baseGold = rules.GoldPerMonsterLevel * info.Level * rules.GoldByRank.For(info.Rank);
        if (members.Count == 0 || baseGold <= 0)
        {
            return;
        }

        var variance = Math.Clamp(rules.GoldVariance, 0, 1);
        var roll = (world.Random.NextFloat() * 2) - 1;
        var total = (int)Math.Round(baseGold * (1 + (variance * roll)), MidpointRounding.AwayFromZero);
        var share = total / members.Count;
        if (share <= 0)
        {
            return;
        }

        foreach (var e in members)
        {
            if (e.TryGet<Items.Wallet>() is { } wallet)
            {
                wallet.Gold += share;
                world.Events.Publish(new Items.GoldGained(e.Id, share, monster.Id));
            }
        }
    }

    /// <summary>
    /// T-310: soma aos participantes os membros dos grupos deles que estão vivos, nesta zona (este mundo) e a até
    /// <see cref="Definitions.ProgressionRules.PartyShareRange"/> do monstro. Ordem por id da entidade (determinística).
    /// </summary>
    private static List<Entity> WithParty(World world, Entity monster, List<Entity> participants)
    {
        var parties = participants.Select(e => e.TryGet<PartyMembership>()?.PartyId ?? 0).Where(id => id != 0).ToHashSet();
        if (parties.Count == 0 || monster.TryGet<Transform>() is not { } at)
        {
            return participants;
        }

        var range = world.Content.Rules.Progression.PartyShareRange;
        var ids = participants.Select(e => e.Id.Value).ToHashSet();
        var all = new List<Entity>(participants);
        foreach (var e in world.Entities)
        {
            if (!ids.Contains(e.Id.Value) && !e.IsRemoved && e.IsAlive && e.Has<CharacterProgress>()
                && e.TryGet<PartyMembership>() is { } m && parties.Contains(m.PartyId)
                && e.TryGet<Transform>() is { } t && Vector2.Distance(t.Position, at.Position) <= range)
            {
                all.Add(e);
            }
        }

        return all.OrderBy(e => e.Id.Value).ToList();
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
