using Game.Core.Combat.Status;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat.Areas;

/// <summary>
/// Terreno do mapa (T-502, <see cref="GridMap.Terrain"/>): quem está vivo, com <see cref="Movement"/> e com o centro dentro de
/// uma área recebe o estado dela; quem sai perde o estado no mesmo tick. Roda depois de tudo que move (andar, investidas,
/// empurrões; ordem <see cref="SystemOrder.Terrain"/>), então o estado do tick vale para o movimento do tick seguinte.
/// <para>
/// O estado é aplicado pelo <see cref="StatusManager.Apply"/> sem fonte (evento <c>StatusApplied</c>, aparece no snapshot como
/// os outros) e mantido em silêncio enquanto a unidade fica dentro: o fim é empurrado para <c>agora + duração padrão</c> a cada
/// tick, sem <c>StatusRefreshed</c>, e por isso nunca acaba sozinho lá dentro. Duas áreas com o mesmo estado não acumulam
/// (uma instância só). Fora de toda área dele, o terreno tira o estado que ele mesmo pôs (sem fonte). T-510: o mesmo estado
/// vindo de uma habilidade ou área de monstro (o Lodo que Puxa da Bruxa do Brejo põe <c>status.deep_water</c>, com a Bruxa como
/// fonte) não é tirado pelo terreno e acaba pela própria duração.
/// </para>
/// <para>
/// Não é controle: o estado não leva a tag <c>control</c> (sem retorno decrescente nem regra de rank) nem as tags que a
/// Purificação e as imunidades a lentidão olham. A IA não desvia do terreno.
/// </para>
/// </summary>
public sealed class TerrainSystem : ISimulationSystem
{
    public void Update(World world)
    {
        if (world.Map is not { Terrain: { Count: > 0 } terrain })
        {
            return;
        }

        var statuses = terrain.Select(t => t.Status).Distinct(StringComparer.Ordinal).ToList();
        foreach (var e in world.Entities)
        {
            if (e.TryGet<Movement>() is null || e.TryGet<Transform>() is not { } tr)
            {
                continue;
            }

            var tags = e.TryGet<UnitInfo>()?.Tags ?? [];
            foreach (var status in statuses)
            {
                var inside = e.IsAlive && terrain.Any(t => t.Status == status && t.Contains(tr.Position) && t.Affects(tags));
                var current = StatusManager.Get(world, e, status);
                if (inside && current is null)
                {
                    StatusManager.Apply(world, null, e, status);
                }
                else if (inside && !StatusManager.IsPermanent(current!))
                {
                    current!.ExpiresAtTick = world.Tick + world.Content.Statuses[status].DefaultDurationTicks;
                }
                else if (!inside && current is not null && current.Source == EntityId.None)
                {
                    StatusManager.Remove(world, e, status, EntityId.None);
                }
            }
        }
    }
}
