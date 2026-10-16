using Game.Core.Combat.Ai;
using Game.Core.Definitions;

namespace Game.Core.Combat.Threat;

/// <summary>Registro do efeito <c>threat</c> (somar ameaça e/ou provocar). Chamado pelo slot T-011 de <see cref="CombatModules"/>.</summary>
public static class ThreatModule
{
    public static void Register(CombatServices s)
    {
        s.Effects.Register<ThreatEffect>((ctx, e) =>
        {
            if (ctx.Source is not { } source || ctx.Target is not { } target || AiState.Ensure(ctx.World, target) is null)
            {
                return; // só monstros com IA têm tabela de ameaça
            }

            ThreatService.Add(ctx.World, target, source, e.Amount, AggroReason.Threat);
            if (e.TauntDurationTicks is { } ticks)
            {
                ThreatService.Taunt(ctx.World, target, source, ticks);
            }
        });
    }
}
