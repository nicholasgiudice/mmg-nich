using Game.Core.Definitions;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.brute</c> (Ent Corrompido, T-340): monstro lento de controle. Usa a primeira habilidade ofensiva (lista =
/// prioridade) cujo alcance cobre o alvo, sem golpear o vazio: um círculo em volta de si só sai com o alvo dentro dele.
/// As habilidades de tag <see cref="AreaTag"/> (raízes) e <see cref="RangedTag"/> (espinhos), na ordem da definição, só
/// saem com o alvo a pelo menos <c>farDistance</c> (3) tiles de borda: elas punem quem foge dele. Depois de usar uma habilidade com telegráfo, fica parado até o telegráfo
/// resolver (a preparação do golpe). Sem nada ao alcance, persegue. Parâmetros: <c>aggroRange</c>, <c>approachRange</c>,
/// <c>farDistance</c>.
/// </summary>
public sealed class BruteProfile : MeleeChaserProfile
{
    public const string AreaTag = "ai.area";
    public const string RangedTag = "ai.ranged";

    public override void Think(AiContext ctx)
    {
        var state = ctx.State;
        if (ctx.Tick < state.PhaseUntilTick)
        {
            ctx.Stop();
            return;
        }

        var target = ctx.Target!;
        var distance = ctx.DistanceTo(target);
        if (distance >= ctx.Param("farDistance", 3))
        {
            foreach (var id in ctx.Abilities.Known)
            {
                if (ctx.World.Content.Abilities.TryGetValue(id, out var a) && (a.Tags.Contains(AreaTag) || a.Tags.Contains(RangedTag))
                    && ctx.InReach(a, target) && Use(ctx, a))
                {
                    return;
                }
            }
        }

        foreach (var a in ctx.OffensiveAbilities())
        {
            if (ctx.InReach(a, target) && Use(ctx, a))
            {
                return;
            }
        }

        var reach = ctx.ApproachRange();
        if (distance > reach * 0.8)
        {
            Chase(ctx, target);
        }
        else
        {
            ctx.Stop();
        }
    }

    private static bool Use(AiContext ctx, AbilityDefinition a)
    {
        if (!ctx.Use(a, ctx.Target))
        {
            return false;
        }

        if (a.Telegraph is { } t)
        {
            ctx.Stop();
            ctx.State.PhaseUntilTick = ctx.Tick + Math.Max(1, t.DelayTicks) + 1;
        }

        return true;
    }
}
