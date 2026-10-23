using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Resources;

/// <summary>
/// T-010. Lê os eventos do tick (dano, cura, escudo, interrupção, habilidade) e avalia as
/// <c>resourceRules</c> da classe de cada entidade; depois aplica regeneração/decaimento passivo.
/// Roda em <see cref="SystemOrder.Resources"/> (depois de habilidades e status).
/// </summary>
public sealed class ResourceSystem : ISimulationSystem
{
    public void Update(World world)
    {
        var services = ResourceModule.For(world.Combat);
        var events = world.Events.Current.ToList(); // cópia: o sistema publica ResourceChanged durante o laço

        foreach (var ev in events)
        {
            switch (ev)
            {
                case DamageDealt d:
                    Touch(world, d.Source);
                    Touch(world, d.Target);
                    Fire(world, services, ResourceTrigger.DamageTaken, d.Target, d.Source, d.Target, d.Amount, d.SourceId);
                    if (d.Source != EntityId.None)
                    {
                        Fire(world, services, ResourceTrigger.DamageDealt, d.Source, d.Source, d.Target, d.Amount, d.SourceId);
                    }

                    break;
                case HealApplied h:
                    Touch(world, h.Source);
                    if (h.Effective > 0 && h.Source != EntityId.None)
                    {
                        Fire(world, services, ResourceTrigger.HealDone, h.Source, h.Source, h.Target, h.Effective, h.SourceId);
                    }

                    break;
                case ShieldApplied s:
                    Touch(world, s.Source);
                    if (s.Source != EntityId.None)
                    {
                        Fire(world, services, ResourceTrigger.ShieldApplied, s.Source, s.Source, s.Target, s.Amount, s.SourceId);
                    }

                    break;
                case CastInterrupted { Reason: CastInterruptReason.Interrupted, By: { } by } ci:
                    Fire(world, services, ResourceTrigger.InterruptDone, by, by, ci.Entity, 0, ci.AbilityId);
                    break;
                case AbilityExecuted a:
                    Touch(world, a.Source);
                    if (a.CostResource is not null && a.CostAmount > 0 && world.Find(a.Source) is { } caster)
                    {
                        PublishCost(world, caster, a);
                    }

                    var first = a.Targets.Count > 0 ? a.Targets[0] : EntityId.None;
                    Fire(world, services, ResourceTrigger.AbilityExecuted, a.Source, a.Source, first, 0, a.AbilityId);
                    break;
            }
        }

        foreach (var n in services.TakeRemovals())
        {
            Fire(world, services, ResourceTrigger.StatusRemoved, n.Remover, n.Remover, n.Holder, 0, n.SourceId, n.StatusTags);
        }

        Regenerate(world);
    }

    private static void PublishCost(World world, Entity caster, AbilityExecuted a)
    {
        var pool = caster.TryGet<ResourcePool>();
        if (pool is not null && pool.Type == a.CostResource)
        {
            world.Events.Publish(new ResourceChanged(
                caster.Id, pool.Type, -a.CostAmount, pool.Current, pool.Max, ResourceChangeReason.Cost, a.AbilityId));
        }
    }

    private static void Touch(World world, EntityId id)
    {
        if (id != EntityId.None && world.Find(id) is { } e && e.Has<ResourcePool>())
        {
            StateOf(e).LastCombatTick = world.Tick;
        }
    }

    private static ResourceState StateOf(Entity e)
    {
        var s = e.TryGet<ResourceState>();
        if (s is null)
        {
            s = new ResourceState();
            e.Add(s);
        }

        return s;
    }

    private static IReadOnlyList<ResourceRuleDefinition> RulesOf(World world, Entity e) =>
        e.TryGet<UnitInfo>() is { } info && world.Content.Classes.TryGetValue(info.DefinitionId, out var cls)
            ? cls.ResourceRules
            : [];

    private static IReadOnlyList<string> AbilityTags(World world, string abilityId) =>
        world.Content.Abilities.TryGetValue(abilityId, out var a) ? a.Tags : [];

    private static void Fire(
        World world,
        ResourceServices services,
        ResourceTrigger trigger,
        EntityId ownerId,
        EntityId sourceId,
        EntityId targetId,
        float value,
        string originId,
        IReadOnlyList<string>? removedStatusTags = null)
    {
        if (ownerId == EntityId.None || world.Find(ownerId) is not { } owner || !owner.IsAlive)
        {
            return;
        }

        var rules = RulesOf(world, owner);
        if (rules.Count == 0)
        {
            return;
        }

        var pool = owner.TryGet<ResourcePool>();
        if (pool is null)
        {
            return;
        }

        var source = sourceId == EntityId.None ? null : world.Find(sourceId);
        var target = targetId == EntityId.None ? null : world.Find(targetId);
        var abilityTags = AbilityTags(world, originId);

        foreach (var rule in rules)
        {
            if (rule.Trigger != trigger)
            {
                continue;
            }

            var state = StateOf(owner);
            if (state.RuleReadyAt.TryGetValue(rule.Id, out var ready) && world.Tick < ready)
            {
                continue;
            }

            if (!ConditionsMet(world, services, rule, owner, source, target, value, abilityTags, removedStatusTags))
            {
                continue;
            }

            var amount = (float)(rule.Amount + rule.Ratio * value);
            if (amount == 0)
            {
                continue;
            }

            if (rule.InternalCooldownTicks > 0)
            {
                state.RuleReadyAt[rule.Id] = world.Tick + rule.InternalCooldownTicks;
            }

            ResourceModule.Change(world, owner, pool.Type, amount, ResourceChangeReason.Rule, rule.Id);
        }
    }

    private static bool ConditionsMet(
        World world,
        ResourceServices services,
        ResourceRuleDefinition r,
        Entity owner,
        Entity? source,
        Entity? target,
        float value,
        IReadOnlyList<string> abilityTags,
        IReadOnlyList<string>? removedStatusTags)
    {
        if (r.MinValue is { } min && value < min)
        {
            return false;
        }

        if (r.AbilityTag is { } at && !abilityTags.Contains(at))
        {
            return false;
        }

        if (r.RemovedStatusTag is { } rt && removedStatusTags?.Contains(rt) != true)
        {
            return false;
        }

        if (r.TargetTag is { } tt && target?.TryGet<UnitInfo>()?.HasTag(tt) != true)
        {
            return false;
        }

        if (r.MinDistance is not null || r.MaxDistance is not null)
        {
            if (source?.TryGet<Transform>() is not { } sp || target?.TryGet<Transform>() is not { } tp)
            {
                return false;
            }

            var dist = System.Numerics.Vector2.Distance(sp.Position, tp.Position);
            if ((r.MinDistance is { } lo && dist < lo) || (r.MaxDistance is { } hi && dist > hi))
            {
                return false;
            }
        }

        if (r.TargetHasStatusTag is { } ts && (target is null || !services.StatusQuery.HasStatusWithTag(world, target, ts)))
        {
            return false;
        }

        if (r.SourceHasStatusTag is { } ss && !services.StatusQuery.HasStatusWithTag(world, owner, ss))
        {
            return false;
        }

        if (r.MinEffectiveHealPercent is { } pct)
        {
            var max = target?.TryGet<Health>()?.Max ?? 0;
            if (max <= 0 || value / max * 100.0 < pct)
            {
                return false;
            }
        }

        return true;
    }

    private static void Regenerate(World world)
    {
        foreach (var e in world.Entities)
        {
            if (!e.IsAlive || e.TryGet<ResourcePool>() is not { } pool || e.TryGet<UnitInfo>() is not { } info)
            {
                continue;
            }

            var regen = world.Content.Classes.TryGetValue(info.DefinitionId, out var cls) ? cls.Resource.RegenPerSecond : 0;
            if (cls is not null && e.TryGet<Stats.CharacterStats>() is { } stats && cls.Resource.Type == EntityFactory.ManaResource)
            {
                regen += Stats.StatMath.ManaRegenBonus(stats, world.Content.Rules.Stats); // T-201: + INT × 0,05 por segundo
            }

            if (cls is null || regen == 0)
            {
                continue;
            }

            var state = StateOf(e);
            if (cls.Resource.PassiveOnlyOutOfCombat && world.Tick - state.LastCombatTick < cls.Resource.CombatTimeoutTicks)
            {
                state.RegenRemainder = 0;
                continue;
            }

            state.RegenRemainder += (float)(regen / World.TickRate);
            var whole = MathF.Truncate(state.RegenRemainder);
            if (whole == 0)
            {
                continue;
            }

            state.RegenRemainder -= whole;
            var applied = ResourceModule.Change(world, e, pool.Type, whole, ResourceChangeReason.Regen, "passive");
            if (applied == 0)
            {
                state.RegenRemainder = 0; // no limite (cheio/vazio): não acumula
            }
        }
    }
}
