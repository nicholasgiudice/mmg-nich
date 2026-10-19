using Game.Core.Combat;
using Game.Core.Simulation;
using Xunit.Abstractions;

namespace Game.Core.Tests;

/// <summary>T-101: o Core rodando em servidor (estado replicável, snapshots leves, vários mundos, conteúdo igual).</summary>
public class ServerReadinessTests(ITestOutputHelper output)
{
    private const string Matriarch = "encounter.forest_matriarch";

    /// <summary>Matriarca com os 4 jogadores parados: a IA, os telegráfos e as invocações trabalham sozinhos.</summary>
    private static World MatriarchWorld(ulong seed, int warmupTicks)
    {
        var content = RealContent.Value;
        var world = EncounterSetup.Create(seed, content, content.Encounters[Matriarch]).World;
        for (var i = 0; i < warmupTicks; i++)
        {
            world.Step();
        }

        return world;
    }

    [Fact]
    public void Replicated_snapshot_has_only_replicated_components_and_keeps_what_the_client_needs()
    {
        var world = MatriarchWorld(1, 100);
        var full = world.Snapshot(SnapshotScope.Full);
        var replicated = world.Snapshot(SnapshotScope.Replicated);

        Assert.Equal(full.Entities.Select(e => e.Id), replicated.Entities.Select(e => e.Id));
        Assert.All(replicated.Entities, e => Assert.All(e.Components, c => Assert.IsAssignableFrom<IReplicated>(c)));

        // Monstros têm estado interno (IA, ameaça) que não vai para o cliente.
        var internalTypes = full.Entities.SelectMany(e => e.Components).Where(c => c is not IReplicated)
            .Select(c => c.GetType().Name).Distinct().ToList();
        Assert.Contains("AiState", internalTypes);
        Assert.Contains("ThreatTable", internalTypes);

        // O que o cliente usa continua lá.
        var boss = replicated.Entities.Single(e => e.TryGet<UnitInfo>()?.DefinitionId == "monster.forest_matriarch");
        Assert.NotNull(boss.TryGet<Simulation.Components.Transform>());
        Assert.NotNull(boss.TryGet<Simulation.Components.Health>());
        Assert.NotNull(boss.TryGet<AbilityState>());
        Assert.NotNull(boss.TryGet<Combat.Phases.PhaseState>());
        Assert.Null(boss.TryGet<Combat.Threat.ThreatTable>());
    }

    [Fact]
    public void Snapshot_factory_builds_entities_in_deterministic_order_and_rejects_duplicates()
    {
        var source = MatriarchWorld(1, 20).Snapshot(SnapshotScope.Replicated).Entities[0];
        var rebuilt = EntitySnapshot.Create(source.Id, source.Components.Reverse());

        Assert.Equal(source.Id, rebuilt.Id);
        Assert.Equal(source.Components.Select(c => c.GetType()), rebuilt.Components.Select(c => c.GetType()));
        Assert.Same(source.TryGet<Simulation.Components.Transform>(), rebuilt.TryGet<Simulation.Components.Transform>());
        Assert.Throws<ArgumentException>(() => EntitySnapshot.Create(source.Id, [source.Components[0], source.Components[0]]));
    }

    [Fact]
    public void Two_worlds_stepped_interleaved_match_the_same_worlds_stepped_separately()
    {
        const int ticks = 400;

        static List<ulong> Hashes(World w)
        {
            var list = new List<ulong>();
            for (var i = 0; i < ticks; i++)
            {
                w.Step();
                list.Add(w.ComputeStateHash());
            }

            return list;
        }

        var separateA = Hashes(MatriarchWorld(11, 0));
        var separateB = Hashes(MatriarchWorld(22, 0));

        var a = MatriarchWorld(11, 0);
        var b = MatriarchWorld(22, 0);
        var interleavedA = new List<ulong>();
        var interleavedB = new List<ulong>();
        for (var i = 0; i < ticks; i++)
        {
            a.Step();
            b.Step();
            interleavedA.Add(a.ComputeStateHash());
            interleavedB.Add(b.ComputeStateHash());
        }

        Assert.Equal(separateA, interleavedA);
        Assert.Equal(separateB, interleavedB);
        Assert.NotEqual(separateA[^1], separateB[^1]); // seeds diferentes, mundos diferentes
    }

    [Fact]
    public void Real_content_has_a_fingerprint()
    {
        Assert.Matches("^[0-9a-f]{16}$", RealContent.Value.Fingerprint);
    }

    [Fact]
    public void Measure_snapshot_and_tick_allocations_in_the_matriarch_encounter()
    {
        var world = MatriarchWorld(1, 200);
        const int samples = 100;

        long Allocated(Action action)
        {
            action(); // aquece caches (ordem dos componentes)
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < samples; i++)
            {
                action();
            }

            return (GC.GetAllocatedBytesForCurrentThread() - before) / samples;
        }

        var full = Allocated(() => world.Snapshot(SnapshotScope.Full));
        var replicated = Allocated(() => world.Snapshot(SnapshotScope.Replicated));
        var perTick = Allocated(world.Step);
        output.WriteLine($"entidades={world.Entities.Count} snapshot completo={full} B replicado={replicated} B tick={perTick} B");

        // Antes da T-101 (Debug, mesmo cenário): snapshot completo 18 167 B, tick 4 892 B.
        Assert.True(replicated < full, $"replicado {replicated} B >= completo {full} B");
        Assert.True(replicated < 18_167 / 2, $"snapshot replicado {replicated} B não caiu pela metade");
    }
}
