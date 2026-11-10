using Game.Core.Combat.Objectives;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.boss</c>: como o perseguidor corpo a corpo, mas antes de atacar tenta as habilidades de papel
/// <c>ai.objective</c> (T-351: no objetivo inimigo mais ferido, como o Último Comando do Vigia nos selos), <c>ai.summon</c>
/// (invocar) e <c>ai.area</c> (poças/teias), na ordem da definição, usando a primeira disponível.
/// As fases de boss (limiar de vida, conjunto de habilidades, <c>noSummons</c>) são aplicadas pelo <c>PhaseGate</c> na
/// validação: habilidade fora da fase é rejeitada e a IA segue para a próxima. A IA só emite intenções.
/// </summary>
public sealed class BossProfile : MeleeChaserProfile
{
    public const string SummonTag = "ai.summon";
    public const string AreaTag = "ai.area";
    public const string ObjectiveTag = "ai.objective";

    public override void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        foreach (var id in ctx.Abilities.Known)
        {
            if (!ctx.World.Content.Abilities.TryGetValue(id, out var a))
            {
                continue;
            }

            if (a.Tags.Contains(ObjectiveTag))
            {
                if (WeakestObjective(ctx) is { } objective && ctx.Use(a, objective))
                {
                    return;
                }

                continue;
            }

            if ((a.Tags.Contains(SummonTag) || a.Tags.Contains(AreaTag)) && ctx.Use(a, target))
            {
                return;
            }
        }

        base.Think(ctx);
    }

    /// <summary>Objetivo vivo de outra facção com a menor fração de vida (desempate pelo id).</summary>
    public static Entity? WeakestObjective(AiContext ctx)
    {
        var faction = ctx.Self.TryGet<FactionTag>()?.Faction;
        Entity? best = null;
        var bestFraction = float.MaxValue;
        foreach (var e in ObjectiveService.Alive(ctx.World))
        {
            if (e.TryGet<FactionTag>()?.Faction == faction || e.TryGet<Health>() is not { Max: > 0 } h)
            {
                continue;
            }

            var fraction = h.Current / h.Max;
            if (fraction < bestFraction)
            {
                best = e;
                bestFraction = fraction;
            }
        }

        return best;
    }
}
