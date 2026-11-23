using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using MmoRpgClient.Input;
using Vec2 = System.Numerics.Vector2;

namespace Game.Client.Tests;

/// <summary>
/// T-434 (pendência da T-431): o cliente soma o alcance a mais dos status (Mira Firme NH3+, Olho de Falcão NH3) no aviso de
/// "fora de alcance", no ataque automático e no limite da mira das áreas, como o Core.
/// </summary>
public class RangeBonusInputTests
{
    private const string Shot = "archer.shot";
    private static readonly Lazy<ContentCatalog> Content = new(SkillPlanTests.LoadContent);
    private static readonly EntityId Player = new(1);
    private static readonly EntityId Wolf = new(2);

    /// <summary>Lobo a 10 tiles do centro (9,6 até a borda): fora dos 9 do Disparo, dentro de 9 + 2.</summary>
    private static readonly Vec2 WolfAt = new(15.5f, 10.5f);

    [Fact]
    public void Without_bonus_the_far_target_is_out_of_reach()
    {
        Assert.Equal(1, WarningsAfterTab(status: null));
    }

    [Fact]
    public void Hawk_eye_rank_3_range_bonus_brings_the_target_into_reach()
    {
        Assert.Equal(0, WarningsAfterTab("status.hawk_eye_3"));
    }

    [Fact]
    public void Clamp_aim_adds_the_range_bonus()
    {
        var cloud = Content.Value.AbilityAt("hunter.toxic_cloud", 1)!; // alcance 9
        var origin = new Vec2(0, 0);
        var far = new Vec2(20, 0);

        Assert.Equal(9f, PlayerInput.ClampAim(cloud, origin, far).X, 1);
        Assert.Equal(11f, PlayerInput.ClampAim(cloud, origin, far, rangeBonus: 2).X, 1);
    }

    private static int WarningsAfterTab(string? status)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { InputActions.TargetNext };
        var input = new PlayerInput(Content.Value, keys.Contains, keys.Contains, _ => false);
        var warnings = 0;
        input.OutOfReach = () => warnings++;

        var abilities = new AbilityState { BasicAttackId = Shot };
        abilities.Known.Add(Shot);
        var statuses = new StatusEffects();
        if (status is not null)
        {
            statuses.Active.Add(new StatusInstance { Id = 1, StatusId = status, ExpiresAtTick = 1000 });
        }

        var authority = new Authority(
        [
            EntitySnapshot.Create(Player, [new Transform { Position = new Vec2(5.5f, 10.5f) }, new Health(100) { Current = 100 },
                new FactionTag(Faction.Player), new Collider(0.4f), abilities, statuses]),
            EntitySnapshot.Create(Wolf, [new Transform { Position = WolfAt }, new Health(100) { Current = 100 },
                new FactionTag(Faction.Monster), new Collider(0.4f)]),
        ]);
        input.Update(authority, WolfAt, hovered: null);
        return warnings;
    }

    private sealed class Authority(IReadOnlyList<EntitySnapshot> entities) : IGameAuthority
    {
        public EntityId? ControlledEntity => Player;

        public long CurrentTick => 10;

        public float InterpolationFraction => 1f;

        public WorldSnapshot? Latest => new(10, entities);

        public WorldSnapshot? Previous => null;

        public Vec2 PositionOf(EntityId id) => entities.First(e => e.Id == id).Get<Transform>().Position;

        public void Advance(double deltaSeconds)
        {
        }

        public void Send(Intent intent)
        {
        }

        public IReadOnlyList<GameEvent> TakeEvents() => Array.Empty<GameEvent>();
    }
}
