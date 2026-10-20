using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Effects;

public enum EffectScope
{
    /// <summary>O handler roda uma vez para cada alvo vivo (padrão).</summary>
    PerTarget,

    /// <summary>O handler roda uma vez por uso (ex.: summon, spawn_area). <see cref="EffectContext.Target"/> é null.</summary>
    PerCast,
}

/// <summary>Dados de uma aplicação de efeito. Source pode ser null/morto (ex.: dano periódico de status).</summary>
public sealed class EffectContext
{
    public required World World { get; init; }
    public Entity? Source { get; init; }

    /// <summary>Alvo atual (PerTarget). Null em efeitos PerCast.</summary>
    public Entity? Target { get; init; }

    /// <summary>Todos os alvos resolvidos do uso.</summary>
    public required IReadOnlyList<Entity> Targets { get; init; }

    /// <summary>Id da origem do efeito (habilidade, status ou área) — vai nos eventos.</summary>
    public required string SourceId { get; init; }

    /// <summary>Habilidade de origem (null para status/área).</summary>
    public AbilityDefinition? Ability { get; init; }
    public Vector2? AimPoint { get; init; }

    /// <summary>Direção de mira normalizada a partir da fonte (facing se não houver mira).</summary>
    public Vector2 AimDirection { get; init; }

    /// <summary>
    /// Potência do uso (T-061): multiplica dano, cura e escudo. 1 = normal; saltos e alvos extras usam frações e
    /// um status que fortalece o próximo uso (Recuo Rápido) aumenta.
    /// </summary>
    public double Power { get; init; } = 1;

    /// <summary>Acerto atual (1..N) de uma habilidade com vários acertos; 1 fora disso.</summary>
    public int HitIndex { get; init; } = 1;

    public EntityId SourceEntityId => Source?.Id ?? EntityId.None;
}

/// <summary>
/// Registro de efeitos por tipo de definição (um tipo C# de <see cref="EffectDefinition"/> = um <c>type</c> do JSON).
/// Outras tarefas registram seus tipos em seus módulos sem editar o executor.
/// </summary>
public sealed class EffectRegistry
{
    private readonly Dictionary<Type, (EffectScope Scope, Action<EffectContext, EffectDefinition> Handler)> _handlers = new();

    public EffectRegistry Register<T>(Action<EffectContext, T> handler, EffectScope scope = EffectScope.PerTarget)
        where T : EffectDefinition
    {
        if (!_handlers.TryAdd(typeof(T), (scope, (c, e) => handler(c, (T)e))))
        {
            throw new InvalidOperationException($"Effect handler for {typeof(T).Name} already registered");
        }

        return this;
    }

    public bool IsRegistered(Type effectType) => _handlers.ContainsKey(effectType);

    public EffectScope ScopeOf(EffectDefinition effect) => Lookup(effect).Scope;

    public void Invoke(EffectContext ctx, EffectDefinition effect) => Lookup(effect).Handler(ctx, effect);

    private (EffectScope Scope, Action<EffectContext, EffectDefinition> Handler) Lookup(EffectDefinition effect) =>
        _handlers.TryGetValue(effect.GetType(), out var h)
            ? h
            : throw new InvalidOperationException(
                $"No handler registered for effect type '{effect.GetType().Name}' (not implemented yet, or its module is not registered in CombatModules)");
}

/// <summary>Executa listas de efeitos. Use também para efeitos periódicos de status/áreas (T-009/T-012).</summary>
public static class EffectApplier
{
    public static void Apply(
        World world,
        IReadOnlyList<EffectDefinition> effects,
        Entity? source,
        IReadOnlyList<Entity> targets,
        string sourceId,
        AbilityDefinition? ability = null,
        Vector2? aimPoint = null,
        Vector2? aimDirection = null,
        double power = 1,
        int hitIndex = 1)
    {
        var dir = aimDirection ?? source?.TryGet<Simulation.Components.Transform>()?.Facing ?? new Vector2(0, 1);
        var registry = world.Combat.Effects;
        IReadOnlyList<Entity>? selfOnly = null;
        foreach (var effect in effects)
        {
            if (effect.OnHit is { } onHit && onHit != hitIndex)
            {
                continue; // T-061: efeito só de um acerto
            }

            var scope = registry.ScopeOf(effect); // lança erro claro se não registrado
            var effectTargets = targets;
            if (effect.Target == EffectTarget.Self)
            {
                // T-017: "target": "self" aplica ao conjurador (ignorado se não há fonte viva).
                if (source is null || !source.IsAlive)
                {
                    continue;
                }

                effectTargets = selfOnly ??= [source];
            }

            if (scope == EffectScope.PerCast)
            {
                registry.Invoke(Make(world, source, null, effectTargets, sourceId, ability, aimPoint, dir, power, hitIndex), effect);
                continue;
            }

            foreach (var t in effectTargets)
            {
                if (t.IsAlive)
                {
                    registry.Invoke(Make(world, source, t, effectTargets, sourceId, ability, aimPoint, dir, power, hitIndex), effect);
                }
            }
        }
    }

    private static EffectContext Make(
        World world, Entity? source, Entity? target, IReadOnlyList<Entity> targets, string sourceId,
        AbilityDefinition? ability, Vector2? aim, Vector2 dir, double power, int hitIndex) =>
        new()
        {
            World = world,
            Source = source,
            Target = target,
            Targets = targets,
            SourceId = sourceId,
            Ability = ability,
            AimPoint = aim,
            AimDirection = dir,
            Power = power,
            HitIndex = hitIndex,
        };
}
