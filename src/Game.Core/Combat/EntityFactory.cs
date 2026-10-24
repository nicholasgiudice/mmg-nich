using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat;

/// <summary>Cria entidades e mundos a partir de definições (o Core recebe as definições já carregadas).</summary>
public static class EntityFactory
{
    /// <summary>Tipo de recurso que a INT aumenta (Mana máxima e regeneração, T-201).</summary>
    public const string ManaResource = "mana";

    /// <param name="ranks">
    /// NH por habilidade (T-060); ausentes ficam no NH1. Valores fora do intervalo da habilidade são limitados.
    /// Assinaturas (T-062) começam no NH0: só entram nas habilidades conhecidas (barra) com NH informado aqui.
    /// </param>
    public static Entity SpawnPlayer(World world, ClassDefinition def, Vector2 position, IReadOnlyDictionary<string, int>? ranks = null)
    {
        var abilities = new AbilityState { BasicAttackId = def.BasicAttack };
        foreach (var (abilityId, rank) in ranks ?? new Dictionary<string, int>())
        {
            if (world.Content.Abilities.TryGetValue(abilityId, out var ability))
            {
                abilities.Ranks[abilityId] = Math.Clamp(rank, 1, ability.HighestRank);
            }
        }

        abilities.Known.Add(def.BasicAttack);
        foreach (var a in def.Abilities.Where(a => a != def.BasicAttack))
        {
            var signature = world.Content.Abilities.TryGetValue(a, out var ad) && ad.Signature;
            if (!signature || abilities.Ranks.ContainsKey(a))
            {
                abilities.Known.Add(a);
            }
        }

        abilities.Consumables.AddRange(def.Consumables.Where(c => !abilities.Known.Contains(c)).Distinct());

        // T-201: com perfil de atributos, vida e Mana saem das estatísticas (regras em data/rules.json, seção stats).
        // T-204: o equipamento inicial (itens de verdade) entra nas estatísticas antes da vida e da Mana.
        var stats = Stats.StatMath.FromClass(def);
        var equipment = stats is null ? null : Items.ItemMath.StartingEquipment(world.Content, def);
        if (stats is not null)
        {
            Items.ItemMath.Recompute(world.Content, def, stats, null, equipment);
        }

        var rules = world.Content.Rules.Stats;
        var health = stats is null ? def.Health : Stats.StatMath.MaxHealth(def, stats, rules);
        var resourceMax = stats is not null && def.Resource.Type == ManaResource ? Stats.StatMath.MaxMana(stats, rules) : def.Resource.Max;
        var resourceStart = def.Resource.Initial >= def.Resource.Max ? resourceMax : Math.Min(def.Resource.Initial, resourceMax);

        var player = world.Spawn(
            new Transform { Position = position },
            new Health((float)health),
            new FactionTag(Faction.Player),
            new ResourcePool(def.Resource.Type, (float)resourceMax, (float)resourceStart),
            new Movement((float)def.Speed),
            new Collider((float)def.Radius),
            new UnitInfo { DefinitionId = def.Id, Name = def.Name },
            abilities);
        if (stats is not null)
        {
            player.Add(stats);
            player.Add(equipment!);
            player.Add(new Items.Inventory(world.Content.Rules.Items.InventorySize));
        }

        return player;
    }

    /// <param name="level">Nível em que o monstro surge (T-206); null = o da definição. A vida cresce com ele.</param>
    public static Entity SpawnMonster(World world, MonsterDefinition def, Vector2 position, int? level = null)
    {
        var abilities = new AbilityState();
        abilities.Known.AddRange(def.Abilities.Distinct());
        var lvl = Math.Max(1, level ?? def.Level);
        return world.Spawn(
            new Transform { Position = position },
            new Health((float)(def.Health * world.Content.Rules.Monsters.HealthFactor(lvl, def.Rank))),
            new FactionTag(Faction.Monster),
            new Movement((float)def.Speed),
            new Collider((float)def.Radius),
            new UnitInfo
            {
                DefinitionId = def.Id,
                Name = def.Name,
                Tags = def.Tags,
                Rank = def.Rank,
                Level = lvl,
                Resistances = def.Resistances,
                Armor = def.Armor,
                Resistance = def.Resistance,
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
            .Select(p => EntityFactory.SpawnPlayer(world, content.Classes[p.Class], Pos(p.Position), p.Ranks))
            .ToList();
        var enemies = encounter.Enemies
            .Select(e => EntityFactory.SpawnMonster(world, content.Monsters[e.Monster], Pos(e.Position), e.Level ?? encounter.MonsterLevel))
            .ToList();
        return new EncounterWorld(world, players, enemies);
    }

    private static Vector2 Pos(PositionDefinition p) => new((float)p.X, (float)p.Y);
}
