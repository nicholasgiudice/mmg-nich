using System.Buffers;
using Game.Core.Combat;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Stats;
using Game.Core.Combat.Status;
using Game.Core.Combat.Telegraphs;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using MemoryPack;

namespace Game.Protocol;

/// <summary>
/// Componentes replicáveis (<see cref="IReplicated"/>) na rede (T-103): uma tag fixa por tipo e os campos na ordem
/// abaixo. Tags nunca mudam nem são reaproveitadas; tipo novo = tag nova e <see cref="ProtocolVersion"/> + 1.
/// Os testes falham se um componente replicável não estiver aqui.
/// </summary>
public static class ComponentCodecs
{
    /// <summary>Tag de cada tipo de componente.</summary>
    public static readonly IReadOnlyDictionary<Type, byte> Tags = new Dictionary<Type, byte>
    {
        [typeof(Transform)] = 1,
        [typeof(Health)] = 2,
        [typeof(FactionTag)] = 3,
        [typeof(ResourcePool)] = 4,
        [typeof(Movement)] = 5,
        [typeof(Collider)] = 6,
        [typeof(UnitInfo)] = 7,
        [typeof(AbilityState)] = 8,
        [typeof(ShieldState)] = 9,
        [typeof(StatusEffects)] = 10,
        [typeof(TelegraphState)] = 11,
        [typeof(AreaState)] = 12,
        [typeof(PhaseState)] = 13,
        [typeof(CharacterStats)] = 14,
    };

    internal static void Write<TB>(ref MemoryPackWriter<TB> w, IComponent component)
        where TB : IBufferWriter<byte>
    {
        if (!Tags.TryGetValue(component.GetType(), out var tag))
        {
            throw new MemoryPackSerializationException($"Componente sem formato de rede: {component.GetType().Name}");
        }

        w.WriteUnmanaged(tag);
        switch (component)
        {
            case Transform t:
                w.V(t.Position);
                w.V(t.Facing);
                break;
            case Health h:
                w.F(h.Current);
                w.F(h.Max);
                w.B(h.IsAlive);
                break;
            case FactionTag f:
                w.E(f.Faction);
                break;
            case ResourcePool p:
                w.S(p.Type);
                w.F(p.Max);
                w.F(p.Current);
                break;
            case Movement m:
                w.F(m.Speed);
                w.V(m.Direction);
                w.B(m.LockFacing);
                break;
            case Collider c:
                w.F(c.Radius);
                break;
            case UnitInfo u:
                w.S(u.DefinitionId);
                w.S(u.Name);
                w.Strs(u.Tags);
                w.E(u.Rank);
                w.I(u.Resistances.Count);
                foreach (var (school, value) in u.Resistances.OrderBy(kv => kv.Key, StringComparer.Ordinal))
                {
                    w.S(school);
                    w.D(value);
                }

                w.D(u.Armor);
                w.D(u.Resistance);
                break;
            case AbilityState a:
                WriteAbilities(ref w, a);
                break;
            case ShieldState s:
                w.I(s.NextShieldId);
                w.I(s.Shields.Count);
                foreach (var sh in s.Shields)
                {
                    w.I(sh.Id);
                    w.F(sh.Remaining);
                    w.L(sh.ExpiresAtTick);
                    w.Id(sh.Source);
                    w.S(sh.SourceId);
                    w.S(sh.RefundResource);
                    w.F(sh.RefundFraction);
                }

                break;
            case StatusEffects fx:
                w.I(fx.NextInstanceId);
                w.I(fx.ControlCount);
                w.L(fx.ControlWindowEndTick);
                w.I(fx.Active.Count);
                foreach (var st in fx.Active)
                {
                    w.I(st.Id);
                    w.S(st.StatusId);
                    w.Id(st.Source);
                    w.L(st.AppliedAtTick);
                    w.L(st.ExpiresAtTick);
                    w.I(st.Stacks);
                    w.I(st.NextPeriodicTick.Length);
                    foreach (var n in st.NextPeriodicTick)
                    {
                        w.L(n);
                    }
                }

                break;
            case TelegraphState t:
                w.Id(t.Source);
                w.S(t.AbilityId);
                w.I(t.Rank);
                w.D(t.Power);
                w.I(t.HitIndex);
                w.E(t.Shape);
                w.F(t.Radius);
                w.F(t.Length);
                w.F(t.Width);
                w.F(t.AngleDegrees);
                w.E(t.Affects);
                w.B(t.Charge);
                w.I(t.SourceFaction.HasValue ? (int)t.SourceFaction.Value : -1);
                w.L(t.StartTick);
                w.L(t.ResolveAtTick);
                break;
            case AreaState a:
                w.S(a.AreaId);
                w.Id(a.Owner);
                w.I(a.OwnerFaction.HasValue ? (int)a.OwnerFaction.Value : -1);
                w.F(a.Radius);
                w.E(a.Mode);
                w.E(a.Affects);
                w.L(a.CreatedTick);
                w.L(a.ArmedAtTick);
                w.B(a.ArmedAnnounced);
                w.L(a.ExpiresAtTick);
                w.L(a.NextApplyTick);
                break;
            case PhaseState p:
                w.I(p.Index);
                w.L(p.EnteredTick);
                break;
            case CharacterStats s:
                w.I(s.Level);
                w.I(s.Str);
                w.I(s.Dex);
                w.I(s.Agi);
                w.I(s.Int);
                w.I(s.Vit);
                w.F(s.WeaponDamage);
                w.F(s.Armor);
                w.F(s.Resistance);
                break;
        }
    }

    internal static IComponent Read(ref MemoryPackReader r)
    {
        var tag = r.ReadUnmanaged<byte>();
        switch (tag)
        {
            case 1:
                return new Transform { Position = r.V(), Facing = r.V() };
            case 2:
                {
                    var current = r.F();
                    var health = new Health(r.F()) { Current = current };
                    health.IsAlive = r.B();
                    return health;
                }

            case 3:
                return new FactionTag(r.E<Faction>());
            case 4:
                {
                    var type = r.S();
                    var max = r.F();
                    return new ResourcePool(type, max, r.F());
                }

            case 5:
                return new Movement(r.F()) { Direction = r.V(), LockFacing = r.B() };
            case 6:
                return new Collider(r.F());
            case 7:
                {
                    var id = r.S();
                    var name = r.S();
                    var tags = r.Strs();
                    var rank = r.E<MonsterRank>();
                    var n = Wire.Count(ref r);
                    var res = new Dictionary<string, double>(n, StringComparer.Ordinal);
                    for (var i = 0; i < n; i++)
                    {
                        res[r.S()] = r.D();
                    }

                    return new UnitInfo { DefinitionId = id, Name = name, Tags = tags, Rank = rank, Resistances = res, Armor = r.D(), Resistance = r.D() };
                }

            case 8:
                return ReadAbilities(ref r);
            case 9:
                {
                    var s = new ShieldState { NextShieldId = r.I() };
                    var n = Wire.Count(ref r);
                    for (var i = 0; i < n; i++)
                    {
                        s.Shields.Add(new ShieldInstance
                        {
                            Id = r.I(),
                            Remaining = r.F(),
                            ExpiresAtTick = r.L(),
                            Source = r.Id(),
                            SourceId = r.S(),
                            RefundResource = r.SOpt(),
                            RefundFraction = r.F(),
                        });
                    }

                    return s;
                }

            case 10:
                {
                    var fx = new StatusEffects { NextInstanceId = r.I(), ControlCount = r.I(), ControlWindowEndTick = r.L() };
                    var n = Wire.Count(ref r);
                    for (var i = 0; i < n; i++)
                    {
                        var st = new StatusInstance { Id = r.I(), StatusId = r.S(), Source = r.Id() };
                        st.AppliedAtTick = r.L();
                        st.ExpiresAtTick = r.L();
                        st.Stacks = r.I();
                        var periodic = new long[Wire.Count(ref r)];
                        for (var k = 0; k < periodic.Length; k++)
                        {
                            periodic[k] = r.L();
                        }

                        st.NextPeriodicTick = periodic;
                        fx.Active.Add(st);
                    }

                    return fx;
                }

            case 11:
                return new TelegraphState
                {
                    Source = r.Id(),
                    AbilityId = r.S(),
                    Rank = r.I(),
                    Power = r.D(),
                    HitIndex = r.I(),
                    Shape = r.E<TelegraphShape>(),
                    Radius = r.F(),
                    Length = r.F(),
                    Width = r.F(),
                    AngleDegrees = r.F(),
                    Affects = r.E<FactionFilter>(),
                    Charge = r.B(),
                    SourceFaction = OptFaction(r.I()),
                    StartTick = r.L(),
                    ResolveAtTick = r.L(),
                };
            case 12:
                return new AreaState
                {
                    AreaId = r.S(),
                    Owner = r.Id(),
                    OwnerFaction = OptFaction(r.I()),
                    Radius = r.F(),
                    Mode = r.E<AreaMode>(),
                    Affects = r.E<FactionFilter>(),
                    CreatedTick = r.L(),
                    ArmedAtTick = r.L(),
                    ArmedAnnounced = r.B(),
                    ExpiresAtTick = r.L(),
                    NextApplyTick = r.L(),
                };
            case 13:
                return new PhaseState { Index = r.I(), EnteredTick = r.L() };
            case 14:
                return new CharacterStats
                {
                    Level = r.I(),
                    Str = r.I(),
                    Dex = r.I(),
                    Agi = r.I(),
                    Int = r.I(),
                    Vit = r.I(),
                    WeaponDamage = r.F(),
                    Armor = r.F(),
                    Resistance = r.F(),
                };
            default:
                throw new MemoryPackSerializationException($"Tag de componente desconhecida: {tag}");
        }
    }

    private static Faction? OptFaction(int v) =>
        v < 0 ? null : Enum.IsDefined((Faction)v) ? (Faction)v : throw new MemoryPackSerializationException($"Facção inválida: {v}");

    private static void WriteAbilities<TB>(ref MemoryPackWriter<TB> w, AbilityState a)
        where TB : IBufferWriter<byte>
    {
        w.Strs(a.Known);
        w.S(a.BasicAttackId);
        WriteMap(ref w, a.ReadyAtTick, static (ref MemoryPackWriter<TB> w2, long v) => w2.L(v));
        w.B(a.Cast is not null);
        if (a.Cast is { } cast)
        {
            w.S(cast.AbilityId);
            w.L(cast.StartTick);
            w.L(cast.EndTick);
            w.IdOpt(cast.Target);
            w.VOpt(cast.AimPoint);
            w.B(cast.LockedFacing);
        }

        w.Strs(a.Consumables);
        WriteMap(ref w, a.ChargesUsed, static (ref MemoryPackWriter<TB> w2, int v) => w2.I(v));

        // Dicionários criados sob demanda no Core: vazios não alocam nada aqui nem no cliente.
        w.B(a.HasRanks);
        if (a.HasRanks)
        {
            WriteMap(ref w, a.Ranks, static (ref MemoryPackWriter<TB> w2, int v) => w2.I(v));
        }

        w.B(a.HasCooldownChargesSpent);
        if (a.HasCooldownChargesSpent)
        {
            WriteMap(ref w, a.CooldownChargesSpent, static (ref MemoryPackWriter<TB> w2, int v) => w2.I(v));
        }
    }

    private static AbilityState ReadAbilities(ref MemoryPackReader r)
    {
        var a = new AbilityState();
        a.Known.AddRange(r.Strs());
        a.BasicAttackId = r.SOpt();
        ReadMap(ref r, a.ReadyAtTick, static (ref MemoryPackReader r2) => r2.L());
        if (r.B())
        {
            a.Cast = new CastState
            {
                AbilityId = r.S(),
                StartTick = r.L(),
                EndTick = r.L(),
                Target = r.IdOpt(),
                AimPoint = r.VOpt(),
                LockedFacing = r.B(),
            };
        }

        a.Consumables.AddRange(r.Strs());
        ReadMap(ref r, a.ChargesUsed, static (ref MemoryPackReader r2) => r2.I());
        if (r.B())
        {
            ReadMap(ref r, a.Ranks, static (ref MemoryPackReader r2) => r2.I());
        }

        if (r.B())
        {
            ReadMap(ref r, a.CooldownChargesSpent, static (ref MemoryPackReader r2) => r2.I());
        }

        return a;
    }

    private delegate void ValueWriter<TB, in T>(ref MemoryPackWriter<TB> w, T value)
        where TB : IBufferWriter<byte>;

    private delegate T ValueReader<out T>(ref MemoryPackReader r);

    private static void WriteMap<TB, T>(ref MemoryPackWriter<TB> w, IReadOnlyDictionary<string, T> map, ValueWriter<TB, T> value)
        where TB : IBufferWriter<byte>
    {
        w.I(map.Count);
        foreach (var (k, v) in map)
        {
            w.S(k);
            value(ref w, v);
        }
    }

    private static void ReadMap<T>(ref MemoryPackReader r, IDictionary<string, T> into, ValueReader<T> value)
    {
        var n = Wire.Count(ref r);
        for (var i = 0; i < n; i++)
        {
            var k = r.S();
            into[k] = value(ref r);
        }
    }
}
