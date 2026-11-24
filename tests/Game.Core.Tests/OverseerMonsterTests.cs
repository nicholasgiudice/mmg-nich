using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Areas;
using Game.Core.Combat.Status;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Tests;

/// <summary>
/// T-512: o Capataz Forjado, chefe das Minas Fundas. Nenhuma mecânica nova de Core: o Martelo é um cone com telegráfo, o Sopro
/// da Fornalha é um círculo em volta dele do tamanho da sala (sem telegráfo, não dá para sair), e a fase 3 invoca Mineiros
/// Possuídos e aplica a Fornalha Acesa nele (velocidade e dano), pelos efeitos de entrada de fase que já existiam. Os testes
/// conferem esse uso contra o <c>data/</c> real.
/// </summary>
public class OverseerMonsterTests
{
    private const string Overseer = "monster.forged_overseer";

    private static ContentCatalog Real => RealContent.Value;

    private static World NewWorld() => WorldBuilder.CreateDefault(1, new GridMap(40, 28, null, []), Real);

    /// <summary>Guerreiro de nível 1 com vida de sobra: o chefe do nível 27 age em volta sem matá-lo durante o teste.</summary>
    private static Entity Player(World w, Vector2 pos)
    {
        var e = EntityFactory.SpawnPlayer(w, Real.Classes["class.warrior"], pos);
        var h = e.Get<Health>();
        h.Max = h.Current = 1_000_000;
        e.Get<Movement>().Direction = Vector2.Zero;
        return e;
    }

    private static bool Hurt(Entity e) => e.Get<Health>().Current < e.Get<Health>().Max;

    private static void Run(World w, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            w.Step();
        }
    }

    private static Entity Spawn(World w, Vector2 pos, double healthFraction)
    {
        var boss = EntityFactory.SpawnMonster(w, Real.Monsters[Overseer], pos);
        var health = boss.Get<Health>();
        health.Current = health.Max * (float)healthFraction;
        boss.Get<Movement>().Direction = Vector2.Zero;
        return boss;
    }

    [Fact]
    public void Furnace_blast_hits_every_player_in_the_hall_and_no_monster()
    {
        var w = NewWorld();
        var boss = Spawn(w, new Vector2(20, 14), 0.5); // fase 2: a Fornalha só existe a partir dela (PhaseGate)
        var miner = EntityFactory.SpawnMonster(w, Real.Monsters["monster.possessed_miner"], new Vector2(22, 14));
        var players = new[] { new Vector2(2, 2), new Vector2(38, 26), new Vector2(21, 14), new Vector2(2, 26) }.Select(p => Player(w, p)).ToList();
        Run(w, 1);

        w.Enqueue(new UseAbilityIntent(boss.Id, "forged_overseer.furnace_blast", players[0].Id, null));
        Run(w, 2);

        Assert.All(players, p => Assert.True(Hurt(p))); // de canto a canto da sala de 40 × 28
        Assert.False(Hurt(miner));
    }

    [Fact]
    public void Furnace_blast_is_not_used_in_the_first_phase()
    {
        var w = NewWorld();
        var boss = Spawn(w, new Vector2(20, 14), 1.0);
        var hero = Player(w, new Vector2(30, 14));
        Run(w, 1);

        var check = AbilityValidator.Validate(w, boss, "forged_overseer.furnace_blast", hero.Id, null);
        Assert.NotNull(check.Reason);
    }

    [Fact]
    public void Hammer_hits_only_the_players_in_the_cone_in_front()
    {
        var w = NewWorld();
        var boss = Spawn(w, new Vector2(10, 14), 1.0);
        var front = Player(w, new Vector2(12, 14));
        var behind = Player(w, new Vector2(7, 14));
        var far = Player(w, new Vector2(16, 14));
        Run(w, 1);

        var hammer = Real.Abilities["forged_overseer.hammer"];
        Assert.Equal(TelegraphShape.Cone, hammer.Telegraph!.Shape);
        w.Enqueue(new UseAbilityIntent(boss.Id, hammer.Id, front.Id, null));
        Run(w, hammer.Telegraph.DelayTicks + 2);

        Assert.True(Hurt(front));
        Assert.False(Hurt(behind));
        Assert.False(Hurt(far)); // o cone é curto (3,5)
    }

    [Fact]
    public void Entering_call_the_crew_summons_miners_and_speeds_the_overseer_up()
    {
        var w = NewWorld();
        var boss = Spawn(w, new Vector2(20, 14), 0.31);
        Player(w, new Vector2(30, 14));
        Run(w, 1);
        var before = w.Combat.SpeedMultiplier(w, boss);
        Assert.False(StatusManager.Has(w, boss, "status.overseer_overdrive"));

        boss.Get<Health>().Current = boss.Get<Health>().Max * 0.29f;
        var summoned = new List<MonsterSummoned>();
        for (var i = 0; i < 2; i++)
        {
            w.Step();
            summoned.AddRange(w.LastTickEvents.OfType<MonsterSummoned>());
        }

        Assert.True(StatusManager.Has(w, boss, "status.overseer_overdrive"));
        Assert.True(w.Combat.SpeedMultiplier(w, boss) > before);
        Assert.Equal(2, summoned.Count(e => e.Owner == boss.Id && e.MonsterId == "monster.possessed_miner"));
    }

    /// <summary>
    /// <c>meleeTargetRange</c> (mecânica nova do <c>ai.boss</c>): o Capataz bate em quem está à frente dele. O Mago longe, com
    /// dez vezes a ameaça, não tira o alvo do Guerreiro colado; quando o Guerreiro se afasta, o chefe volta à tabela.
    /// </summary>
    [Fact]
    public void Overseer_hits_whoever_is_in_front_of_him_before_the_threat_table()
    {
        var w = NewWorld();
        var boss = Spawn(w, new Vector2(20, 14), 1.0);
        var tank = Player(w, new Vector2(22, 14));
        var mage = Player(w, new Vector2(30, 14));
        ThreatService.Add(w, boss, tank, 100, AggroReason.Damage);
        ThreatService.Add(w, boss, mage, 1000, AggroReason.Damage);
        Run(w, 2);
        Assert.Equal(tank.Id, boss.Get<AiState>().Target);

        tank.Get<Transform>().Position = new Vector2(20, 24); // longe dos dois
        Run(w, 2);
        Assert.Equal(mage.Id, boss.Get<AiState>().Target);
    }

    /// <summary>Sem o parâmetro (todo outro chefe, como o Vigia), a escolha continua sendo a da tabela de ameaça.</summary>
    [Fact]
    public void Without_melee_target_range_a_boss_follows_the_threat_table()
    {
        var w = NewWorld();
        var watcher = EntityFactory.SpawnMonster(w, Real.Monsters["monster.faceless_watcher"], new Vector2(20, 14));
        watcher.Get<Movement>().Direction = Vector2.Zero;
        var tank = Player(w, new Vector2(22, 14));
        var mage = Player(w, new Vector2(30, 14));
        ThreatService.Add(w, watcher, tank, 100, AggroReason.Damage);
        ThreatService.Add(w, watcher, mage, 1000, AggroReason.Damage);
        Run(w, 2);
        Assert.Equal(mage.Id, watcher.Get<AiState>().Target);
    }

    [Fact]
    public void Overseer_has_three_phases_hammer_then_furnace_then_crew()
    {
        var m = Real.Monsters[Overseer];
        Assert.Equal(MonsterRank.Boss, m.Rank);
        Assert.Equal(27, m.Level);
        Assert.Contains("construct", m.Tags);

        var phases = m.Phases;
        Assert.Equal(3, phases.Count);
        Assert.Contains("forged_overseer.hammer", phases[0].Abilities!);
        Assert.DoesNotContain("forged_overseer.furnace_blast", phases[0].Abilities!);
        Assert.Contains("forged_overseer.furnace_blast", phases[1].Abilities!);
        Assert.Contains("forged_overseer.hammer", phases[1].Abilities!);
        Assert.Contains("forged_overseer.call_crew", phases[2].Abilities!);
        Assert.Contains("forged_overseer.furnace_blast", phases[2].Abilities!);
        Assert.Contains(phases[2].OnEnter.OfType<SummonEffect>(), s => s.Monster == "monster.possessed_miner");
        Assert.True(Real.Abilities["forged_overseer.hammer_fast"].CooldownTicks < Real.Abilities["forged_overseer.hammer"].CooldownTicks);
    }
}
