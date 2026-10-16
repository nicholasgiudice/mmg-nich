using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat;

/// <summary>Cria entidades e mundos a partir de definições (o Core recebe as definições já carregadas).</summary>
public static class EntityFactory
{
    public static Entity SpawnPlayer(World world, ClassDefinition def, Vector2 position)
    {
        var abilities = new AbilityState { BasicAttackId = def.BasicAttack };
        abilities.Known.Add(def.BasicAttack);
        foreach (var a in def.Abilities.Where(a => a != def.BasicAttack))
        {
            abilities.Known.Add(a);
        }

        return world.Spawn(
            new Transform { Position = position },
            new Health((float)def.Health),
            new FactionTag(Faction.Player),
            new ResourcePool(def.Resource.Type, (float)def.Resource.Max, (float)def.Resource.Initial),
            new Movement((float)def.Speed),
            new Collider((float)def.Radius),
            new UnitInfo { DefinitionId = def.Id, Name = def.Name },
            abilities);
    }

    public static Entity SpawnMonster(World world, MonsterDefinition def, Vector2 position)
    {
        var abilities = new AbilityState();
        abilities.Known.AddRange(def.Abilities.Distinct());
        return world.Spawn(
            new Transform { Position = position },
            new Health((float)def.Health),
            new FactionTag(Faction.Monster),
            new Movement((float)def.Speed),
            new Collider((float)def.Radius),
            new UnitInfo
            {
                DefinitionId = def.Id,
                Name = def.Name,
                Tags = def.Tags,
                Rank = def.Rank,
                Resistances = def.Resistances,
            },
            abilities);
    }

    /// <summary>Mapa de colisão de uma arena (apenas '#' é parede; outros caracteres contam como chão). Scale é ignorado no protótipo.</summary>
    public static GridMap BuildMap(ArenaDefinition arena) =>
        GridMap.FromAscii(arena.Lines.Select(l => new string(l.Select(c => c == '#' ? '#' : '.').ToArray())).ToArray());
}

public sealed record EncounterWorld(World World, IReadOnlyList<Entity> Players, IReadOnlyList<Entity> Enemies);

public static class EncounterSetup
{
    /// <summary>Cria o mundo (sistemas padrão, mapa da arena, conteúdo) e spawna jogadores e inimigos do encontro.</summary>
    public static EncounterWorld Create(ulong seed, ContentCatalog content, EncounterDefinition encounter)
    {
        var arena = content.Arenas[encounter.Arena];
        var world = WorldBuilder.CreateDefault(seed, EntityFactory.BuildMap(arena), content);
        var players = encounter.Players
            .Select(p => EntityFactory.SpawnPlayer(world, content.Classes[p.Class], Pos(p.Position)))
            .ToList();
        var enemies = encounter.Enemies
            .Select(e => EntityFactory.SpawnMonster(world, content.Monsters[e.Monster], Pos(e.Position)))
            .ToList();
        return new EncounterWorld(world, players, enemies);
    }

    private static Vector2 Pos(PositionDefinition p) => new((float)p.X, (float)p.Y);
}
