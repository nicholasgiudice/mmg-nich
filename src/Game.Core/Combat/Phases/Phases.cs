using Game.Core.Combat.Areas;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Phases;

/// <summary>Fase atual de um boss. <see cref="Index"/> = -1 antes da primeira avaliação; só aumenta (cada fase é entrada uma única vez).</summary>
public sealed class PhaseState : IComponent, IReplicated
{
    public int Index { get; set; } = -1;
    public long EnteredTick { get; set; }

    public IComponent Clone() => new PhaseState { Index = Index, EnteredTick = EnteredTick };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Index);
        hasher.Add(EnteredTick);
    }
}

/// <summary>O boss entrou na fase <paramref name="Phase"/> (índice <paramref name="Index"/>); <paramref name="Previous"/> = -1 na primeira.</summary>
public sealed record PhaseChanged(
    EntityId Entity, string MonsterId, string Phase, int Previous, int Index, float HealthPercent) : GameEvent;

public static class PhaseService
{
    public static PhaseDefinition? Current(World world, Entity entity)
    {
        if (entity.TryGet<PhaseState>() is not { Index: >= 0 } st
            || entity.TryGet<UnitInfo>() is not { } info
            || !world.Content.Monsters.TryGetValue(info.DefinitionId, out var def)
            || st.Index >= def.Phases.Count)
        {
            return null;
        }

        return def.Phases[st.Index];
    }

    public static bool SummonsBlocked(World world, Entity entity) => Current(world, entity)?.NoSummons == true;
}

/// <summary>
/// Gate de fase: a fase atual limita as habilidades usáveis (<c>abilities</c>) e pode proibir invocar (<c>noSummons</c>).
/// Rejeita com <see cref="AbilityRejectReason.CannotAct"/>; a IA então escolhe outra habilidade.
/// </summary>
public sealed class PhaseGate : IActionGate
{
    public AbilityRejectReason? CanUse(World world, Entity actor, string abilityId, AbilityDefinition? ability, bool isBasicAttack)
    {
        if (PhaseService.Current(world, actor) is not { } phase)
        {
            return null;
        }

        if (phase.Abilities is { } allowed && !allowed.Contains(abilityId))
        {
            return AbilityRejectReason.CannotAct;
        }

        if (phase.NoSummons && ability is not null && ability.Effects.Any(e => e is SummonEffect))
        {
            return AbilityRejectReason.CannotAct;
        }

        return null;
    }
}

/// <summary>
/// Troca as fases de boss (ordem <see cref="SystemOrder.Phases"/>). Entra na fase seguinte quando vida% &lt;= limiar, uma vez;
/// se a vida cruzar vários limiares de uma vez, passa por todas em ordem (cada uma com seus efeitos de entrada).
/// Os efeitos <c>onEnter</c> usam o boss como fonte e alvo (summon/spawn_area são PerCast; status e dano vão no boss).
/// A primeira fase (limiar 100) é entrada no primeiro tick.
/// </summary>
public sealed class PhaseSystem : ISimulationSystem
{
    public void Update(World world)
    {
        foreach (var boss in world.Entities)
        {
            if (!boss.IsAlive || boss.TryGet<UnitInfo>() is not { } info
                || !world.Content.Monsters.TryGetValue(info.DefinitionId, out var def) || def.Phases.Count == 0
                || boss.TryGet<Health>() is not { } health)
            {
                continue;
            }

            var state = boss.TryGet<PhaseState>();
            if (state is null)
            {
                state = new PhaseState();
                boss.Add(state);
            }

            var percent = health.Max <= 0 ? 0f : health.Current / health.Max * 100f;
            while (state.Index + 1 < def.Phases.Count && percent <= def.Phases[state.Index + 1].HealthPercent && boss.IsAlive)
            {
                var previous = state.Index;
                state.Index++;
                state.EnteredTick = world.Tick;
                var phase = def.Phases[state.Index];
                world.Events.Publish(new PhaseChanged(boss.Id, def.Id, phase.Id, previous, state.Index, percent));
                if (phase.OnEnter.Count > 0)
                {
                    var aim = boss.TryGet<Transform>()?.Position;
                    EffectApplier.Apply(world, phase.OnEnter, boss, [boss], $"{def.Id}.{phase.Id}", null, aim);
                }
            }
        }
    }
}
