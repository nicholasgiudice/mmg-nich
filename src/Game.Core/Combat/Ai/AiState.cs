using System.Numerics;
using Game.Core.Combat.Threat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai;

/// <summary>
/// Estado de decisão de um monstro. Campos genéricos (<see cref="Phase"/>, <see cref="PhaseUntilTick"/>...) são
/// reservados para o perfil usar (ex.: fases do guardian, T-012 para bosses). Tudo aqui entra no hash.
/// </summary>
public sealed class AiState : IComponent
{
    /// <summary>Grupo de spawn: monstros do mesmo grupo entram em combate juntos. Padrão 0 (todos os monstros do mundo/encontro).</summary>
    public int GroupId { get; set; }

    /// <summary>Alvo atual (None = sem alvo / fora de combate).</summary>
    public EntityId Target { get; set; }

    /// <summary>True se a última intenção enviada foi de movimento (para enviar um único <c>StopIntent</c>).</summary>
    public bool Moving { get; set; }

    // ---- estado de perfil (uso livre pelo perfil) ----
    public int Phase { get; set; }
    public long PhaseUntilTick { get; set; }
    public long PhaseEmitTick { get; set; }
    public long NextFaceTick { get; set; }

    /// <summary>Alvo temporário que sobrepõe a tabela (swarm).</summary>
    public EntityId OverrideTarget { get; set; }

    public long OverrideUntilTick { get; set; }
    public long NextRetargetTick { get; set; }

    /// <summary>Até quando a matilha mantém o foco decidido pelo líder.</summary>
    public long FocusUntilTick { get; set; }

    public int LastGroupAlive { get; set; }
    public bool PendingRally { get; set; }
    public long RallyUntilTick { get; set; }

    // ---- cache de caminho ----
    public List<Vector2> Path { get; } = new();
    public Vector2 PathGoal { get; set; }
    public long PathRecalcTick { get; set; }

    /// <summary>
    /// Devolve o <see cref="AiState"/> do monstro, criando-o (e a <see cref="ThreatTable"/>) na primeira vez.
    /// Null se a entidade não é um monstro de dados.
    /// </summary>
    public static AiState? Ensure(World world, Entity entity)
    {
        if (entity.TryGet<AiState>() is { } existing)
        {
            return existing;
        }

        if (entity.TryGet<FactionTag>()?.Faction != Faction.Monster
            || entity.TryGet<UnitInfo>() is not { } info
            || !world.Content.Monsters.ContainsKey(info.DefinitionId))
        {
            return null;
        }

        var state = new AiState();
        entity.Add(state);
        if (!entity.Has<ThreatTable>())
        {
            entity.Add(new ThreatTable());
        }

        return state;
    }

    public IComponent Clone()
    {
        var c = new AiState
        {
            GroupId = GroupId,
            Target = Target,
            Moving = Moving,
            Phase = Phase,
            PhaseUntilTick = PhaseUntilTick,
            PhaseEmitTick = PhaseEmitTick,
            NextFaceTick = NextFaceTick,
            OverrideTarget = OverrideTarget,
            OverrideUntilTick = OverrideUntilTick,
            NextRetargetTick = NextRetargetTick,
            FocusUntilTick = FocusUntilTick,
            LastGroupAlive = LastGroupAlive,
            PendingRally = PendingRally,
            RallyUntilTick = RallyUntilTick,
            PathGoal = PathGoal,
            PathRecalcTick = PathRecalcTick,
        };
        c.Path.AddRange(Path);
        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(GroupId);
        hasher.Add(Target.Value);
        hasher.Add(Moving);
        hasher.Add(Phase);
        hasher.Add(PhaseUntilTick);
        hasher.Add(PhaseEmitTick);
        hasher.Add(NextFaceTick);
        hasher.Add(OverrideTarget.Value);
        hasher.Add(OverrideUntilTick);
        hasher.Add(NextRetargetTick);
        hasher.Add(FocusUntilTick);
        hasher.Add(LastGroupAlive);
        hasher.Add(PendingRally);
        hasher.Add(RallyUntilTick);
        hasher.Add(PathGoal);
        hasher.Add(PathRecalcTick);
        foreach (var p in Path)
        {
            hasher.Add(p);
        }
    }
}
