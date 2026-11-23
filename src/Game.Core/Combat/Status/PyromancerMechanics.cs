using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Status;

/// <summary>
/// Mecânicas do Piromante (T-441): efeitos só em quem tem um estado, e tirar o estado (Combustão, <c>if_status</c>); status
/// do mesmo grupo que não convivem (Queimadura por NH, <c>exclusive</c>); e disparo em linha que explode no primeiro que acerta
/// (Bola de Fogo, <c>line.explodeRadius</c>). A área depois do impacto (Meteoro NH3) já funcionava: <c>spawn_area</c> num
/// telegráfo nasce no ponto do telegráfo. Registrado em <see cref="CombatModules"/>.
/// </summary>
public static class PyromancerMechanics
{
    public static void Register(CombatServices s) => s.Effects.Register<IfStatusEffect>(OnIfStatus, EffectScope.PerCast);

    /// <summary>
    /// Combustão: quem tem o status é escolhido antes de qualquer efeito (uma cópia feita para outro alvo no mesmo uso não o
    /// faz levar dano). Depois, em cada um, na ordem dos alvos: os efeitos e, com <c>consume</c>, a retirada do status.
    /// </summary>
    private static void OnIfStatus(EffectContext c, IfStatusEffect e)
    {
        var marked = c.Targets.Where(t => t.IsAlive && HasAny(c.World, t, e.Tags)).ToList();
        foreach (var t in marked)
        {
            if (!t.IsAlive)
            {
                continue;
            }

            EffectApplier.Apply(c.World, e.Effects, c.Source, [t], c.SourceId, c.Ability, c.AimPoint, c.AimDirection, c.Power, c.HitIndex);
            if (e.Consume && t.IsAlive)
            {
                StatusManager.RemoveByTags(c.World, t, e.Tags, 0, c.SourceEntityId);
            }
        }
    }

    /// <summary>Tem um status ativo (não permanente) com qualquer uma das etiquetas.</summary>
    public static bool HasAny(World world, Entity entity, IReadOnlyList<string> tags) =>
        StatusManager.Active(world, entity).Any(s => !StatusManager.IsPermanent(s) && HunterMechanics.HasTag(world, s, tags));
}

/// <summary>
/// T-441 (Queimadura por NH): status com <c>exclusive</c> do mesmo grupo não convivem. Chamado por
/// <see cref="StatusManager.Apply"/> antes de criar ou renovar a instância.
/// </summary>
public static class ExclusiveStatus
{
    /// <summary>
    /// Resolve o grupo de <paramref name="def"/> em <paramref name="target"/>. Se já há um status do grupo de nível mais alto,
    /// ele fica (mesma fonte, mesmos números), com o fim no maior entre o dele e agora + <paramref name="duration"/>, e é
    /// devolvido: a aplicação para aí. Se o que há é de nível igual ou mais baixo, ele sai e <paramref name="duration"/> passa a
    /// ser a maior entre a dela e o que faltava dele. O mesmo status só não encurta. Retorna null para seguir a aplicação.
    /// </summary>
    internal static StatusInstance? Resolve(World world, Entity target, StatusEffects fx, StatusDefinition def, ref int duration, EntityId by)
    {
        if (def.Exclusive is not { } mine)
        {
            return null;
        }

        foreach (var other in fx.Active.Where(s => StatusManager.IsActive(world, s) && !StatusManager.IsPermanent(s)).ToList())
        {
            if (StatusManager.Def(world, other).Exclusive is not { } theirs || theirs.Group != mine.Group)
            {
                continue;
            }

            var remaining = (int)Math.Min(other.ExpiresAtTick - world.Tick, int.MaxValue);
            if (other.StatusId != def.Id && theirs.Tier > mine.Tier)
            {
                other.ExpiresAtTick = Math.Max(other.ExpiresAtTick, world.Tick + duration);
                world.Events.Publish(new StatusRefreshed(by, target.Id, other.StatusId, other.Id, other.Stacks, other.ExpiresAtTick));
                return other;
            }

            duration = Math.Max(duration, remaining);
            if (other.StatusId != def.Id)
            {
                StatusManager.Remove(world, target, other.StatusId, by);
            }
        }

        return null;
    }
}

/// <summary>T-441 (Bola de Fogo): o disparo em linha para no primeiro alvo e explode em volta dele.</summary>
public static class LineExplosion
{
    /// <summary>
    /// Entre os alvos da linha, o primeiro é o mais à frente de <paramref name="origin"/> na direção <paramref name="dir"/>
    /// (empate: menor id). Os alvos passam a ser ele e todos que <paramref name="filter"/> aceita a até
    /// <paramref name="radius"/> tiles dele (o corpo conta), com linha de visão a partir dele, os mais perto primeiro. Linha
    /// vazia: nenhum alvo (não explode).
    /// </summary>
    public static IReadOnlyList<Entity> Resolve(
        World world, Vector2 origin, Vector2 dir, IReadOnlyList<Entity> line, double radius, Func<Entity, bool> filter)
    {
        var first = line
            .OrderBy(e => Vector2.Dot(e.Get<Transform>().Position - origin, dir))
            .ThenBy(e => e.Id.Value)
            .FirstOrDefault();
        if (first is null)
        {
            return [];
        }

        var center = first.Get<Transform>().Position;
        var around = world.Spatial.InRadius(center, (float)radius, x => x != first && filter(x) && HunterMechanics.HasLos(world, center, x))
            .OrderBy(x => Vector2.Distance(center, x.Get<Transform>().Position))
            .ThenBy(x => x.Id.Value);
        return [first, .. around];
    }
}
