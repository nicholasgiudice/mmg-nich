using Game.Core.Combat.Ai;

namespace Game.Core.Combat.Ai.Profiles;

/// <summary>
/// <c>ai.boss</c>: como o perseguidor corpo a corpo, mas antes de atacar tenta as habilidades de papel
/// <c>ai.summon</c> (invocar) e <c>ai.area</c> (poças/teias), na ordem da definição, usando a primeira disponível.
/// As fases de boss (limiar de vida, conjunto de habilidades, <c>noSummons</c>) são aplicadas pelo <c>PhaseGate</c> na
/// validação: habilidade fora da fase é rejeitada e a IA segue para a próxima. A IA só emite intenções.
/// </summary>
public sealed class BossProfile : MeleeChaserProfile
{
    public const string SummonTag = "ai.summon";
    public const string AreaTag = "ai.area";

    public override void Think(AiContext ctx)
    {
        var target = ctx.Target!;
        foreach (var id in ctx.Abilities.Known)
        {
            if (ctx.World.Content.Abilities.TryGetValue(id, out var a)
                && (a.Tags.Contains(SummonTag) || a.Tags.Contains(AreaTag))
                && ctx.Use(a, target))
            {
                return;
            }
        }

        base.Think(ctx);
    }
}
