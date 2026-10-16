using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.guardian</c> (Guarda Esquelético): ciclo Perseguir -> Guarda -> Golpe pesado -> Recuperar. A guarda é a habilidade
/// com tag <c>ai.guard</c> (auto-alvo; em dados, um <c>apply_status</c> com modificador frontal, T-009) e o golpe pesado a
/// com tag <c>ai.heavy</c> (conjuração longa = telegráfo visível). Durante a guarda o monstro fica parado e só reorienta
/// para o alvo a cada <c>faceIntervalSeconds</c> (padrão 1), o que abre janela para atacar pelo flanco.
/// Fases em <see cref="AiState.Phase"/>: 0 perseguir, 1 guarda, 2 golpe, 3 recuperar.
/// Parâmetros: <c>guardSeconds</c> (3), <c>recoverSeconds</c> (1), <c>faceIntervalSeconds</c> (1).
/// </summary>
public sealed class GuardianProfile : MeleeChaserProfile
{
    public const string GuardTag = "ai.guard";
    public const string HeavyTag = "ai.heavy";

    public override void Think(AiContext ctx)
    {
        var heavy = ctx.FindByTag(HeavyTag);
        if (heavy is null)
        {
            base.Think(ctx);
            return;
        }

        var target = ctx.Target!;
        var state = ctx.State;
        var inReach = ctx.InReach(heavy, target);

        switch (state.Phase)
        {
            case 0:
                if (inReach)
                {
                    state.Phase = 1;
                    state.PhaseUntilTick = ctx.Tick + Math.Max(1, ctx.ParamTicks("guardSeconds", 3));
                    state.NextFaceTick = ctx.Tick + Math.Max(1, ctx.ParamTicks("faceIntervalSeconds", 1));
                    if (ctx.FindByTag(GuardTag) is { } guard)
                    {
                        ctx.Use(guard, target);
                    }

                    ctx.Stop();
                }
                else
                {
                    Chase(ctx, target);
                }

                break;

            case 1:
                if (!inReach)
                {
                    state.Phase = 0;
                    Chase(ctx, target);
                }
                else if (ctx.Tick >= state.PhaseUntilTick)
                {
                    if (ctx.Use(heavy, target))
                    {
                        state.Phase = 2;
                        state.PhaseEmitTick = ctx.Tick;
                    }

                    ctx.Stop(); // sem sucesso (recarga): segue em guarda
                }
                else if (ctx.Tick >= state.NextFaceTick)
                {
                    ctx.FaceToward(target.Get<Transform>().Position);
                    state.NextFaceTick = ctx.Tick + Math.Max(1, ctx.ParamTicks("faceIntervalSeconds", 1));
                }
                else
                {
                    ctx.Stop();
                }

                break;

            case 2:
                ctx.Stop();
                if (ctx.Tick > state.PhaseEmitTick && !ctx.Abilities.IsCasting)
                {
                    state.Phase = 3;
                    state.PhaseUntilTick = ctx.Tick + Math.Max(1, ctx.ParamTicks("recoverSeconds", 1));
                }

                break;

            default:
                ctx.Stop();
                if (ctx.Tick >= state.PhaseUntilTick)
                {
                    state.Phase = 0;
                }

                break;
        }
    }
}
