using Game.Core.Combat.Threat;
using Game.Core.Simulation;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.pack</c> (Lobo): como o perseguidor, mas o grupo foca o mesmo alvo e a morte de um membro dispara a
/// habilidade com tag <c>ai.rally</c> (auto-alvo; em dados, tipicamente um <c>apply_status</c> de agressividade).
/// O líder é o membro engajado de menor id; ele escolhe o alvo pela tabela a cada <c>focusSeconds</c> (padrão 4) e os
/// demais o seguem. Provocação continua tendo prioridade (tratada antes do perfil).
/// Parâmetros: <c>focusSeconds</c>, <c>rallyWindowSeconds</c> (tempo para conseguir usar a habilidade, padrão 2).
/// </summary>
public sealed class PackProfile : MeleeChaserProfile
{
    public const string RallyTag = "ai.rally";

    public override EntityId SelectTarget(AiContext ctx, EntityId fromTable)
    {
        Entity? leader = null;
        foreach (var m in ctx.GroupMembers())
        {
            if (m.Get<ThreatTable>().Entries.Count > 0)
            {
                leader = m;
                break;
            }
        }

        if (leader is null || leader.Id == ctx.Self.Id)
        {
            var current = ctx.State.Target;
            if (ctx.Tick < ctx.State.FocusUntilTick && !current.IsNone && ctx.World.Find(current) is { IsAlive: true })
            {
                return current;
            }

            ctx.State.FocusUntilTick = ctx.Tick + ctx.ParamTicks("focusSeconds", 4);
            return fromTable;
        }

        var leaderTarget = leader.Get<AiState>().Target;
        return !leaderTarget.IsNone && ctx.World.Find(leaderTarget) is { IsAlive: true } ? leaderTarget : fromTable;
    }

    public override void Think(AiContext ctx)
    {
        var alive = ctx.GroupMembers().Count;
        var state = ctx.State;
        if (state.LastGroupAlive > alive)
        {
            state.PendingRally = true;
            state.RallyUntilTick = ctx.Tick + ctx.ParamTicks("rallyWindowSeconds", 2);
        }

        state.LastGroupAlive = alive;

        if (state.PendingRally)
        {
            if (ctx.Tick > state.RallyUntilTick || ctx.FindByTag(RallyTag) is null)
            {
                state.PendingRally = false;
            }
            else if (ctx.UseTagged(RallyTag, null))
            {
                state.PendingRally = false;
                return;
            }
        }

        base.Think(ctx);
    }
}
