using System.Runtime.CompilerServices;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Resources;

/// <summary>Módulo da T-010: efeito <c>resource</c> e operações sobre o recurso.</summary>
public static class ResourceModule
{
    private static readonly ConditionalWeakTable<CombatServices, ResourceServices> Table = new();

    public static ResourceServices For(CombatServices s) => Table.GetValue(s, _ => new ResourceServices());

    public static void Register(CombatServices s)
    {
        For(s);
        s.Effects.Register<ResourceEffect>(OnResource);
    }

    private static void OnResource(EffectContext c, ResourceEffect e) =>
        Change(c.World, c.Target!, e.Resource, (float)e.Amount, ResourceChangeReason.Effect, c.SourceId);

    /// <summary>
    /// Ganha (positivo) ou gasta (negativo) recurso, limitado a 0..máx, e publica <see cref="ResourceChanged"/> se mudou.
    /// Ignora se a entidade está morta ou não usa esse recurso. Retorna a variação efetiva.
    /// </summary>
    public static float Change(World world, Entity entity, string resource, float amount, ResourceChangeReason reason, string sourceId)
    {
        var pool = entity.TryGet<ResourcePool>();
        if (pool is null || pool.Type != resource || !entity.IsAlive)
        {
            return 0;
        }

        var before = pool.Current;
        pool.Current = Math.Clamp(before + amount, 0, pool.Max);
        var delta = pool.Current - before;
        if (delta != 0)
        {
            world.Events.Publish(new ResourceChanged(entity.Id, resource, delta, pool.Current, pool.Max, reason, sourceId));
        }

        return delta;
    }
}
