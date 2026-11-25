using Game.Core.Authority;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using MmoRpgClient.Input;
using Vec2 = System.Numerics.Vector2;

namespace Game.Client.Tests;

/// <summary>
/// T-386: com um alvo escolhido pelo Tab, o ataque básico (clique, clique mantido, Espaço e automático) vai nele e mira
/// nele, mesmo com o cursor longe; clicar em outro monstro troca o alvo; fora do alcance, o jogador é avisado.
/// </summary>
public class PlayerInputTests
{
    private const string Basic = "warrior.sword_strike";
    private static readonly Lazy<ContentCatalog> Content = new(SkillPlanTests.LoadContent);

    private static readonly EntityId Player = new(1);
    private static readonly EntityId Wolf = new(2);
    private static readonly EntityId OtherWolf = new(3);

    /// <summary>Cursor longe de tudo (canto do mapa), como no relato do playtest.</summary>
    private static readonly Vec2 FarCursor = new(1, 18);

    [Fact]
    public void Click_with_the_cursor_away_goes_to_the_tab_target()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        Press(input, keys, authority, InputActions.TargetNext);
        Assert.Equal(Wolf, input.SelectedTarget);

        var use = Press(input, keys, authority, InputActions.Attack);

        Assert.Equal(Wolf, use.Target);
        Assert.Equal(new Vec2(6.5f, 10.5f), use.AimPoint); // mira no lobo, não no cursor
    }

    [Fact]
    public void Held_click_with_the_cursor_away_keeps_hitting_the_tab_target()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        Press(input, keys, authority, InputActions.TargetNext);

        keys.Held.Add(InputActions.Attack);
        input.Update(authority, FarCursor, hovered: null);

        var use = Assert.Single(authority.Sent.OfType<UseAbilityIntent>());
        Assert.Equal(Wolf, use.Target);
        Assert.Equal(new Vec2(6.5f, 10.5f), use.AimPoint);
    }

    [Fact]
    public void Space_and_auto_attack_go_to_the_tab_target()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        Press(input, keys, authority, InputActions.TargetNext);

        var auto = Assert.Single(authority.Sent.OfType<UseAbilityIntent>()); // automático no mesmo frame do Tab
        Assert.Equal(Wolf, auto.Target);

        authority.Sent.Clear();
        authority.Tick += 100; // recarga passou
        var space = Press(input, keys, authority, InputActions.AttackKey);
        Assert.Equal(Wolf, space.Target);
        Assert.Equal(new Vec2(6.5f, 10.5f), space.AimPoint);
    }

    [Fact]
    public void Auto_attack_works_after_moving_to_a_zone_whose_tick_is_lower()
    {
        // Cada zona tem o seu próprio tick: o último envio veio da zona anterior, num tick maior que o da nova.
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        authority.Tick = 50_000;
        Press(input, keys, authority, InputActions.AttackKey);

        input.ClearTarget();
        authority.Sent.Clear();
        authority.Tick = 200;
        Press(input, keys, authority, InputActions.TargetNext);

        var auto = Assert.Single(authority.Sent.OfType<UseAbilityIntent>()); // só o Tab, sem clique nem Espaço
        Assert.Equal(Wolf, auto.Target);
    }

    [Fact]
    public void Clicking_another_monster_switches_the_target()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f), otherAt: new Vec2(5.5f, 11.8f));
        Press(input, keys, authority, InputActions.TargetNext);
        var first = input.SelectedTarget;
        var other = first == Wolf ? OtherWolf : Wolf;
        authority.Sent.Clear();
        authority.Tick += 100;

        keys.JustPressed.Add(InputActions.Attack);
        input.Update(authority, authority.PositionOf(other), hovered: other);
        keys.JustPressed.Clear();

        Assert.Equal(other, input.SelectedTarget);
        var use = Assert.Single(authority.Sent.OfType<UseAbilityIntent>());
        Assert.Equal(other, use.Target);
        Assert.Equal(authority.PositionOf(other), use.AimPoint);
    }

    [Fact]
    public void Without_a_target_the_click_still_aims_at_the_cursor()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        var use = Press(input, keys, authority, InputActions.Attack);

        Assert.Null(input.SelectedTarget);
        Assert.Equal(FarCursor, use.AimPoint);
    }

    [Fact]
    public void Tab_onto_a_far_target_warns_out_of_reach_and_does_not_attack()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(15.5f, 10.5f));
        var warnings = 0;
        input.OutOfReach = () => warnings++;

        Press(input, keys, authority, InputActions.TargetNext);

        Assert.Equal(Wolf, input.SelectedTarget);
        Assert.Equal(1, warnings);
        Assert.Empty(authority.Sent.OfType<UseAbilityIntent>()); // o automático espera o jogador chegar perto

        input.Update(authority, FarCursor, hovered: null);
        Assert.Equal(1, warnings); // não repete a cada frame

        Press(input, keys, authority, InputActions.AttackKey);
        Assert.Equal(2, warnings); // apertar o ataque longe também avisa
    }

    [Fact]
    public void Tab_onto_a_near_target_does_not_warn()
    {
        var (input, keys, authority) = Setup(wolfAt: new Vec2(6.5f, 10.5f));
        var warnings = 0;
        input.OutOfReach = () => warnings++;

        Press(input, keys, authority, InputActions.TargetNext);
        Press(input, keys, authority, InputActions.Attack);

        Assert.Equal(0, warnings);
    }

    private static UseAbilityIntent Press(PlayerInput input, Keys keys, FakeAuthority authority, string action)
    {
        var before = authority.Sent.Count;
        keys.JustPressed.Add(action);
        input.Update(authority, FarCursor, hovered: null);
        keys.JustPressed.Clear();
        return authority.Sent.Skip(before).OfType<UseAbilityIntent>().LastOrDefault()!;
    }

    private static (PlayerInput Input, Keys Keys, FakeAuthority Authority) Setup(Vec2 wolfAt, Vec2? otherAt = null)
    {
        var keys = new Keys();
        var abilities = new Dictionary<string, Game.Core.Definitions.AbilityDefinition>
        {
            [Basic] = Content.Value.AbilityAt(Basic, 1)!,
        };
        var input = new PlayerInput(abilities, keys.JustPressed.Contains, a => keys.Held.Contains(a) || keys.JustPressed.Contains(a), _ => false);

        var state = new AbilityState { BasicAttackId = Basic };
        state.Known.Add(Basic);
        var entities = new List<EntitySnapshot>
        {
            Unit(Player, new Vec2(5.5f, 10.5f), Faction.Player, state),
            Unit(Wolf, wolfAt, Faction.Monster),
        };
        if (otherAt is { } other)
        {
            entities.Add(Unit(OtherWolf, other, Faction.Monster));
        }

        return (input, keys, new FakeAuthority(entities));
    }

    private static EntitySnapshot Unit(EntityId id, Vec2 at, Faction faction, AbilityState? abilities = null)
    {
        var components = new List<IComponent>
        {
            new Transform { Position = at },
            new Health(100) { Current = 100 },
            new FactionTag(faction),
            new Collider(0.4f),
        };
        if (abilities is not null)
        {
            components.Add(abilities);
        }

        return EntitySnapshot.Create(id, components);
    }

    private sealed class Keys
    {
        public HashSet<string> JustPressed { get; } = new(StringComparer.Ordinal);

        public HashSet<string> Held { get; } = new(StringComparer.Ordinal);
    }

    private sealed class FakeAuthority(IReadOnlyList<EntitySnapshot> entities) : IGameAuthority
    {
        public List<Intent> Sent { get; } = new();

        public long Tick { get; set; } = 10;

        public EntityId? ControlledEntity => Player;

        public long CurrentTick => Tick;

        public float InterpolationFraction => 1f;

        public WorldSnapshot? Latest => new(Tick, entities);

        public WorldSnapshot? Previous => null;

        public Vec2 PositionOf(EntityId id) => entities.First(e => e.Id == id).Get<Transform>().Position;

        public void Advance(double deltaSeconds)
        {
        }

        public void Send(Intent intent) => Sent.Add(intent);

        public IReadOnlyList<GameEvent> TakeEvents() => Array.Empty<GameEvent>();
    }
}
