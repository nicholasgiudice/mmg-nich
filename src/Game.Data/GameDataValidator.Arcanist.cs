using Game.Core.Definitions;

namespace Game.Data;

/// <summary>Validação das mecânicas do Arcanista (T-442): canalização com pulsos e disparos no alvo atual.</summary>
internal static partial class GameDataValidator
{
    private const int MaxChannelPulses = 40;

    /// <summary>
    /// <c>channel</c> na base e em cada NH; e, em cada NH efetivo com canalização, nada de <c>castTime</c>, <c>hits</c>,
    /// telegráfo nem passiva (a canalização já é o tempo da habilidade e os seus acertos).
    /// </summary>
    private static void ValidateChannel(AbilityDefinition x, Ctx ctx, Refs refs)
    {
        if (x.Channel is { } channel) { ValidateChannelFields(channel, ctx.At(".channel"), refs); }
        for (var i = 0; i < x.Ranks.Count; i++)
        {
            if (x.Ranks[i].Channel is { } rc) { ValidateChannelFields(rc, ctx.At($".ranks[{i}].channel"), refs); }
        }

        for (var rank = 1; rank <= x.HighestRank; rank++)
        {
            var eff = Game.Core.Combat.AbilityRanks.Resolve(x, rank);
            if (eff.Channel is null)
            {
                continue;
            }

            var at = rank == 1 ? "channel" : $"channel (NH{rank})";
            if (x.Passive) { ctx.Error(at, "Passiva não canaliza."); }
            if (eff.CastTimeTicks != 0) { ctx.Error(at, "Canalização não combina com castTime (a canalização já é o tempo da habilidade)."); }
            if (eff.Hits is not null) { ctx.Error(at, "Canalização não combina com hits (os pulsos são os acertos)."); }
            if (eff.Telegraph is not null) { ctx.Error(at, "Canalização não combina com telegraph."); }
        }
    }

    private static void ValidateChannelFields(ChannelDefinition c, Ctx ctx, Refs refs)
    {
        if (c.Pulses is < 1 or > MaxChannelPulses) { ctx.Error("pulses", $"Deve estar entre 1 e {MaxChannelPulses}."); }
        ctx.PositiveTicks("interval", c.IntervalTicks);
        ctx.NonNegative("pulseGrowth", c.PulseGrowth);
        if (c.MoveSpeedMultiplier is { } m && !(m > 0 && m <= 1)) { ctx.Error("moveSpeedMultiplier", "Deve estar em (0, 1]."); }
        ValidateEffects(c.OnComplete, ctx, "onComplete", refs);
    }

    private static void ValidateCurrentTarget(CurrentTargetEffect e, Ctx c, Refs refs)
    {
        c.Positive("range", e.Range);
        if (e.Count < 1) { c.Error("count", "Deve ser pelo menos 1."); }
        if (e.Effects.Count == 0) { c.Error("effects", "Precisa de pelo menos um efeito."); }
        ValidateEffects(e.Effects, c, "effects", refs);
    }
}
