using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Items;
using Game.Core.Combat.Progression;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Tests;

/// <summary>T-310: XP e direito ao saque divididos com os membros do grupo que estão perto.</summary>
public class PartyTests
{
    private static readonly ContentCatalog Content = RealContent.Value;
    private static readonly ProgressionRules Rules = Content.Rules.Progression;

    private static ContentCatalog AlwaysDrops() =>
        new(Content.Classes.Values, Content.Abilities.Values, Content.Statuses.Values, Content.Areas.Values,
            Content.Monsters.Values, Content.Arenas.Values, Content.Encounters.Values, Content.Items.Values)
        {
            Rules = Content.Rules with
            {
                Items = Content.Rules.Items with
                {
                    Drops = Content.Rules.Items.Drops with { Normal = Content.Rules.Items.Drops.Normal with { Chance = 1 } },
                },
            },
            Threat = Content.Threat,
        };

    private static Entity Hero(World w, float x, int party = 0)
    {
        var e = Leveling.SpawnCharacter(w, w.Content.Classes["class.warrior"], new Vector2(x, 1), new CharacterBuild { Level = 2 });
        if (party != 0)
        {
            e.Add(new PartyMembership { PartyId = party });
        }

        return e;
    }

    private static Entity Wolf(World w)
    {
        var m = EntityFactory.SpawnMonster(w, w.Content.Monsters["monster.grey_wolf"], new Vector2(2, 1));
        m.Get<Health>().Current = 1;
        m.Get<Movement>().Speed = 0;
        return m;
    }

    private static List<GameEvent> Kill(World w, Entity attacker, Entity target)
    {
        var events = new List<GameEvent>();
        for (var t = 0; t < 200 && target.IsAlive; t++)
        {
            w.Enqueue(new UseAbilityIntent(attacker.Id, attacker.Get<AbilityState>().BasicAttackId!, target.Id));
            w.Step();
            events.AddRange(w.LastTickEvents);
        }

        Assert.False(target.IsAlive);
        return events;
    }

    private static Dictionary<EntityId, int> Gains(List<GameEvent> events) =>
        events.OfType<XpGained>().ToDictionary(g => g.Entity, g => g.Amount);

    private static readonly double WolfXp = Rules.MonsterXp(2, MonsterRank.Normal, 2); // 12

    [Fact]
    public void A_party_member_ten_tiles_away_gets_a_share_of_the_kill()
    {
        var w = WorldBuilder.CreateDefault(11, null, Content);
        var killer = Hero(w, 1, party: 1);
        var friend = Hero(w, 12, party: 1); // 10 tiles do lobo
        var wolf = Wolf(w);

        var gains = Gains(Kill(w, killer, wolf));

        var each = Rules.Share(WolfXp, 2); // 12 × 1,1 / 2 = 6,6 → 7
        Assert.Equal(7, each);
        Assert.Equal(each, gains[killer.Id]);
        Assert.Equal(each, gains[friend.Id]);
    }

    [Fact]
    public void A_party_member_forty_tiles_away_gets_nothing()
    {
        var w = WorldBuilder.CreateDefault(11, null, Content);
        var killer = Hero(w, 1, party: 1);
        var far = Hero(w, 42, party: 1); // 40 tiles do lobo
        var wolf = Wolf(w);

        var gains = Gains(Kill(w, killer, wolf));

        Assert.Equal(Rules.Share(WolfXp, 1), gains[killer.Id]);
        Assert.False(gains.ContainsKey(far.Id));
    }

    [Fact]
    public void Outside_the_party_only_the_participant_gets_xp()
    {
        var w = WorldBuilder.CreateDefault(11, null, Content);
        var killer = Hero(w, 1, party: 1);
        var stranger = Hero(w, 5); // perto, sem grupo
        var otherParty = Hero(w, 6, party: 2); // perto, outro grupo
        var wolf = Wolf(w);

        var gains = Gains(Kill(w, killer, wolf));

        Assert.Equal(12, Assert.Single(gains).Value);
        Assert.True(gains.ContainsKey(killer.Id));
        Assert.False(gains.ContainsKey(stranger.Id));
        Assert.False(gains.ContainsKey(otherParty.Id));
    }

    [Fact]
    public void Dead_party_members_get_nothing()
    {
        var w = WorldBuilder.CreateDefault(11, null, Content);
        var killer = Hero(w, 1, party: 1);
        var dead = Hero(w, 4, party: 1);
        w.Kill(dead);
        var wolf = Wolf(w);

        var gains = Gains(Kill(w, killer, wolf));

        Assert.Equal(12, Assert.Single(gains).Value);
    }

    [Fact]
    public void Party_members_who_got_xp_also_own_the_loot()
    {
        var w = WorldBuilder.CreateDefault(11, null, AlwaysDrops());
        var killer = Hero(w, 1, party: 1);
        var friend = Hero(w, 12, party: 1);
        var far = Hero(w, 42, party: 1);
        var wolf = Wolf(w);

        Kill(w, killer, wolf);

        var loot = Assert.Single(w.Entities, e => e.Has<GroundItem>()).Get<GroundItem>();
        Assert.Equal(new[] { killer.Id.Value, friend.Id.Value }, loot.Owners);
        Assert.False(loot.IsOwner(far.Id));
    }
}
