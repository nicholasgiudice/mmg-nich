using System.Buffers;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Resources;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Events;
using MemoryPack;

namespace Game.Protocol;

/// <summary>
/// Eventos do Core na rede (T-103): uma tag fixa por tipo, o tick do evento e os campos na ordem do construtor.
/// Tags nunca mudam nem são reaproveitadas; evento novo = tag nova e <see cref="ProtocolVersion"/> + 1.
/// Os testes falham se um <see cref="GameEvent"/> do Core não estiver aqui.
/// </summary>
public static class EventCodecs
{
    public static readonly IReadOnlyDictionary<Type, ushort> Tags = new Dictionary<Type, ushort>
    {
        [typeof(EntitySpawned)] = 1,
        [typeof(EntityDied)] = 2,
        [typeof(EntityRemoved)] = 3,
        [typeof(EntityRevived)] = 4,
        [typeof(AbilityStarted)] = 10,
        [typeof(AbilityExecuted)] = 11,
        [typeof(AbilityRejected)] = 12,
        [typeof(AbilityHit)] = 13,
        [typeof(CastInterrupted)] = 14,
        [typeof(DamageDealt)] = 20,
        [typeof(HealApplied)] = 21,
        [typeof(ShieldApplied)] = 22,
        [typeof(ShieldExpired)] = 23,
        [typeof(Displaced)] = 24,
        [typeof(ChainJumped)] = 25,
        [typeof(StatusApplied)] = 30,
        [typeof(StatusRefreshed)] = 31,
        [typeof(StatusStacked)] = 32,
        [typeof(StatusExpired)] = 33,
        [typeof(StatusRemoved)] = 34,
        [typeof(StatusResisted)] = 35,
        [typeof(ResourceChanged)] = 40,
        [typeof(AggroGained)] = 50,
        [typeof(TargetChanged)] = 51,
        [typeof(Taunted)] = 52,
        [typeof(TelegraphStarted)] = 60,
        [typeof(TelegraphResolved)] = 61,
        [typeof(AreaSpawned)] = 70,
        [typeof(AreaArmed)] = 71,
        [typeof(AreaTriggered)] = 72,
        [typeof(AreaExpired)] = 73,
        [typeof(MonsterSummoned)] = 74,
        [typeof(PhaseChanged)] = 80,
        [typeof(XpGained)] = 90,
        [typeof(LevelUp)] = 91,
        [typeof(AttributeRaised)] = 92,
        [typeof(SkillRankRaised)] = 93,
        [typeof(PointsReset)] = 94,
        [typeof(ProgressionRejected)] = 95,
    };

    internal static void Write<TB>(ref MemoryPackWriter<TB> w, GameEvent e)
        where TB : IBufferWriter<byte>
    {
        if (!Tags.TryGetValue(e.GetType(), out var tag))
        {
            throw new MemoryPackSerializationException($"Evento sem formato de rede: {e.GetType().Name}");
        }

        w.WriteVarInt(tag);
        w.L(e.Tick);
        switch (e)
        {
            case EntitySpawned x: w.Id(x.Entity); break;
            case EntityDied x: w.Id(x.Entity); break;
            case EntityRemoved x: w.Id(x.Entity); break;
            case EntityRevived x: w.Id(x.Entity); w.Id(x.By); w.S(x.SourceId); w.F(x.Health); break;

            case AbilityStarted x:
                w.Id(x.Source); w.S(x.AbilityId); w.I(x.CastTicks); w.L(x.EndTick); w.IdOpt(x.Target); w.VOpt(x.AimPoint);
                break;
            case AbilityExecuted x:
                w.Id(x.Source); w.S(x.AbilityId); w.Ids(x.Targets); w.S(x.CostResource); w.F(x.CostAmount);
                w.B(x.IsBasicAttack); w.L(x.CooldownReadyTick); w.IOpt(x.ChargesRemaining);
                break;
            case AbilityRejected x: w.Id(x.Source); w.S(x.AbilityId); w.E(x.Reason); break;
            case AbilityHit x: w.Id(x.Source); w.S(x.AbilityId); w.I(x.HitIndex); w.I(x.HitCount); w.Ids(x.Targets); break;
            case CastInterrupted x: w.Id(x.Entity); w.S(x.AbilityId); w.E(x.Reason); w.IdOpt(x.By); break;

            case DamageDealt x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.SourceId); w.E(x.School); w.F(x.Amount); w.F(x.Absorbed);
                w.F(x.Applied); w.F(x.Overkill); w.B(x.Killed); w.B(x.Critical);
                break;
            case HealApplied x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.SourceId); w.F(x.Amount); w.F(x.Effective); w.F(x.Overheal); w.B(x.Critical);
                break;
            case ShieldApplied x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.SourceId); w.I(x.ShieldId); w.F(x.Amount); w.L(x.ExpiresAtTick); w.B(x.Critical);
                break;
            case ShieldExpired x: w.Id(x.Entity); w.I(x.ShieldId); w.F(x.Remaining); w.E(x.Reason); break;
            case Displaced x:
                w.Id(x.Source); w.Id(x.Entity); w.S(x.SourceId); w.E(x.Kind); w.V(x.From); w.V(x.To); w.F(x.Distance);
                break;
            case ChainJumped x: w.Id(x.Source); w.Id(x.From); w.Id(x.To); w.S(x.SourceId); w.I(x.Jump); break;

            case StatusApplied x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.StatusId); w.I(x.InstanceId); w.I(x.Stacks); w.L(x.ExpiresAtTick);
                w.I(x.DurationTicks);
                break;
            case StatusRefreshed x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.StatusId); w.I(x.InstanceId); w.I(x.Stacks); w.L(x.ExpiresAtTick);
                break;
            case StatusStacked x:
                w.Id(x.Source); w.Id(x.Target); w.S(x.StatusId); w.I(x.InstanceId); w.I(x.Stacks); w.L(x.ExpiresAtTick);
                break;
            case StatusExpired x: w.Id(x.Entity); w.S(x.StatusId); w.I(x.InstanceId); break;
            case StatusRemoved x: w.Id(x.Entity); w.S(x.StatusId); w.I(x.InstanceId); w.Id(x.By); break;
            case StatusResisted x: w.Id(x.Source); w.Id(x.Target); w.S(x.StatusId); break;

            case ResourceChanged x:
                w.Id(x.Entity); w.S(x.Resource); w.F(x.Delta); w.F(x.Current); w.F(x.Max); w.E(x.Reason); w.S(x.SourceId);
                break;

            case AggroGained x: w.Id(x.Monster); w.Id(x.Target); w.E(x.Reason); break;
            case TargetChanged x: w.Id(x.Monster); w.Id(x.Previous); w.Id(x.Current); break;
            case Taunted x: w.Id(x.Taunter); w.Id(x.Monster); w.L(x.UntilTick); break;

            case TelegraphStarted x:
                w.Id(x.Telegraph); w.Id(x.Source); w.S(x.AbilityId); w.E(x.Shape); w.V(x.Origin); w.V(x.Direction);
                w.F(x.Radius); w.F(x.Length); w.F(x.Width); w.F(x.AngleDegrees); w.L(x.ResolveAtTick);
                break;
            case TelegraphResolved x: w.Id(x.Telegraph); w.Id(x.Source); w.S(x.AbilityId); w.Ids(x.Targets); w.B(x.Cancelled); break;

            case AreaSpawned x:
                w.Id(x.Area); w.S(x.AreaId); w.Id(x.Owner); w.V(x.Position); w.F(x.Radius); w.L(x.ExpiresAtTick); w.L(x.ArmedAtTick);
                break;
            case AreaArmed x: w.Id(x.Area); w.S(x.AreaId); w.Id(x.Owner); break;
            case AreaTriggered x: w.Id(x.Area); w.S(x.AreaId); w.Id(x.Owner); w.Ids(x.Targets); break;
            case AreaExpired x: w.Id(x.Area); w.S(x.AreaId); w.E(x.Reason); break;
            case MonsterSummoned x: w.Id(x.Owner); w.Id(x.Summon); w.S(x.MonsterId); w.V(x.Position); break;

            case PhaseChanged x:
                w.Id(x.Entity); w.S(x.MonsterId); w.S(x.Phase); w.I(x.Previous); w.I(x.Index); w.F(x.HealthPercent);
                break;

            case XpGained x: w.Id(x.Entity); w.I(x.Amount); w.Id(x.Monster); w.S(x.SourceId); break;
            case LevelUp x: w.Id(x.Entity); w.I(x.Level); w.I(x.AttributePoints); w.I(x.SkillPoints); break;
            case AttributeRaised x: w.Id(x.Entity); w.E(x.Attribute); w.I(x.Value); break;
            case SkillRankRaised x: w.Id(x.Entity); w.S(x.AbilityId); w.I(x.Rank); break;
            case PointsReset x: w.Id(x.Entity); w.I(x.AttributePoints); w.I(x.SkillPoints); break;
            case ProgressionRejected x: w.Id(x.Entity); w.S(x.Id); w.E(x.Reason); break;
        }
    }

    /// <summary>Lê um evento; tag 0 = null (lista com buraco, nunca enviado pelo servidor).</summary>
    internal static GameEvent? Read(ref MemoryPackReader r)
    {
        var tag = r.ReadVarIntUInt16();
        if (tag == 0)
        {
            return null;
        }

        var tick = r.L();
        GameEvent e = tag switch
        {
            1 => new EntitySpawned(r.Id()),
            2 => new EntityDied(r.Id()),
            3 => new EntityRemoved(r.Id()),
            4 => new EntityRevived(r.Id(), r.Id(), r.S(), r.F()),

            10 => new AbilityStarted(r.Id(), r.S(), r.I(), r.L(), r.IdOpt(), r.VOpt()),
            11 => new AbilityExecuted(r.Id(), r.S(), r.Ids(), r.SOpt(), r.F(), r.B(), r.L(), r.IOpt()),
            12 => new AbilityRejected(r.Id(), r.S(), r.E<AbilityRejectReason>()),
            13 => new AbilityHit(r.Id(), r.S(), r.I(), r.I(), r.Ids()),
            14 => new CastInterrupted(r.Id(), r.S(), r.E<CastInterruptReason>(), r.IdOpt()),

            20 => new DamageDealt(r.Id(), r.Id(), r.S(), r.E<DamageSchool>(), r.F(), r.F(), r.F(), r.F(), r.B(), r.B()),
            21 => new HealApplied(r.Id(), r.Id(), r.S(), r.F(), r.F(), r.F(), r.B()),
            22 => new ShieldApplied(r.Id(), r.Id(), r.S(), r.I(), r.F(), r.L(), r.B()),
            23 => new ShieldExpired(r.Id(), r.I(), r.F(), r.E<ShieldEndReason>()),
            24 => new Displaced(r.Id(), r.Id(), r.S(), r.E<DisplacementKind>(), r.V(), r.V(), r.F()),
            25 => new ChainJumped(r.Id(), r.Id(), r.Id(), r.S(), r.I()),

            30 => new StatusApplied(r.Id(), r.Id(), r.S(), r.I(), r.I(), r.L(), r.I()),
            31 => new StatusRefreshed(r.Id(), r.Id(), r.S(), r.I(), r.I(), r.L()),
            32 => new StatusStacked(r.Id(), r.Id(), r.S(), r.I(), r.I(), r.L()),
            33 => new StatusExpired(r.Id(), r.S(), r.I()),
            34 => new StatusRemoved(r.Id(), r.S(), r.I(), r.Id()),
            35 => new StatusResisted(r.Id(), r.Id(), r.S()),

            40 => new ResourceChanged(r.Id(), r.S(), r.F(), r.F(), r.F(), r.E<ResourceChangeReason>(), r.S()),

            50 => new AggroGained(r.Id(), r.Id(), r.E<AggroReason>()),
            51 => new TargetChanged(r.Id(), r.Id(), r.Id()),
            52 => new Taunted(r.Id(), r.Id(), r.L()),

            60 => new TelegraphStarted(r.Id(), r.Id(), r.S(), r.E<TelegraphShape>(), r.V(), r.V(), r.F(), r.F(), r.F(), r.F(), r.L()),
            61 => new TelegraphResolved(r.Id(), r.Id(), r.S(), r.Ids(), r.B()),

            70 => new AreaSpawned(r.Id(), r.S(), r.Id(), r.V(), r.F(), r.L(), r.L()),
            71 => new AreaArmed(r.Id(), r.S(), r.Id()),
            72 => new AreaTriggered(r.Id(), r.S(), r.Id(), r.Ids()),
            73 => new AreaExpired(r.Id(), r.S(), r.E<AreaExpireReason>()),
            74 => new MonsterSummoned(r.Id(), r.Id(), r.S(), r.V()),

            80 => new PhaseChanged(r.Id(), r.S(), r.S(), r.I(), r.I(), r.F()),

            90 => new XpGained(r.Id(), r.I(), r.Id(), r.S()),
            91 => new LevelUp(r.Id(), r.I(), r.I(), r.I()),
            92 => new AttributeRaised(r.Id(), r.E<AttributeKind>(), r.I()),
            93 => new SkillRankRaised(r.Id(), r.S(), r.I()),
            94 => new PointsReset(r.Id(), r.I(), r.I()),
            95 => new ProgressionRejected(r.Id(), r.S(), r.E<ProgressionRejectReason>()),
            _ => throw new MemoryPackSerializationException($"Tag de evento desconhecida: {tag}"),
        };
        e.Tick = tick;
        return e;
    }
}
