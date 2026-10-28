using System.Numerics;
using Game.Core.Combat;
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
/// Roda na thread do loop do <see cref="WorldServer"/>.
/// </summary>
public sealed class OpenZone
{
    private readonly List<SpawnSlot> _slots = [];

    public OpenZone(ContentCatalog content, ZoneDefinition definition, ulong seed, ServerOptions options)
    {
        Definition = definition;
        Arena = content.Arenas[definition.Arena];
        World = WorldBuilder.CreateDefault(seed, EntityFactory.BuildMap(Arena), content);
        Grid = new InterestGrid(options.InterestCellSize, options.InterestRadiusCells);
        foreach (var point in definition.MonsterSpawns)
        {
            for (var i = 0; i < point.Count; i++)
            {
                var slot = new SpawnSlot(point, content.Monsters[point.Monster]);
                Spawn(slot);
                _slots.Add(slot);
            }
        }
    }

    public ZoneDefinition Definition { get; }

    public string Id => Definition.Id;

    public ArenaDefinition Arena { get; }

    public Core.Simulation.World World { get; }

    public InterestGrid Grid { get; }

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

            slot.Entity = null;
            slot.RespawnAt = slot.DiedAt + slot.Point.RespawnTicks;
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

        slot.Entity = EntityFactory.SpawnMonster(World, slot.Monster, position, slot.Point.Level);
        slot.RespawnAt = null;
    }

    /// <summary>Posição dentro do mapa, fora de parede e diferente de (0, 0) (que quer dizer "sem posição").</summary>
    private bool IsFree(Vector2 p) => p != Vector2.Zero && World.Map is { } map && !map.IsBlocked(p);

    private sealed class SpawnSlot(MonsterSpawnDefinition point, MonsterDefinition monster)
    {
        public MonsterSpawnDefinition Point { get; } = point;
        public MonsterDefinition Monster { get; } = monster;
        public Entity? Entity { get; set; }
        public long? DiedAt { get; set; }
        public long? RespawnAt { get; set; }
    }
}
