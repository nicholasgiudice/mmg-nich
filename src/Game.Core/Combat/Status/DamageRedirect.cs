using System.Numerics;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Status;

/// <summary>
/// Dano desviado (T-406): <paramref name="Protector"/> recebeu <paramref name="Amount"/> (dano final, depois da defesa dele e
/// da redução do status) do dano que <paramref name="Attacker"/> causaria em <paramref name="Target"/>, por causa do status
/// <paramref name="StatusId"/>. O dano em si sai num <see cref="DamageDealt"/> no protetor, publicado antes deste evento.
/// </summary>
public sealed record DamageRedirected(
    EntityId Attacker, EntityId Target, EntityId Protector, string SourceId, string StatusId, float Amount) : GameEvent;

/// <summary>Uma parte do dano a desviar: o status, o protetor e a fração (já limitada para o total não passar de 1).</summary>
public readonly record struct RedirectShare(StatusInstance Status, RedirectDefinition Redirect, Entity Protector, double Fraction);

/// <summary>Escolha dos desvios de dano de um alvo (T-406). Quem aplica é o <see cref="Effects.DamageResolver"/>.</summary>
public static class DamageRedirect
{
    /// <summary>
    /// Desvios que valem agora para um dano em <paramref name="target"/>, na ordem de aplicação dos status. Pula protetor
    /// morto, sem vida, longe demais (<c>maxDistance</c>) ou que é o próprio alvo. As frações somam no máximo 1.
    /// </summary>
    public static IReadOnlyList<RedirectShare> Shares(World world, Entity target)
    {
        if (target.TryGet<StatusEffects>() is not { Active.Count: > 0 })
        {
            return [];
        }

        List<RedirectShare>? shares = null;
        var total = 0.0;
        foreach (var s in StatusManager.Active(world, target))
        {
            if (StatusManager.Def(world, s).Redirect is not { } r || total >= 1)
            {
                continue;
            }

            if (s.Protector == target.Id || world.Find(s.Protector) is not { IsAlive: true, IsRemoved: false } protector
                || protector.TryGet<Health>() is not { IsAlive: true }
                || !WithinReach(target, protector, r.MaxDistance))
            {
                continue;
            }

            var fraction = Math.Min(r.Fraction, 1 - total);
            if (fraction <= 0)
            {
                continue;
            }

            total += fraction;
            (shares ??= []).Add(new RedirectShare(s, r, protector, fraction));
        }

        return shares ?? (IReadOnlyList<RedirectShare>)[];
    }

    private static bool WithinReach(Entity target, Entity protector, double maxDistance)
    {
        if (target.TryGet<Transform>() is not { } a || protector.TryGet<Transform>() is not { } b)
        {
            return false;
        }

        return Vector2.Distance(a.Position, b.Position) <= maxDistance + 1e-6;
    }
}
