using Game.Core.Definitions;
using Game.Core.Spatial;

namespace Game.Data;

/// <summary>
/// T-331: monta a parte de posição de cada <see cref="ZoneDefinition"/> a partir do mapa do Tiled da zona, que é a fonte
/// única dela:
/// <list type="bullet">
/// <item>nascimento = <c>player_spawn</c> chamado <see cref="TiledMapReader.ZoneSpawnName"/>;</item>
/// <item>portões = objetos de <c>transitions</c> (<c>to</c>; <c>arrival</c> é o nome de um <c>player_spawn</c> do mapa de
/// destino, sem ele o nascimento do destino);</item>
/// <item>surgimentos = objetos <c>monster_spawn</c> (<c>monster</c>, <c>level</c>, <c>count</c>, <c>radius</c>,
/// <c>respawn</c>), na ordem do arquivo; a subárea é o primeiro retângulo <c>subarea</c> de <c>areas</c> que contém o ponto;</item>
/// <item>NPCs = retângulos de classe <c>npc</c> de <c>areas</c> (posição = centro).</item>
/// </list>
/// Erros vão com o arquivo do mapa e o objeto. Monstro desconhecido, ponto na parede, propriedades inválidas e chegadas que
/// não existem já são conferidos pelo <see cref="TiledMapReader"/> e pelo validador; aqui o que depende da zona.
/// </summary>
internal static class ZoneComposer
{
    /// <summary>Campos da zona (nome no JSON) que vêm do mapa e não podem estar no <c>zones.json</c>.</summary>
    public static readonly IReadOnlySet<string> FromMap = new HashSet<string>(StringComparer.Ordinal) { "spawn", "gates", "monsterSpawns", "npcs" };

    public static void Compose(RawContent c, List<LoadError> errors)
    {
        var arenas = c.Arenas.GroupBy(a => a.Value.Id).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);
        var zones = c.Zones.GroupBy(z => z.Value.Id).ToDictionary(g => g.Key, g => g.First().Value, StringComparer.Ordinal);

        ArenaDefinition? MapOf(ZoneDefinition z) => arenas.GetValueOrDefault(z.Arena) is { Source: not null } a ? a : null;

        static MapSpawn? PlayerSpawn(ArenaDefinition map, string name) =>
            map.Spawns.FirstOrDefault(s => s.Kind == MapSpawnKind.Player && s.Name == name);

        for (var i = 0; i < c.Zones.Count; i++)
        {
            var entry = c.Zones[i];
            var zone = entry.Value;
            if (!arenas.TryGetValue(zone.Arena, out var arena))
            {
                continue; // arena inexistente: o validador avisa
            }

            if (arena.Source is null)
            {
                errors.Add(new LoadError(entry.File, entry.Path + ".arena",
                    $"Zona sem nascimento: '{zone.Arena}' é uma arena ASCII. A zona precisa de um mapa do Tiled (data/maps/*.tmj), de onde vêm o nascimento, os portões e os surgimentos."));
                continue;
            }

            var spawn = PlayerSpawn(arena, TiledMapReader.ZoneSpawnName);
            if (spawn is null)
            {
                errors.Add(new LoadError(arena.Source, $"layers[{TiledMapReader.SpawnsLayer}]",
                    $"Zona '{zone.Id}' sem nascimento: crie um {TiledMapReader.PlayerSpawnClass} chamado '{TiledMapReader.ZoneSpawnName}'."));
            }

            var gates = new List<GateDefinition>();
            foreach (var t in arena.Transitions)
            {
                // Destino ou chegada inexistentes: o validador avisa (ValidateMapReferences) ou o destino já tem erro próprio.
                if (zones.GetValueOrDefault(t.To) is { } target && MapOf(target) is { } targetMap
                    && PlayerSpawn(targetMap, t.Arrival ?? TiledMapReader.ZoneSpawnName) is { } arrival)
                {
                    gates.Add(new GateDefinition { Area = t.Area, To = t.To, Arrival = arrival.Position });
                }
            }

            var subareas = arena.Areas.Where(a => a.Kind == TiledMapReader.SubareaClass).ToList();
            var monsters = new List<MonsterSpawnDefinition>();
            foreach (var s in arena.Spawns.Where(s => s.Kind == MapSpawnKind.Monster))
            {
                if (zone.Kind == ZoneKind.Safe)
                {
                    errors.Add(new LoadError(arena.Source, $"layers[{TiledMapReader.SpawnsLayer}].objects[{s.Name}]",
                        $"Zona segura '{zone.Id}' não tem monstros: tire este {TiledMapReader.MonsterSpawnClass}."));
                }

                monsters.Add(new MonsterSpawnDefinition
                {
                    Area = subareas.FirstOrDefault(a => a.Area.Contains(s.Position.X, s.Position.Y))?.Name,
                    Monster = s.Monster ?? "",
                    Position = s.Position,
                    Count = s.Count,
                    Radius = s.Radius,
                    Level = s.Level,
                    RespawnTicks = s.RespawnTicks,
                });
            }

            var npcs = arena.Areas.Where(a => a.Kind == TiledMapReader.NpcClass).Select(a => new NpcPlacementDefinition
            {
                Name = a.Name,
                Npc = a.Properties.GetValueOrDefault("npc") ?? "",
                Shop = a.Properties.GetValueOrDefault("shop"),
                Position = new PositionDefinition(a.Area.X + (a.Area.Width / 2), a.Area.Y + (a.Area.Height / 2)),
                Properties = a.Properties.Where(p => p.Key is not ("npc" or "shop")).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
            }).ToList();

            c.Zones[i] = entry with
            {
                Value = zone with
                {
                    Spawn = spawn?.Position ?? zone.Spawn,
                    Gates = gates,
                    MonsterSpawns = monsters,
                    Npcs = npcs,
                },
            };
        }

        // Conferências entre zonas (precisam do nome do objeto): nascimento dentro de um portão da própria zona, e chegada
        // dentro de um portão do destino (o personagem voltaria na hora).
        foreach (var zone in c.Zones.Select(e => e.Value).DistinctBy(z => z.Id))
        {
            if (MapOf(zone) is not { } map)
            {
                continue;
            }

            foreach (var t in map.Transitions)
            {
                var path = $"layers[{TiledMapReader.TransitionsLayer}].objects[{t.Name}]";
                if (PlayerSpawn(map, TiledMapReader.ZoneSpawnName) is { } spawn && t.Area.Contains(spawn.Position.X, spawn.Position.Y))
                {
                    errors.Add(new LoadError(map.Source!, path, $"O nascimento da zona ('{TiledMapReader.ZoneSpawnName}') está dentro desta transição."));
                }

                var arrivalName = t.Arrival ?? TiledMapReader.ZoneSpawnName;
                if (zones.GetValueOrDefault(t.To) is { } target && MapOf(target) is { } targetMap
                    && PlayerSpawn(targetMap, arrivalName) is { } arrival
                    && targetMap.Transitions.FirstOrDefault(tt => tt.Area.Contains(arrival.Position.X, arrival.Position.Y)) is { } back)
                {
                    errors.Add(new LoadError(map.Source!, path,
                        $"A chegada '{arrivalName}' em '{targetMap.Source}' fica dentro da transição '{back.Name}' de lá: o personagem voltaria na hora."));
                }
            }
        }
    }
}
