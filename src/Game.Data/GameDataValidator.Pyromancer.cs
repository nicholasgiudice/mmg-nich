using Game.Core.Definitions;

namespace Game.Data;

/// <summary>
/// Validação dos campos do Piromante (T-441): o efeito <c>if_status</c>, <c>exclusive</c> no status e
/// <c>explodeRadius</c> na mira em linha.
/// </summary>
internal static partial class GameDataValidator
{
    private static void ValidateIfStatus(IfStatusEffect e, Ctx c, Refs refs)
    {
        ValidateTagList(e.Tags, c, "tags");
        if (e.Effects.Count == 0) { c.Error("effects", "Precisa de pelo menos um efeito."); }
        ValidateEffects(e.Effects, c, "effects", refs);
    }

    private static void ValidatePyromancerStatus(StatusDefinition x, Ctx ctx)
    {
        if (x.Exclusive is { } ex)
        {
            if (string.IsNullOrWhiteSpace(ex.Group)) { ctx.Error("exclusive.group", "Grupo vazio."); }
            if (ex.Tier < 1) { ctx.Error("exclusive.tier", "Deve ser pelo menos 1."); }
        }
    }

    private static void ValidatePyromancerTargeting(LineTargeting l, Ctx ctx)
    {
        if (l.ExplodeRadius is { } blast)
        {
            ctx.Positive("explodeRadius", blast);
        }
    }
}
