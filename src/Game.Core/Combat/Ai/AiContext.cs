using System.Numerics;
using Game.Core.Combat.Threat;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Game.Core.Spatial;

namespace Game.Core.Combat.Ai;

public enum AiUseResult
{
    /// <summary>Nenhuma habilidade ofensiva ao alcance.</summary>
    OutOfReach,

    /// <summary>Há habilidade ao alcance, mas nenhuma disponível agora (recarga etc.).</summary>
    Waiting,

    /// <summary>Uma <see cref="UseAbilityIntent"/> foi emitida.</summary>
    Used,
}

/// <summary>
/// Tudo que um perfil de IA precisa: leitura do mundo e emissão de intenções (a IA nunca altera estado diretamente).
/// Habilidades com tag que começa com <c>ai.</c> são papéis especiais (<c>ai.guard</c>, <c>ai.heavy</c>, <c>ai.rally</c>...)
/// e ficam fora da seleção automática de ataque.
/// </summary>
public sealed class AiContext
{
    public const string SpecialTagPrefix = "ai.";

    public AiContext(World world, Entity self, AiState state, ThreatTable threat, MonsterDefinition definition)
    {
        World = world;
        Self = self;
        State = state;
        Threat = threat;
        Definition = definition;
        Transform = self.Get<Transform>();
        Radius = self.TryGet<Collider>()?.Radius ?? 0f;
    }

    public World World { get; }
    public Entity Self { get; }
    public AiState State { get; }
    public ThreatTable Threat { get; }
    public MonsterDefinition Definition { get; }
    public Transform Transform { get; }
    public float Radius { get; }

    /// <summary>Alvo escolhido neste tick (definido pelo sistema antes de <see cref="IAiProfile.Think"/>).</summary>
    public Entity? Target { get; internal set; }

    public Vector2 Position => Transform.Position;
    public long Tick => World.Tick;
    public ThreatRules Rules => World.Content.Threat;
    public AbilityState Abilities => Self.Get<AbilityState>();

    // ---------- parâmetros ----------

    public double Param(string key, double fallback) =>
        Definition.Ai.Parameters.TryGetValue(key, out var v) ? v : fallback;

    /// <summary>Parâmetro em segundos convertido para ticks (arredonda metades para longe do zero).</summary>
    public int ParamTicks(string key, double fallbackSeconds) =>
        (int)Math.Round(Param(key, fallbackSeconds) * World.TickRate, MidpointRounding.AwayFromZero);

    /// <summary>Raio de aggro por proximidade (parâmetro <c>aggroRange</c>, padrão 8 tiles).</summary>
    public double AggroRange => Param("aggroRange", 8);

    // ---------- consulta ----------

    /// <summary>Distância entre as bordas (centro a centro menos o raio do alvo; mesma medida do validador de habilidades).</summary>
    public float DistanceTo(Entity other) =>
        Vector2.Distance(Position, other.Get<Transform>().Position) - (other.TryGet<Collider>()?.Radius ?? 0f);

    public bool HasLineOfSightTo(Entity other) =>
        World.Map is not { } map || map.HasLineOfSight(Position, other.Get<Transform>().Position);

    /// <summary>Membros vivos do mesmo grupo e do mesmo perfil de IA (inclui o próprio), em ordem de id.</summary>
    public List<Entity> GroupMembers(bool sameProfile = true)
    {
        var list = new List<Entity>();
        foreach (var e in World.Entities)
        {
            if (!e.IsAlive || e.TryGet<AiState>() is not { } st || st.GroupId != State.GroupId)
            {
                continue;
            }

            if (sameProfile && !(e.TryGet<UnitInfo>() is { } info
                && World.Content.Monsters.TryGetValue(info.DefinitionId, out var def)
                && def.Ai.Id == Definition.Ai.Id))
            {
                continue;
            }

            list.Add(e);
        }

        return list;
    }

    // ---------- habilidades ----------

    /// <summary>Alcance de borda de uma habilidade ofensiva (null = sem alcance relevante, ex.: auto-alvo).</summary>
    public static double? ReachOf(AbilityDefinition a) => a.Targeting switch
    {
        UnitTargeting u => u.Range,
        MeleeArcTargeting m => m.Range,
        LineTargeting l => l.Length,
        CircleAtPointTargeting c => c.Range,
        CircleSelfTargeting cs => cs.Radius,
        _ => null,
    };

    public static bool IsSpecial(AbilityDefinition a) => a.Tags.Any(t => t.StartsWith(SpecialTagPrefix, StringComparison.Ordinal));

    /// <summary>Habilidades ofensivas conhecidas (na ordem de prioridade da definição), sem as de papel especial.</summary>
    public IEnumerable<AbilityDefinition> OffensiveAbilities()
    {
        foreach (var id in Abilities.Known)
        {
            if (World.Content.Abilities.TryGetValue(id, out var a) && !IsSpecial(a) && ReachOf(a) is not null)
            {
                yield return a;
            }
        }
    }

    public AbilityDefinition? FindByTag(string tag)
    {
        foreach (var id in Abilities.Known)
        {
            if (World.Content.Abilities.TryGetValue(id, out var a) && a.Tags.Contains(tag))
            {
                return a;
            }
        }

        return null;
    }

    /// <summary>Menor alcance entre as habilidades ofensivas (distância em que todas funcionam). Parâmetro <c>approachRange</c> sobrepõe.</summary>
    public double ApproachRange()
    {
        if (Definition.Ai.Parameters.TryGetValue("approachRange", out var p))
        {
            return p;
        }

        var min = double.PositiveInfinity;
        foreach (var a in OffensiveAbilities())
        {
            min = Math.Min(min, ReachOf(a)!.Value);
        }

        return double.IsInfinity(min) ? 1.0 : min;
    }

    public bool InReach(AbilityDefinition a, Entity target) => ReachOf(a) is { } r && DistanceTo(target) <= r;

    /// <summary>Tenta usar a primeira habilidade ofensiva disponível (lista = prioridade; condições = validação: recarga, alcance, LoS, recurso).</summary>
    public AiUseResult TryUseBest(Entity target, bool instantOnly = false)
    {
        var inReach = false;
        foreach (var a in OffensiveAbilities())
        {
            if (instantOnly && a.CastTimeTicks > 0 && !a.CastWhileMoving)
            {
                continue;
            }

            if (Use(a, target))
            {
                return AiUseResult.Used;
            }

            inReach |= InReach(a, target);
        }

        return inReach ? AiUseResult.Waiting : AiUseResult.OutOfReach;
    }

    /// <summary>Valida e, se possível, emite a intenção de usar a habilidade. Retorna false se rejeitada pela validação.</summary>
    public bool Use(AbilityDefinition ability, Entity? target)
    {
        var check = AbilityValidator.Validate(World, Self, ability.Id, target?.Id, target?.Get<Transform>().Position);
        if (check.Reason is not null)
        {
            return false;
        }

        if (ability.CastTimeTicks > 0 && !ability.CastWhileMoving)
        {
            Stop();
        }

        World.Enqueue(new UseAbilityIntent(Self.Id, ability.Id, target?.Id, target?.Get<Transform>().Position));
        return true;
    }

    /// <summary>Usa a primeira habilidade com a tag (papel especial).</summary>
    public bool UseTagged(string tag, Entity? target)
    {
        var a = FindByTag(tag);
        return a is not null && Use(a, target);
    }

    // ---------- movimento ----------

    public void Stop()
    {
        if (State.Moving)
        {
            World.Enqueue(new StopIntent(Self.Id));
            State.Moving = false;
        }
    }

    public void MoveDirection(Vector2 direction)
    {
        World.Enqueue(new MoveIntent(Self.Id, direction));
        State.Moving = true;
    }

    /// <summary>Vira para o ponto sem andar (movimento de 1% da velocidade na direção; ainda é só uma intenção).</summary>
    public void FaceToward(Vector2 point)
    {
        var d = point - Position;
        if (d.LengthSquared() > 1e-6f)
        {
            MoveDirection(Vector2.Normalize(d) * 0.01f);
        }
    }

    /// <summary>Anda até <paramref name="goal"/> (reto se houver linha de visão; senão por A*), parando a <paramref name="stopDistance"/>.</summary>
    public void MoveToward(Vector2 goal, float stopDistance = 0.1f)
    {
        var pos = Position;
        if (Vector2.Distance(pos, goal) <= stopDistance)
        {
            Stop();
            return;
        }

        var aim = goal;
        if (World.Map is { } map && !map.HasLineOfSight(pos, goal, Radius))
        {
            if (State.Path.Count == 0 || Vector2.DistanceSquared(State.PathGoal, goal) > 1f || Tick >= State.PathRecalcTick)
            {
                State.Path.Clear();
                State.Path.AddRange(Pathfinder.FindPath(map, pos, goal, Radius));
                State.PathGoal = goal;
                State.PathRecalcTick = Tick + World.TickRate;
            }

            while (State.Path.Count > 0 && Vector2.Distance(pos, State.Path[0]) < 0.25f)
            {
                State.Path.RemoveAt(0);
            }

            if (State.Path.Count > 0)
            {
                aim = State.Path[0];
            }
        }
        else if (State.Path.Count > 0)
        {
            State.Path.Clear();
        }

        var dir = aim - pos;
        if (dir.LengthSquared() < 1e-6f)
        {
            Stop();
            return;
        }

        MoveDirection(Vector2.Normalize(dir));
    }
}
