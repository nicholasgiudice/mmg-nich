using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;

namespace Game.Core.Combat.Status;

/// <summary>API pública de status: aplicar, remover e consultar. Todo o resto do jogo deve passar por aqui.</summary>
public static class StatusManager
{
    public const string ControlTag = "control";

    // ---------- consulta ----------

    /// <summary>Ativo no tick atual (expiração exata: com duração D aplicada no tick T, ativo em T..T+D-1).</summary>
    public static bool IsActive(World world, StatusInstance s) => s.ExpiresAtTick > world.Tick;

    public static IEnumerable<StatusInstance> Active(World world, Entity entity) =>
        entity.TryGet<StatusEffects>()?.Active.Where(s => IsActive(world, s)) ?? Enumerable.Empty<StatusInstance>();

    public static StatusInstance? Get(World world, Entity entity, string statusId) =>
        Active(world, entity).FirstOrDefault(s => s.StatusId == statusId);

    public static bool Has(World world, Entity entity, string statusId) => Get(world, entity, statusId) is not null;

    public static bool HasTag(World world, Entity entity, string tag) =>
        Active(world, entity).Any(s => Def(world, s).Tags.Contains(tag));

    public static int Stacks(World world, Entity entity, string statusId) => Get(world, entity, statusId)?.Stacks ?? 0;

    public static bool IsStunned(World world, Entity e) => Active(world, e).Any(s => Def(world, s).Flags.Stunned);

    public static bool IsRooted(World world, Entity e) => Active(world, e).Any(s => Def(world, s).Flags.Rooted);

    public static bool IsSilenced(World world, Entity e) => Active(world, e).Any(s => Def(world, s).Flags.Silenced);

    internal static StatusDefinition Def(World world, StatusInstance s) => world.Content.Statuses[s.StatusId];

    // ---------- aplicação ----------

    /// <summary>
    /// Aplica um status. Retorna a instância (nova ou renovada) ou null se o alvo é inválido/morto ou resistiu (controle imune).
    /// <paramref name="durationTicks"/> null = duração padrão da definição. Controle (tag "control") passa pelas regras de rank e retorno decrescente.
    /// </summary>
    public static StatusInstance? Apply(World world, Entity? source, Entity target, string statusId, int? durationTicks = null)
    {
        if (!target.IsAlive)
        {
            return null;
        }

        if (!world.Content.Statuses.TryGetValue(statusId, out var def))
        {
            throw new InvalidOperationException($"Unknown status '{statusId}' (not in ContentCatalog.Statuses)");
        }

        var sourceId = source?.Id ?? EntityId.None;
        var effects = target.TryGet<StatusEffects>();
        if (effects is null)
        {
            effects = new StatusEffects();
            target.Add(effects);
        }

        var duration = durationTicks ?? def.DefaultDurationTicks;
        if (def.Tags.Contains(ControlTag))
        {
            var rules = world.Content.Rules.Control ?? ControlRules.Neutral;
            var rank = target.TryGet<UnitInfo>()?.Rank ?? MonsterRank.Normal;
            var factor = rules.DurationByRank.For(rank) * NextDiminishingStep(world, effects, rules.Diminishing);
            duration = factor <= 0 ? 0 : Math.Max(1, (int)Math.Round(duration * factor, MidpointRounding.AwayFromZero));
            if (duration == 0)
            {
                world.Events.Publish(new StatusResisted(sourceId, target.Id, statusId));
                return null;
            }
        }

        var expires = world.Tick + duration;
        var inst = effects.Active.FirstOrDefault(s => s.StatusId == statusId && IsActive(world, s));
        if (inst is null)
        {
            inst = new StatusInstance
            {
                Id = effects.NextInstanceId++,
                StatusId = statusId,
                Source = sourceId,
                AppliedAtTick = world.Tick,
                ExpiresAtTick = expires,
                NextPeriodicTick = def.Periodic.Select(p => world.Tick + p.IntervalTicks).ToArray(),
            };
            effects.Active.RemoveAll(s => s.StatusId == statusId); // restos expirados ainda não limpos
            effects.Active.Add(inst);
            world.Events.Publish(new StatusApplied(sourceId, target.Id, statusId, inst.Id, 1, expires, duration));
        }
        else
        {
            inst.Source = sourceId;
            inst.ExpiresAtTick = expires;
            if (def.Stacking == StackingMode.Stack && inst.Stacks < def.MaxStacks)
            {
                inst.Stacks++;
                world.Events.Publish(new StatusStacked(sourceId, target.Id, statusId, inst.Id, inst.Stacks, expires));
            }
            else
            {
                world.Events.Publish(new StatusRefreshed(sourceId, target.Id, statusId, inst.Id, inst.Stacks, expires));
            }
        }

        if (def.Flags.Stunned)
        {
            CastControl.Cancel(world, target, CastInterruptReason.Disabled, sourceId);
        }
        else if (def.Flags.Silenced
            && target.TryGet<AbilityState>() is { Cast: { } cast } st
            && !st.IsBasicAttack(cast.AbilityId))
        {
            CastControl.Cancel(world, target, CastInterruptReason.Disabled, sourceId);
        }

        return inst;
    }

    /// <summary>Passo do retorno decrescente para esta aplicação (1 = cheio, 0 = imune) e registra a aplicação.</summary>
    private static double NextDiminishingStep(World world, StatusEffects fx, ControlDiminishing d)
    {
        if (d.Steps.Count == 0)
        {
            return 1;
        }

        if (fx.ControlCount > 0 && world.Tick >= fx.ControlWindowEndTick)
        {
            fx.ControlCount = 0;
        }

        var step = d.Steps[Math.Min(fx.ControlCount, d.Steps.Count - 1)];
        if (step <= 0)
        {
            return 0; // imune: não conta nem estende a janela
        }

        if (fx.ControlCount == 0)
        {
            fx.ControlWindowEndTick = world.Tick + d.WindowTicks;
        }

        fx.ControlCount++;
        return step;
    }

    // ---------- remoção ----------

    /// <summary>Remove até <paramref name="count"/> status com a tag, do mais antigo ao mais novo. Retorna quantos removeu.</summary>
    public static int RemoveByTag(World world, Entity target, string tag, int count, EntityId by)
    {
        var fx = target.TryGet<StatusEffects>();
        if (fx is null)
        {
            return 0;
        }

        var removed = 0;
        foreach (var s in fx.Active.ToList())
        {
            if (removed >= count)
            {
                break;
            }

            if (IsActive(world, s) && Def(world, s).Tags.Contains(tag))
            {
                fx.Active.Remove(s);
                world.Events.Publish(new StatusRemoved(target.Id, s.StatusId, s.Id, by));
                removed++;
            }
        }

        return removed;
    }

    /// <summary>Remove um status específico pelo id da definição. Retorna false se não estava ativo.</summary>
    public static bool Remove(World world, Entity target, string statusId, EntityId by)
    {
        var fx = target.TryGet<StatusEffects>();
        var s = fx?.Active.FirstOrDefault(x => x.StatusId == statusId && IsActive(world, x));
        if (fx is null || s is null)
        {
            return false;
        }

        fx.Active.Remove(s);
        world.Events.Publish(new StatusRemoved(target.Id, s.StatusId, s.Id, by));
        return true;
    }
}
