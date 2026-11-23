using Game.Core.Definitions;

namespace Game.Data;

/// <summary>
/// Validação dos campos do Caçador (T-432): <c>damage.movingBonus</c>, <c>apply_status.stacks</c>, o efeito
/// <c>copy_status</c> e, no status, <c>hitEffects</c>, <c>byRank</c> e <c>jumpOnExpire</c>.
/// </summary>
internal static partial class GameDataValidator
{
    private static void ValidateHunterDamage(DamageEffect d, Ctx c) => c.NonNegative("movingBonus", d.MovingBonus);

    private static void ValidateHunterApplyStatus(ApplyStatusEffect a, Ctx c)
    {
        if (a.Stacks < 1) { c.Error("stacks", "Deve ser pelo menos 1."); }
    }

    private static void ValidateCopyStatus(CopyStatusEffect e, Ctx c)
    {
        ValidateTagList(e.Tags, c, "tags");
        c.Positive("radius", e.Radius);
        if (e.Count < 0) { c.Error("count", "Deve ser maior ou igual a 0 (0 = todos no raio)."); }
    }

    private static void ValidateHunterStatus(StatusDefinition x, Ctx ctx, Refs refs)
    {
        if (x.HitEffects.Count > 0)
        {
            ValidateEffects(x.HitEffects, ctx, "hitEffects", refs);
        }

        if (x.ByRank is { } br)
        {
            ctx.Exists("byRank.ability", br.Ability, refs.Abilities, "habilidade");
            if (br.Statuses.Count == 0) { ctx.Error("byRank.statuses", "Informe pelo menos um status (o do NH1)."); }
            for (var i = 0; i < br.Statuses.Count; i++)
            {
                ctx.Exists($"byRank.statuses[{i}]", br.Statuses[i], refs.Statuses, "status");
                if (br.Statuses[i] == x.Id) { ctx.Error($"byRank.statuses[{i}]", "Não pode apontar para o próprio status."); }
            }
        }

        if (x.JumpOnExpire is { } j)
        {
            ValidateTagList(j.Tags, ctx, "jumpOnExpire.tags");
            ctx.Positive("jumpOnExpire.radius", j.Radius);
        }
    }

    /// <summary>T-432: os status de um <c>byRank</c> são os de verdade (não podem ter <c>byRank</c> de novo).</summary>
    private static void ValidateStatusByRank(RawContent c, List<LoadError> errors)
    {
        var withByRank = c.Statuses.Where(e => e.Value.ByRank is not null).Select(e => e.Value.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var e in c.Statuses)
        {
            if (e.Value.ByRank is not { } br)
            {
                continue;
            }

            var ctx = new Ctx(e.File, e.Path, errors);
            for (var i = 0; i < br.Statuses.Count; i++)
            {
                if (br.Statuses[i] != e.Value.Id && withByRank.Contains(br.Statuses[i]))
                {
                    ctx.Error($"byRank.statuses[{i}]", $"'{br.Statuses[i]}' também tem byRank; aponte para os status de cada NH.");
                }
            }
        }
    }

    private static void ValidateTagList(IReadOnlyList<string> tags, Ctx c, string field)
    {
        if (tags.Count == 0) { c.Error(field, "Informe pelo menos uma etiqueta."); }
        if (tags.Any(string.IsNullOrWhiteSpace)) { c.Error(field, "Etiqueta vazia."); }
    }
}
