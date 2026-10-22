using System.Collections;
using System.Numerics;
using System.Reflection;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Data;
using Game.Protocol;
using Xunit.Abstractions;

namespace Game.Protocol.Tests;

/// <summary>T-103: mensagens, componentes replicáveis e eventos ida e volta; cobertura de todos os tipos do Core.</summary>
public class ProtocolTests(ITestOutputHelper output)
{
    private static readonly Assembly Core = typeof(World).Assembly;

    private static T RoundTrip<T>(T message)
        where T : INetMessage
    {
        var bytes = ProtocolSerializer.Serialize(message);
        var back = Assert.IsType<T>(ProtocolSerializer.Deserialize(bytes));
        Assert.Equal(bytes, ProtocolSerializer.Serialize(back)); // ida e volta estável, byte a byte
        return back;
    }

    // ---------- mensagens ----------

    [Fact]
    public void Every_simple_message_round_trips()
    {
        Assert.Equal(new Hello(ProtocolVersion.Current, "abc123", "dev", "class.mage"), RoundTrip(new Hello(ProtocolVersion.Current, "abc123", "dev", "class.mage")));
        Assert.Equal(new Hello(1, "f", "x", null), RoundTrip(new Hello(1, "f", "x", null)));
        Assert.Equal(new Ping(123456789), RoundTrip(new Ping(123456789)));
        Assert.Equal(new Pong(5, 77), RoundTrip(new Pong(5, 77)));
        Assert.Equal(new Welcome(new EntityId(7), 1200, "encounter.forest_matriarch", "arena.forest"), RoundTrip(new Welcome(new EntityId(7), 1200, "encounter.forest_matriarch", "arena.forest")));
        Assert.Equal(new Reject(RejectReason.Content, "fingerprint"), RoundTrip(new Reject(RejectReason.Content, "fingerprint")));
        Assert.Equal(new IntentAck(42, 900), RoundTrip(new IntentAck(42, 900)));
    }

    [Fact]
    public void Intents_round_trip_and_always_use_the_connection_entity()
    {
        var me = new EntityId(3);
        var intents = new Intent[]
        {
            new MoveIntent(new EntityId(99), Vector2.Normalize(new Vector2(1, -1))),
            new StopIntent(new EntityId(99)),
            new UseAbilityIntent(new EntityId(99), "mage.elemental_blast", null, new Vector2(4.5f, 7.25f)),
            new UseAbilityIntent(new EntityId(99), "archer.power_shot", new EntityId(12)),
        };

        for (var i = 0; i < intents.Length; i++)
        {
            var msg = RoundTrip(new IntentMessage((uint)i + 1, 1000 + i, NetIntent.From(intents[i])));
            Assert.Equal((uint)i + 1, msg.Sequence);
            Assert.Equal(intents[i] with { Entity = me }, msg.Intent.ToIntent(me));
        }

        Assert.Null(new NetIntent(NetIntentKind.UseAbility, default, null, null, null).ToIntent(me));
        Assert.Null(new NetIntent(NetIntentKind.Move, new Vector2(float.NaN, 0), null, null, null).ToIntent(me));
    }

    [Fact]
    public void Garbage_is_rejected_without_throwing()
    {
        Assert.False(ProtocolSerializer.TryDeserialize([], out _));
        Assert.False(ProtocolSerializer.TryDeserialize([5, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F, 1, 2], out _));
        Assert.False(ProtocolSerializer.TryDeserialize([200, 1, 2, 3], out _));
    }

    // ---------- cobertura: todo tipo do Core tem formato ----------

    [Fact]
    public void Every_replicated_component_has_a_codec_and_internal_ones_do_not()
    {
        var replicated = Core.GetTypes().Where(t => typeof(IReplicated).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false }).ToList();
        var missing = replicated.Where(t => !ComponentCodecs.Tags.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0, "Componentes replicáveis sem formato de rede: " + string.Join(", ", missing));
        Assert.All(ComponentCodecs.Tags.Keys, t => Assert.True(typeof(IReplicated).IsAssignableFrom(t), t.Name));
        Assert.Equal(ComponentCodecs.Tags.Count, ComponentCodecs.Tags.Values.Distinct().Count());
    }

    [Fact]
    public void Every_game_event_of_the_core_has_a_codec()
    {
        var events = Core.GetTypes().Where(t => t.IsSubclassOf(typeof(GameEvent)) && !t.IsAbstract).ToList();
        var missing = events.Where(t => !EventCodecs.Tags.ContainsKey(t)).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0, "Eventos sem formato de rede (adicione em EventCodecs e suba ProtocolVersion): " + string.Join(", ", missing));
        Assert.Equal(EventCodecs.Tags.Count, EventCodecs.Tags.Values.Distinct().Count());
        Assert.DoesNotContain((ushort)0, EventCodecs.Tags.Values); // 0 = null
    }

    public static TheoryData<string> EventTypes()
    {
        var data = new TheoryData<string>();
        foreach (var t in EventCodecs.Tags.Keys.OrderBy(t => t.Name))
        {
            data.Add(t.AssemblyQualifiedName!);
        }

        return data;
    }

    /// <summary>Cada evento com valores diferentes em cada campo: troca de ordem ou campo esquecido aparece.</summary>
    [Theory]
    [MemberData(nameof(EventTypes))]
    public void Every_event_round_trips_field_by_field(string typeName)
    {
        var type = Type.GetType(typeName)!;
        var ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var seed = 1;
        var args = ctor.GetParameters().Select(p => Sample(p.ParameterType, seed++)).ToArray();
        var original = (GameEvent)ctor.Invoke(args);
        typeof(GameEvent).GetProperty(nameof(GameEvent.Tick))!.SetValue(original, 4321L);

        var back = Assert.Single(RoundTrip(new EventsMessage(4321, [original])).Events);

        Assert.IsType(type, back);
        Assert.Equal(4321L, back.Tick);
        foreach (var prop in type.GetProperties().Where(p => p.DeclaringType == type))
        {
            AssertDeepEqual(prop.GetValue(original), prop.GetValue(back), $"{type.Name}.{prop.Name}");
        }
    }

    private static object? Sample(Type t, int n)
    {
        var under = Nullable.GetUnderlyingType(t);
        if (under is not null)
        {
            return Sample(under, n);
        }

        if (t == typeof(EntityId)) return new EntityId(n);
        if (t == typeof(string)) return $"s{n}";
        if (t == typeof(int)) return n * 3;
        if (t == typeof(long)) return n * 1000L;
        if (t == typeof(float)) return n + 0.25f;
        if (t == typeof(bool)) return n % 2 == 0;
        if (t == typeof(Vector2)) return new Vector2(n, -n * 0.5f);
        if (t.IsEnum)
        {
            var values = Enum.GetValues(t);
            return values.GetValue(values.Length > 1 ? 1 : 0);
        }

        if (t == typeof(IReadOnlyList<EntityId>)) return new[] { new EntityId(n), new EntityId(n + 100) };
        throw new NotSupportedException($"Sem amostra para {t.Name}: acrescente em Sample e em Wire");
    }

    private static void AssertDeepEqual(object? expected, object? actual, string what)
    {
        if (expected is IEnumerable a && expected is not string && actual is IEnumerable b)
        {
            Assert.True(a.Cast<object>().SequenceEqual(b.Cast<object>()), what);
            return;
        }

        Assert.True(Equals(expected, actual), $"{what}: esperado {expected}, veio {actual}");
    }

    // ---------- snapshot e eventos reais da Matriarca ----------

    private static ContentCatalog RealContent()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        var db = GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();
        return new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
            db.Monsters.Values, db.Arenas.Values, db.Encounters.Values)
        {
            Rules = db.Rules,
            Threat = db.Threat,
            Fingerprint = db.Fingerprint,
        };
    }

    [Fact]
    public void Real_matriarch_snapshot_and_events_round_trip()
    {
        var content = RealContent();
        var setup = EncounterSetup.Create(7, content, content.Encounters["encounter.forest_matriarch"]);
        var world = setup.World;
        var director = new BotDirector(world, new TelegraphHazardSource()) { Seed = 7 };
        foreach (var p in setup.Players)
        {
            director.Add(p, BotStrategies.Create("kite+dodge"), new BotOptions(new Dictionary<string, string> { ["class.mage:explosion"] = "mage.elemental_blast" }));
        }

        var events = new List<GameEvent>();
        var biggest = 0;
        var seenTypes = new HashSet<Type>();
        WorldSnapshot? richest = null;
        for (var t = 0; t < 60 * World.TickRate && setup.Players.Any(p => p.IsAlive); t++)
        {
            director.Tick();
            world.Step();
            events.AddRange(world.LastTickEvents);
            if (t % 20 == 0)
            {
                var snap = world.Snapshot(SnapshotScope.Replicated);
                var types = snap.Entities.SelectMany(e => e.Components).Select(c => c.GetType()).ToHashSet();
                if (types.Count >= seenTypes.Count)
                {
                    seenTypes = types;
                    richest = snap;
                }

                biggest = Math.Max(biggest, ProtocolSerializer.Serialize(SnapshotMessage.From(snap)).Length);
            }
        }

        // Snapshot do início (o que o cliente recebe ao entrar) e o mais variado da luta.
        var first = EncounterSetup.Create(7, content, content.Encounters["encounter.forest_matriarch"]).World.Snapshot(SnapshotScope.Replicated);
        foreach (var snap in new[] { first, richest! })
        {
            var msg = RoundTrip(SnapshotMessage.From(snap));
            Assert.Equal(snap.Tick, msg.Tick);
            Assert.Equal(snap.Entities.Select(e => e.Id), msg.Entities.Select(e => e.Id));
            for (var i = 0; i < snap.Entities.Count; i++)
            {
                var a = snap.Entities[i].Components;
                var b = msg.Entities[i].Components;
                Assert.Equal(a.Select(c => c.GetType()), b.Select(c => c.GetType()));
                for (var k = 0; k < a.Count; k++)
                {
                    Assert.Equal(Hash(a[k]), Hash(b[k]));
                }
            }
        }

        var full = ProtocolSerializer.Serialize(SnapshotMessage.From(first)).Length;
        output.WriteLine($"Snapshot completo da Matriarca no início: {full} B ({first.Entities.Count} entidades); maior na luta: {biggest} B");
        output.WriteLine($"Componentes vistos no snapshot mais variado: {string.Join(", ", seenTypes.Select(t => t.Name).Order())}");
        Assert.True(full < 2048, $"snapshot inicial grande demais: {full} B");

        var back = RoundTrip(new EventsMessage(world.Tick, events.ToArray()));
        Assert.Equal(events.Count, back.Events.Length);
        Assert.Equal(events.Select(e => e.GetType()), back.Events.Select(e => e.GetType()));
        Assert.Equal(events.Select(e => e.Tick), back.Events.Select(e => e.Tick));
        output.WriteLine($"{events.Count} eventos em 60 s: {ProtocolSerializer.Serialize(back).Length} B");
    }

    private static ulong Hash(IComponent c)
    {
        var h = new StateHasher();
        c.WriteHash(h);
        return h.Value;
    }

    [Fact]
    public void Core_does_not_reference_the_protocol_nor_network_packages()
    {
        var refs = Core.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.DoesNotContain("Game.Protocol", refs);
        Assert.DoesNotContain(refs, r => r.StartsWith("MemoryPack", StringComparison.Ordinal) || r.StartsWith("LiteNetLib", StringComparison.Ordinal));
    }
}
