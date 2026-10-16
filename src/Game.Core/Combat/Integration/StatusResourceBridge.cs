using Game.Core.Combat.Resources;
using Game.Core.Combat.Status;
using Game.Core.Simulation;

namespace Game.Core.Combat.Integration;

/// <summary>
/// Liga o módulo de status (T-009) ao de recursos (T-010):
/// condições <c>sourceHasStatusTag</c>/<c>targetHasStatusTag</c> consultam os status reais, e
/// remoções de status com autor (purificação, dissipar) alimentam o gatilho <c>status_removed</c>.
/// </summary>
public static class StatusResourceBridge
{
    public static void Register(CombatServices s) => ResourceModule.For(s).StatusQuery = StatusTagQuery.Instance;
}

/// <summary>Consulta de tags de status ativos via <see cref="StatusManager"/>.</summary>
public sealed class StatusTagQuery : IStatusQuery
{
    public static readonly StatusTagQuery Instance = new();

    public bool HasStatusWithTag(World world, Entity entity, string tag) => StatusManager.HasTag(world, entity, tag);
}

/// <summary>
/// Lê <see cref="StatusRemoved"/> do tick e informa o sistema de recursos. Remoções sem autor
/// (<c>By = None</c>) não geram recurso. Roda entre Status (3000) e Resources (4000).
/// </summary>
public sealed class StatusRemovalBridgeSystem : ISimulationSystem
{
    public void Update(World world)
    {
        if (world.Combat is null)
        {
            return;
        }

        var resources = ResourceModule.For(world.Combat);
        foreach (var e in world.Events.Of<StatusRemoved>())
        {
            if (e.By == EntityId.None)
            {
                continue;
            }

            IReadOnlyList<string> tags = world.Content is not null && world.Content.Statuses.TryGetValue(e.StatusId, out var def)
                ? def.Tags
                : [];
            resources.NotifyStatusRemoved(new StatusRemovalNotice(e.By, e.Entity, e.StatusId, tags, e.StatusId));
        }
    }
}
