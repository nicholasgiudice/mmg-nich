using System.Numerics;
using Game.Core.Combat;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Bots;

/// <summary>
/// Controlador de uma entidade jogador. A cada tick lê o estado do mundo e devolve intenções
/// (nunca altera o mundo diretamente, igual a um cliente). Instâncias podem guardar estado próprio (um controlador por bot).
/// </summary>
public interface IBotController
{
    string Name { get; }

    void Think(BotContext ctx, List<Intent> output);
}

/// <summary>Tudo que um controlador pode ler no tick atual.</summary>
public sealed class BotContext(World world, Entity self, BotOptions options, IHazardSource hazards)
{
    public World World { get; } = world;
    public Entity Self { get; } = self;
    public BotOptions Options { get; } = options;
    public IHazardSource Hazards { get; } = hazards;

    public string ClassId => Self.TryGet<UnitInfo>()?.DefinitionId ?? "";
    public Vector2 Position => Self.Get<Transform>().Position;
    public float Radius => Self.TryGet<Collider>()?.Radius ?? 0f;
    public AbilityState Abilities => Self.Get<AbilityState>();
    public float HealthFraction => HealthOf(Self);

    public static float HealthOf(Entity e) =>
        e.TryGet<Health>() is { Max: > 0 } h ? h.Current / h.Max : 1f;

    public static float RadiusOf(Entity e) => e.TryGet<Collider>()?.Radius ?? 0f;

    public static Vector2 PositionOf(Entity e) => e.Get<Transform>().Position;

    /// <summary>Distância entre bordas (centro a centro menos o raio do alvo), a mesma medida usada pela validação de alcance.</summary>
    public float EdgeDistanceTo(Entity other) =>
        Vector2.Distance(Position, PositionOf(other)) - RadiusOf(other);

    public Faction Faction => Self.TryGet<FactionTag>()?.Faction ?? Faction.Player;

    /// <summary>Inimigos vivos, do mais próximo ao mais distante (desempate por id).</summary>
    public List<Entity> Enemies() => Others(e => e.Get<FactionTag>().Faction != Faction);

    /// <summary>Aliados vivos (inclui o próprio bot), ordem de id.</summary>
    public List<Entity> Allies() =>
        World.Entities.Where(e => e.IsAlive && e.TryGet<FactionTag>() is { } f && f.Faction == Faction && e.Has<Transform>()).ToList();

    private List<Entity> Others(Func<Entity, bool> pred)
    {
        var pos = Position;
        return World.Entities
            .Where(e => e.IsAlive && e.Id != Self.Id && e.Has<FactionTag>() && e.Has<Transform>() && pred(e))
            .OrderBy(e => Vector2.DistanceSquared(pos, PositionOf(e)))
            .ThenBy(e => e.Id.Value)
            .ToList();
    }

    /// <summary>
    /// Valida e, se possível, emite <see cref="UseAbilityIntent"/>. Habilidade que exige ficar parado também emite
    /// <see cref="StopIntent"/> (andar cancela a conjuração). Retorna true se emitiu.
    /// </summary>
    public bool TryUse(AbilityInfo info, EntityId? target, Vector2? aim, List<Intent> output)
    {
        var check = AbilityValidator.Validate(World, Self, info.Id, target, aim);
        if (check.Reason is not null)
        {
            return false;
        }

        if (info.NeedsStanding)
        {
            output.Add(new StopIntent(Self.Id));
        }

        output.Add(new UseAbilityIntent(Self.Id, info.Id, target, aim));
        return true;
    }
}
