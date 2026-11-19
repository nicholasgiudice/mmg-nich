using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Protocol;
using Game.Server.Zones;

namespace Game.Server.Tests;

/// <summary>T-395: sair do mundo (trocar de personagem) com <see cref="LogoutRequest"/>, com espera em combate.</summary>
public class LogoutTests
{
    /// <summary>Espera em combate de <c>data/rules.json</c> (10 s).</summary>
    private const int Delay = 200;

    /// <summary>Ticks entre o <see cref="LogoutState.Done"/> e o servidor fechar a conexão.</summary>
    private const int CloseDelay = 10;

    private static void InCombat(WorldRig w, TestClient c)
    {
        var (zone, e) = w.Where(c);
        if (e.TryGet<CombatActivity>() is not { } activity)
        {
            e.Add(activity = new CombatActivity());
        }

        activity.LastCombatTick = zone.World.Tick;
        Assert.True(Leveling.InCombat(zone.World, e));
    }

    private static void SendIntent(TestClient c, uint sequence, Intent intent) => c.Send(new IntentMessage(sequence, 0, NetIntent.From(intent)));

    private static List<LogoutState> States(TestClient c) => c.All<LogoutStatus>().Select(s => s.State).ToList();

    private static bool InWorld(WorldRig w, TestClient c) => w.Server.Placement.ContainsKey(c.Raw.Peer);

    /// <summary>Pede a saída em combate e devolve o <see cref="LogoutState.Waiting"/> que chegou.</summary>
    private static LogoutStatus RequestInCombat(WorldRig w, TestClient c)
    {
        InCombat(w, c);
        c.Send(new LogoutRequest());
        w.Tick(1, c);
        var waiting = Assert.Single(c.All<LogoutStatus>());
        Assert.Equal(LogoutState.Waiting, waiting.State);
        return waiting;
    }

    private static ContentCatalog WithCombatDelay(int ticks)
    {
        var c = TestRig.RealContent.Value;
        return new ContentCatalog(c.Classes.Values, c.Abilities.Values, c.Statuses.Values, c.Areas.Values,
            c.Monsters.Values, c.Arenas.Values, c.Encounters.Values, c.Items.Values)
        {
            Rules = c.Rules with { Logout = new LogoutRules { CombatDelayTicks = ticks } },
            Threat = c.Threat,
            Fingerprint = c.Fingerprint,
            Zones = c.Zones,
            Shops = c.Shops,
        };
    }

    [Fact]
    public void Out_of_combat_leaves_at_once_and_the_connection_closes()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var other = w.Join("bruno");
        var id = w.Where(c).Entity.Id;
        Assert.False(Leveling.InCombat(w.City.World, w.Where(c).Entity));

        c.Send(new LogoutRequest());
        w.Tick(1, c, other);

        Assert.Equal([LogoutState.Done], States(c));
        Assert.Equal(0, c.All<LogoutStatus>()[0].Tick);
        Assert.False(InWorld(w, c));
        Assert.Null(w.City.World.Find(id));
        Assert.Equal(1, w.Server.PlayerCount);
        Assert.Null(other.Entity(id)); // os outros veem o personagem sumir

        // Depois do Done a conexão não serve para outro personagem: o cliente volta para a lista e entra com outra.
        c.Send(new Hello(ProtocolVersion.Current, w.Content.Fingerprint, "aria", null));
        w.Tick(CloseDelay, c, other);
        Assert.False(InWorld(w, c));
        Assert.True(c.Raw.Closed);
        Assert.Equal([LogoutState.Done], States(c));
    }

    [Fact]
    public void Dead_character_leaves_at_once_even_in_combat()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        InCombat(w, c);
        w.City.World.Kill(w.Where(c).Entity);
        w.Tick(1, c);

        c.Send(new LogoutRequest());
        w.Tick(1, c);

        Assert.Equal([LogoutState.Done], States(c));
        Assert.False(InWorld(w, c));
    }

    [Fact]
    public void In_combat_the_character_stays_until_the_final_tick_and_then_leaves()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;
        SendIntent(c, 1, new MoveIntent(hero.Id, Vector2.UnitY));
        w.Tick(2, c);
        Assert.NotEqual(Vector2.Zero, hero.Get<Movement>().Direction);

        var waiting = RequestInCombat(w, c);

        Assert.Equal(w.City.World.Tick + Delay, waiting.Tick);
        w.Tick(1, c);
        Assert.Equal(Vector2.Zero, hero.Get<Movement>().Direction); // quem andava parou; o servidor não anda por ele
        while (w.City.World.Tick < waiting.Tick - 1)
        {
            w.Tick(1, c);
        }

        Assert.True(InWorld(w, c)); // continua no mundo, vulnerável, até o fim
        Assert.Same(hero, w.Where(c).Entity);
        Assert.Equal([LogoutState.Waiting], States(c));

        // Pedir de novo na espera só repete o mesmo tick.
        c.Send(new LogoutRequest());
        w.Tick(1, c);

        Assert.Equal(waiting.Tick, w.City.World.Tick);
        Assert.Equal([LogoutState.Waiting, LogoutState.Waiting, LogoutState.Done], States(c));
        Assert.Equal(waiting.Tick, c.All<LogoutStatus>()[1].Tick);
        Assert.False(InWorld(w, c));
        Assert.Null(w.City.World.Find(hero.Id));
    }

    [Fact]
    public void Moving_cancels_the_wait_but_stopping_does_not()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;
        RequestInCombat(w, c);

        SendIntent(c, 1, new StopIntent(hero.Id));
        SendIntent(c, 2, new MoveIntent(hero.Id, Vector2.Zero));
        w.Tick(1, c);
        Assert.Equal([LogoutState.Waiting], States(c));

        SendIntent(c, 3, new MoveIntent(hero.Id, Vector2.UnitX));
        w.Tick(1, c);
        Assert.Equal([LogoutState.Waiting, LogoutState.Cancelled], States(c));
        Assert.Equal(Vector2.UnitX, hero.Get<Movement>().Direction); // a intenção que cancelou vale

        w.Tick(Delay + 5, c);
        Assert.True(InWorld(w, c));
        Assert.Equal([LogoutState.Waiting, LogoutState.Cancelled], States(c));
    }

    [Fact]
    public void Using_an_ability_cancels_the_wait()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;
        RequestInCombat(w, c);

        SendIntent(c, 1, new UseAbilityIntent(hero.Id, w.Content.Classes["class.warrior"].Abilities[0]));
        w.Tick(Delay + 5, c);

        Assert.Equal([LogoutState.Waiting, LogoutState.Cancelled], States(c));
        Assert.True(InWorld(w, c));
    }

    [Fact]
    public void Logout_cancel_cancels_the_wait_and_a_new_request_starts_over()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        RequestInCombat(w, c);

        c.Send(new LogoutCancel());
        w.Tick(Delay + 5, c);
        c.Send(new LogoutCancel()); // sem espera: nada a cancelar, nada chega
        w.Tick(1, c);

        Assert.Equal([LogoutState.Waiting, LogoutState.Cancelled], States(c));
        Assert.True(InWorld(w, c));

        // Fora de combate de novo (o combate acaba em 5 s): o próximo pedido sai na hora.
        c.Send(new LogoutRequest());
        w.Tick(1, c);
        Assert.Equal([LogoutState.Waiting, LogoutState.Cancelled, LogoutState.Done], States(c));
        Assert.False(InWorld(w, c));
    }

    [Fact]
    public void Request_and_move_in_the_same_tick_is_cancelled()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        var hero = w.Where(c).Entity;

        c.Send(new LogoutRequest());
        SendIntent(c, 1, new MoveIntent(hero.Id, Vector2.UnitX));
        w.Tick(3, c);

        Assert.Equal([LogoutState.Cancelled], States(c));
        Assert.True(InWorld(w, c));
    }

    [Fact]
    public void Dying_during_the_wait_does_not_cancel_and_leaves_at_once()
    {
        using var w = new WorldRig();
        var c = w.Join("aria");
        RequestInCombat(w, c);
        w.Tick(20, c);

        w.City.World.Kill(w.Where(c).Entity);
        w.Tick(1, c);

        Assert.Equal([LogoutState.Waiting, LogoutState.Done], States(c));
        Assert.False(InWorld(w, c));
    }

    [Fact]
    public void Zero_combat_delay_leaves_at_once_in_combat()
    {
        using var w = new WorldRig(content: WithCombatDelay(0));
        var c = w.Join("aria");
        InCombat(w, c);

        c.Send(new LogoutRequest());
        w.Tick(1, c);

        Assert.Equal([LogoutState.Done], States(c));
    }

    [Fact]
    public void Leaving_drops_the_character_from_the_party()
    {
        using var w = new WorldRig();
        var a = w.Join("aria");
        var b = w.Join("bruno");
        a.Send(new PartyInvite(null, "bruno"));
        w.Tick(1, a, b);
        b.Send(new PartyRespond(b.All<PartyInviteReceived>().Last().InviteId, true));
        w.Tick(1, a, b);
        Assert.NotEqual(0, w.Server.Parties.PartyOf(b.Raw.Peer));

        b.Send(new LogoutRequest());
        w.Tick(2, a, b);

        Assert.Equal(0, w.Server.Parties.PartyOf(b.Raw.Peer));
        Assert.Equal(0, w.Server.PartyStateFor(a.Raw.Peer).PartyId); // grupo de dois desfeito
    }

    // ---------------- com conta (gravação) ----------------

    [Fact]
    public void Done_arrives_only_after_the_save_finishes_and_the_character_comes_back_where_it_left()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        var hero = w.Where(c).Entity;
        SendIntent(c, 1, new MoveIntent(hero.Id, Vector2.UnitY));
        w.Tick(10, c);
        SendIntent(c, 2, new StopIntent(hero.Id));
        w.Tick(2, c);
        var spot = hero.Get<Transform>().Position;
        Assert.NotEqual(w.City.SpawnPosition, spot);
        w.Flush();
        var saves = store.Saves;

        store.SaveGate = new TaskCompletionSource(); // banco lento
        c.Send(new LogoutRequest());
        w.Tick(30, c);

        Assert.False(InWorld(w, c)); // já saiu do mundo
        Assert.Empty(c.All<LogoutStatus>()); // mas o Done espera a gravação
        Assert.False(c.Raw.Closed);
        Assert.Equal(saves, store.Saves);

        // Enquanto grava, o mesmo personagem não entra por outra conexão.
        var early = w.JoinWithTicket(AccountRig.Ticket(row));
        Assert.Equal(RejectReason.AlreadyConnected, Assert.Single(early.All<Reject>()).Reason);

        store.SaveGate.SetResult();
        BackgroundWork.TickUntil(() => c.All<LogoutStatus>().Count > 0, () => w.Tick(1, c), "o Done");

        Assert.Equal([LogoutState.Done], States(c));
        Assert.Equal(saves + 1, store.Saves); // gravado antes do Done: a lista da API já vem certa
        var saved = store.Row(row.Id);
        Assert.Equal("zone.city", saved.ZoneId);
        Assert.Equal(spot.X, saved.PositionX, 0.01f);
        Assert.Equal(spot.Y, saved.PositionY, 0.01f);
        w.Tick(CloseDelay, c);
        Assert.True(c.Raw.Closed);

        // Logo depois do Done entra de novo, onde saiu.
        var back = w.JoinWithTicket(AccountRig.Ticket(row));
        Assert.Equal("zone.city", back.Welcome!.EncounterId);
        Assert.Equal(spot, w.Where(back).Entity.Get<Transform>().Position);
    }

    [Fact]
    public void Failed_save_still_sends_done_and_the_save_is_retried()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        w.Tick(1, c);
        w.Flush();
        var saves = store.Saves;

        store.FailNextSaves = 1;
        c.Send(new LogoutRequest());
        BackgroundWork.TickUntil(() => c.All<LogoutStatus>().Count > 0, () => w.Tick(1, c), "o Done");

        Assert.Equal([LogoutState.Done], States(c));
        w.Flush(); // a repetição grava depois
        Assert.Equal(saves + 1, store.Saves);
    }

    [Fact]
    public void Stuck_save_sends_done_after_the_safety_timeout()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        w.Tick(1, c);
        w.Flush();

        store.SaveGate = new TaskCompletionSource();
        c.Send(new LogoutRequest());
        w.Tick(WorldServer.LogoutSaveTimeoutTicks, c);
        Assert.Empty(c.All<LogoutStatus>());

        w.Tick(1, c);
        Assert.Equal([LogoutState.Done], States(c));
        store.SaveGate.SetResult();
        w.Flush();
    }

    [Fact]
    public void Account_character_in_combat_is_saved_at_the_end_of_the_wait()
    {
        var store = new FakeStore();
        var row = store.Add();
        using var w = new WorldRig(store, devLogin: false);
        var c = w.JoinWithTicket(AccountRig.Ticket(row));
        w.Tick(1, c);
        w.Flush();
        var saves = store.Saves;

        var waiting = RequestInCombat(w, c);
        while (w.City.World.Tick < waiting.Tick - 1)
        {
            w.Tick(1, c);
        }

        Assert.True(InWorld(w, c));
        BackgroundWork.TickUntil(() => c.All<LogoutStatus>().Count > 1, () => w.Tick(1, c), "o Done");
        Assert.Equal([LogoutState.Waiting, LogoutState.Done], States(c));
        Assert.True(store.Saves > saves);
    }
}
