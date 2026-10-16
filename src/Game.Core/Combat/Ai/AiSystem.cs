using Game.Core.Combat.Ai.Profiles;
using Game.Core.Combat.Threat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai;

/// <summary>
/// Sistema de IA e ameaça (ordem <see cref="SystemOrder.Ai"/>, depois do pipeline de habilidades).
/// 1) converte eventos do tick (dano, cura) em ameaça; 2) para cada monstro: aggro por proximidade, escolha de alvo
/// (provocação &gt; perfil/tabela) e o <see cref="IAiProfile"/> emite intenções (<c>MoveIntent</c>/<c>UseAbilityIntent</c>),
/// aplicadas no início do próximo tick, exatamente como as de um jogador.
/// </summary>
public sealed class AiSystem : ISimulationSystem
{
    private readonly AiProfileRegistry _profiles;

    public AiSystem()
        : this(AiProfileRegistry.CreateDefault())
    {
    }

    public AiSystem(AiProfileRegistry profiles)
    {
        _profiles = profiles;
    }

    public void Update(World world)
    {
        var rules = world.Content.Threat;
        ThreatFromEvents(world, rules);

        foreach (var monster in world.Entities)
        {
            if (!monster.IsAlive)
            {
                continue;
            }

            var state = AiState.Ensure(world, monster);
            if (state is null)
            {
                continue;
            }

            UpdateMonster(world, monster, state, rules);
        }
    }

    private static void ThreatFromEvents(World world, ThreatRules rules)
    {
        foreach (var d in world.Events.Of<DamageDealt>())
        {
            if (d.Amount <= 0 || world.Find(d.Target) is not { } target || world.Find(d.Source) is not { } source)
            {
                continue;
            }

            ThreatService.Add(world, target, source, d.Amount * rules.DamageMultiplier, AggroReason.Damage);
        }

        foreach (var h in world.Events.Of<HealApplied>())
        {
            if (h.Effective <= 0 || world.Find(h.Target) is not { } healed || world.Find(h.Source) is not { } healer)
            {
                continue;
            }

            foreach (var monster in world.Entities)
            {
                if (monster.TryGet<ThreatTable>() is { } t && t.Entries.ContainsKey(healed.Id.Value))
                {
                    ThreatService.Add(world, monster, healer, h.Effective * rules.HealMultiplier, AggroReason.Heal);
                }
            }
        }
    }

    private void UpdateMonster(World world, Entity monster, AiState state, ThreatRules rules)
    {
        var table = monster.Get<ThreatTable>();
        var info = monster.Get<UnitInfo>();
        var def = world.Content.Monsters[info.DefinitionId];
        var profile = _profiles.Get(def.Ai.Id);
        var ctx = new AiContext(world, monster, state, table, def);

        ThreatService.Prune(world, table);

        if (table.Entries.Count == 0 && profile is not PassiveProfile)
        {
            ProximityAggro(world, ctx, rules);
        }

        EntityId selected;
        if (table.Entries.Count == 0)
        {
            selected = EntityId.None;
        }
        else if (!table.TauntTarget.IsNone)
        {
            selected = table.TauntTarget;
        }
        else
        {
            var fromTable = ThreatService.SelectFromTable(table, state.Target, rules.OvertakeRatio)!.Value;
            selected = profile.SelectTarget(ctx, fromTable);
        }

        if (selected != state.Target)
        {
            world.Events.Publish(new TargetChanged(monster.Id, state.Target, selected));
            state.Target = selected;
        }

        if (selected.IsNone || world.Find(selected) is not { } target || !target.IsAlive)
        {
            ctx.Stop();
            return;
        }

        ctx.Target = target;
        if (ctx.Abilities.IsCasting)
        {
            ctx.Stop();
            return;
        }

        profile.Think(ctx);
    }

    private static void ProximityAggro(World world, AiContext ctx, ThreatRules rules)
    {
        var range = (float)ctx.AggroRange;
        var nearest = world.Spatial.Nearest(
            ctx.Position,
            range,
            e => e.IsAlive && e.TryGet<FactionTag>()?.Faction == Faction.Player);
        if (nearest is not null && ctx.HasLineOfSightTo(nearest))
        {
            ThreatService.Add(world, ctx.Self, nearest, rules.ProximityThreat, AggroReason.Proximity);
        }
    }
}
