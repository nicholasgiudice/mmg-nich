using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Ai;
using Game.Core.Combat.Rest;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Server.Interest;
using Game.Server.Persistence;

namespace Game.Server.Zones;

/// <summary>
/// Uma zona do mundo aberto (T-214): <see cref="Core.Simulation.World"/> próprio com o mapa da arena, grade de interesse e os
/// pontos de surgimento da <see cref="ZoneDefinition"/>. Nunca termina: monstro morto vira corpo, some depois de
/// <see cref="ZoneDefinition.CorpseTicks"/> e renasce em volta do ponto depois de <see cref="MonsterSpawnDefinition.RespawnTicks"/>.
/// Monstros sem ponto (invocações) também viram corpo e somem no mesmo prazo, mas não renascem (T-301). Cada ponto é um grupo
/// de aggro próprio (<see cref="AiState.GroupId"/> = índice do ponto + 1; o 0 fica para os encontros): puxar um monstro puxa só
/// os do mesmo ponto e as invocações deles.
/// Roda na thread do loop do <see cref="WorldServer"/>.
/// <para>T-350: a mesma classe roda as cópias de instância (<see cref="InstanceId"/> ≥ 1), em que o monstro morto vira corpo,
/// some e não renasce.</para>
/// </summary>
public sealed class OpenZone
{
    private readonly List<SpawnSlot> _slots = [];
    private readonly HashSet<EntityId> _slotEntities = [];

    /// <summary>Tick da morte dos monstros sem ponto de surgimento (invocações), até o corpo sumir.</summary>
    private readonly Dictionary<EntityId, long> _looseCorpses = [];

    public OpenZone(ContentCatalog content, ZoneDefinition definition, ulong seed, ServerOptions options, int instanceId = 0)
    {
        Definition = definition;
        InstanceId = instanceId;
        Arena = content.Arenas[definition.Arena];
        World = WorldBuilder.CreateDefault(seed, EntityFactory.BuildMap(Arena), content);
        Grid = new InterestGrid(options.InterestCellSize, options.InterestRadiusCells);
        for (var p = 0; p < definition.MonsterSpawns.Count; p++)
        {
            var point = definition.MonsterSpawns[p];
            for (var i = 0; i < point.Count; i++)
            {
                var slot = new SpawnSlot(point, content.Monsters[point.Monster], GroupOf(p));
                Spawn(slot);
                _slots.Add(slot);
            }
        }

        // T-321: NPCs do mapa (sem vida nem IA; a loja vem da propriedade "shop").
        foreach (var npc in definition.Npcs)
        {
            Npcs.Add(Core.Combat.Shops.Shop.SpawnNpc(World, npc));
        }
    }

    /// <summary>NPCs da zona (T-321), na ordem do mapa.</summary>
    public List<Entity> Npcs { get; } = [];

    public ZoneDefinition Definition { get; }

    public string Id => Definition.Id;

    /// <summary>Id da cópia, se esta zona é uma instância de dungeon (T-350); 0 = zona aberta.</summary>
    public int InstanceId { get; }

    /// <summary>Monstros renascem? Não numa instância (T-350): nascem uma vez.</summary>
    public bool Respawns => Definition.Kind != ZoneKind.Instance;

    public ArenaDefinition Arena { get; }

    public Core.Simulation.World World { get; }

    public InterestGrid Grid { get; }

    /// <summary>Grupo de aggro do ponto <paramref name="spawnIndex"/> de <see cref="ZoneDefinition.MonsterSpawns"/> (T-301).</summary>
    public static int GroupOf(int spawnIndex) => spawnIndex + 1;

    public Vector2 SpawnPosition => new((float)Definition.Spawn.X, (float)Definition.Spawn.Y);

    /// <summary>Monstros vivos dos pontos de surgimento.</summary>
    public int LivingMonsters => _slots.Count(s => s.Entity is { IsAlive: true });

    /// <summary>
    /// Põe o personagem na zona com o estado dele, em <paramref name="position"/> (ou no nascimento, se a posição é (0, 0) ou
    /// parede). <paramref name="vitals"/> mantém vida e Mana atuais (troca de zona); null = cheias.
    /// </summary>
    public Entity AddPlayer(CharacterState state, ClassDefinition cls, Vector2 position, (float Health, float Mana)? vitals, List<string>? dropped = null)
    {
        var e = state.Spawn(World, cls, IsFree(position) ? position : SpawnPosition, dropped);
        if (Definition.RestPerSecond > 0)
        {
            e.Add(new Resting(Definition.RestPerSecond));
        }

        if (vitals is { } v)
        {
            var health = e.Get<Health>();
            health.Current = Math.Clamp(v.Health, 1, health.Max);
            if (e.TryGet<ResourcePool>() is { } pool)
            {
                pool.Current = Math.Clamp(v.Mana, 0, pool.Max);
            }
        }

        return e;
    }

    /// <summary>Portão em que a posição está, se algum.</summary>
    public GateDefinition? GateAt(Vector2 position) => Definition.Gates.FirstOrDefault(g => g.Area.Contains(position.X, position.Y));

    /// <summary>Um tick: simulação e depois corpos e renascimentos.</summary>
    public void Step()
    {
        World.Step();
        foreach (var slot in _slots)
        {
            Update(slot);
        }

        RemoveLooseCorpses();
    }

    /// <summary>Corpos de monstros que não vêm de ponto de surgimento (invocações): somem depois do prazo e não renascem.</summary>
    private void RemoveLooseCorpses()
    {
        var tick = World.Tick;
        List<EntityId>? expired = null;
        foreach (var e in World.Entities)
        {
            if (e.IsAlive || _slotEntities.Contains(e.Id) || e.TryGet<FactionTag>()?.Faction != Faction.Monster)
            {
                continue;
            }

            if (!_looseCorpses.TryGetValue(e.Id, out var diedAt))
            {
                _looseCorpses[e.Id] = tick;
            }
            else if (tick - diedAt >= Definition.CorpseTicks)
            {
                (expired ??= []).Add(e.Id);
            }
        }

        foreach (var id in expired ?? [])
        {
            World.Remove(id);
            _looseCorpses.Remove(id);
        }

        // Corpo removido por outro caminho (ex.: invocação desfeita): esquece.
        foreach (var id in _looseCorpses.Keys.Where(id => World.Find(id) is null).ToList())
        {
            _looseCorpses.Remove(id);
        }
    }

    private void Update(SpawnSlot slot)
    {
        var tick = World.Tick;
        if (slot.Entity is { } e)
        {
            if (e.IsAlive)
            {
                return;
            }

            slot.DiedAt ??= tick;
            if (!e.IsRemoved && tick - slot.DiedAt < Definition.CorpseTicks)
            {
                return;
            }

            if (!e.IsRemoved)
            {
                World.Remove(e.Id);
            }

            _slotEntities.Remove(e.Id);
            slot.Entity = null;
            slot.RespawnAt = Respawns ? slot.DiedAt + slot.Point.RespawnTicks : null; // T-350: instância não renasce
            slot.DiedAt = null;
        }

        if (tick >= slot.RespawnAt)
        {
            Spawn(slot);
        }
    }

    private void Spawn(SpawnSlot slot)
    {
        var center = new Vector2((float)slot.Point.Position.X, (float)slot.Point.Position.Y);
        var position = center;
        for (var attempt = 0; attempt < 8 && slot.Point.Radius > 0; attempt++)
        {
            var angle = World.Random.NextFloat() * MathF.Tau;
            var distance = World.Random.NextFloat() * (float)slot.Point.Radius;
            var candidate = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance);
            if (IsFree(candidate))
            {
                position = candidate;
                break;
            }
        }

        var monster = EntityFactory.SpawnMonster(World, slot.Monster, position, slot.Point.Level);
        if (AiState.Ensure(World, monster) is { } ai)
        {
            ai.GroupId = slot.GroupId;
        }

        slot.Entity = monster;
        _slotEntities.Add(monster.Id);
        slot.RespawnAt = null;
    }

    /// <summary>Posição dentro do mapa, fora de parede e diferente de (0, 0) (que quer dizer "sem posição").</summary>
    private bool IsFree(Vector2 p) => p != Vector2.Zero && World.Map is { } map && !map.IsBlocked(p);

    private sealed class SpawnSlot(MonsterSpawnDefinition point, MonsterDefinition monster, int groupId)
    {
        public int GroupId { get; } = groupId;
        public MonsterSpawnDefinition Point { get; } = point;
        public MonsterDefinition Monster { get; } = monster;
        public Entity? Entity { get; set; }
        public long? DiedAt { get; set; }
        public long? RespawnAt { get; set; }
    }
}
