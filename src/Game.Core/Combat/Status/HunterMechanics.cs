using System.Numerics;
using Game.Core.Combat.Effects;
using Game.Core.Definitions;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Core.Combat.Status;

/// <summary>
/// Mecânicas do Caçador (T-432): efeito em todo acerto (Flecha Envenenada), status pelo NH de outra habilidade (Veneno do
/// Caçador), dano periódico maior se o alvo andar (Ferida Aberta), cópia de status para outros perto (Flecha Dispersa) e
/// status que pula ao acabar (Praga). Registrado em <see cref="CombatModules"/>.
/// </summary>
public static class HunterMechanics
{
    public static void Register(CombatServices s)
    {
        s.Effects.Register<CopyStatusEffect>(StatusCopy.OnCopyStatus);
        s.DamageReactions.Add(new HitEffectsReaction());
    }

    /// <summary>
    /// Acúmulos a mais numa instância já aplicada (T-432, <c>apply_status</c> com <c>stacks</c> e cópias), até o máximo do
    /// status e só em <c>stacking: stack</c>. Publica um <see cref="StatusStacked"/> com o total, se mudou.
    /// </summary>
    public static void AddStacks(World world, EntityId source, Entity target, StatusInstance inst, int extra)
    {
        var def = StatusManager.Def(world, inst);
        if (extra <= 0 || def.Stacking != StackingMode.Stack)
        {
            return;
        }

        var stacks = Math.Min(def.MaxStacks, inst.Stacks + extra);
        if (stacks == inst.Stacks)
        {
            return;
        }

        inst.Stacks = stacks;
        world.Events.Publish(new StatusStacked(source, target.Id, inst.StatusId, inst.Id, stacks, inst.ExpiresAtTick));
    }

    /// <summary><c>apply_status</c> com <c>stacks</c> &gt; 1: os acúmulos a mais depois da aplicação normal.</summary>
    internal static void AfterApplyStatus(EffectContext c, ApplyStatusEffect e, StatusInstance? inst)
    {
        if (inst is not null && e.Stacks > 1)
        {
            AddStacks(c.World, c.SourceEntityId, c.Target!, inst, e.Stacks - 1);
        }
    }

    internal static bool HasTag(World world, StatusInstance s, IReadOnlyList<string> tags) =>
        StatusManager.Def(world, s).Tags.Any(tags.Contains);

    internal static bool HasLos(World world, Vector2 from, Entity to) =>
        world.Map?.HasLineOfSight(from, to.Get<Transform>().Position) ?? true;
}

/// <summary>
/// T-432 (Flecha Envenenada): depois de um dano de habilidade (ataque básico incluído) de quem tem um status com
/// <c>hitEffects</c>, os efeitos rodam no alvo com quem bateu como fonte e o status como origem. Não conta: dano indireto
/// (refletido ou desviado), dano em si mesmo, dano de status (periódico, reflexos) e de área (a origem não é uma habilidade).
/// Como a origem dos efeitos é o status, um dano dentro de <c>hitEffects</c> não dispara de novo.
/// </summary>
public sealed class HitEffectsReaction : IDamageReaction
{
    public void AfterDamage(World world, DamageReport damage)
    {
        if (damage.Indirect || damage.Source is not { IsAlive: true } attacker || attacker == damage.Target
            || !damage.Target.IsAlive || !world.Content.Abilities.ContainsKey(damage.SourceId))
        {
            return;
        }

        foreach (var s in StatusManager.Active(world, attacker).ToList())
        {
            var def = StatusManager.Def(world, s);
            if (def.HitEffects.Count > 0 && damage.Target.IsAlive)
            {
                EffectApplier.Apply(world, def.HitEffects, attacker, [damage.Target], s.StatusId);
            }
        }
    }
}

/// <summary>T-432 (Veneno do Caçador): escolhe o status pelo NH de outra habilidade de quem aplica (<c>byRank</c>).</summary>
public static class RankedStatus
{
    /// <summary>
    /// O status que vale para esta fonte: <c>statuses[NH − 1]</c> pelo NH de <c>byRank.ability</c> de quem aplica. Sem pontos
    /// (NH 0), sem fonte ou sem NH (monstros): o NH1. Acima da lista: o último. Status sem <c>byRank</c>: ele mesmo.
    /// </summary>
    public static string Resolve(Entity? source, string statusId, StatusDefinition def)
    {
        if (def.ByRank is not { Statuses.Count: > 0 } byRank)
        {
            return statusId;
        }

        var rank = source?.TryGet<AbilityState>()?.RankOf(byRank.Ability) ?? 1; // sem NH guardado = 1; NH 0 vira 1 abaixo
        return byRank.Statuses[Math.Clamp(rank, 1, byRank.Statuses.Count) - 1];
    }
}

/// <summary>
/// T-432 (Flecha Dispersa): cópia de status para outras entidades, com os mesmos acúmulos e o tempo que falta. Reaproveitável
/// (T-441, Combustão NH5): o efeito <c>copy_status</c> ou, direto do código, <see cref="Spread"/> e <see cref="CopyTo"/>.
/// </summary>
public static class StatusCopy
{
    internal static void OnCopyStatus(EffectContext c, CopyStatusEffect e)
    {
        if (c.Source is { } source)
        {
            Spread(c.World, source, c.Target!, e.Tags, e.Radius, e.Count, e.Affects);
        }
    }

    /// <summary>
    /// Copia os status de <paramref name="from"/> com qualquer uma das <paramref name="tags"/> (menos os permanentes) para até
    /// <paramref name="count"/> entidades (0 = todas) a até <paramref name="radius"/> tiles dele, da facção
    /// <paramref name="affects"/> em relação a <paramref name="source"/>, as mais perto primeiro (o corpo conta; empate: menor
    /// id), com linha de visão e que podem ser alvo. <paramref name="source"/> vira a fonte das cópias. Retorna quem recebeu.
    /// </summary>
    public static IReadOnlyList<Entity> Spread(
        World world, Entity source, Entity from, IReadOnlyList<string> tags, double radius, int count, FactionFilter affects = FactionFilter.Enemy)
    {
        var statuses = StatusManager.Active(world, from)
            .Where(s => !StatusManager.IsPermanent(s) && HunterMechanics.HasTag(world, s, tags))
            .ToList();
        if (statuses.Count == 0 || from.TryGet<Transform>() is not { } ft)
        {
            return [];
        }

        var center = ft.Position;
        var ordered = world.Spatial.InRadius(
                center,
                (float)radius,
                x => x != from && x.Has<Health>() && TargetResolver.MatchesFaction(source, x, affects)
                    && TargetResolver.CanBeHitBy(world, source, x) && HunterMechanics.HasLos(world, center, x))
            .OrderBy(x => Vector2.Distance(center, x.Get<Transform>().Position))
            .ThenBy(x => x.Id.Value);
        var chosen = (count > 0 ? ordered.Take(count) : ordered).ToList();
        foreach (var to in chosen)
        {
            foreach (var s in statuses)
            {
                CopyTo(world, source, to, s);
            }
        }

        return chosen;
    }

    /// <summary>
    /// Copia uma instância de status para <paramref name="to"/>, com os mesmos acúmulos e o tempo que falta, com
    /// <paramref name="source"/> como fonte. Se <paramref name="to"/> já tem o status, fica com o maior número de acúmulos e o
    /// maior tempo (e a fonte nova). Status permanente, acabado ou alvo morto: nada. Retorna a instância em
    /// <paramref name="to"/> ou null.
    /// </summary>
    public static StatusInstance? CopyTo(World world, Entity? source, Entity to, StatusInstance from)
    {
        var remaining = from.ExpiresAtTick - world.Tick;
        if (!to.IsAlive || remaining <= 0 || StatusManager.IsPermanent(from))
        {
            return null;
        }

        var def = world.Content.Statuses[from.StatusId];
        var sourceId = source?.Id ?? EntityId.None;
        if (StatusManager.Get(world, to, from.StatusId) is { } existing)
        {
            if (StatusManager.IsPermanent(existing))
            {
                return null;
            }

            var stacks = Math.Min(def.MaxStacks, Math.Max(existing.Stacks, from.Stacks));
            existing.Source = sourceId;
            existing.ExpiresAtTick = Math.Max(existing.ExpiresAtTick, from.ExpiresAtTick);
            if (stacks > existing.Stacks)
            {
                existing.Stacks = stacks;
                world.Events.Publish(new StatusStacked(sourceId, to.Id, existing.StatusId, existing.Id, stacks, existing.ExpiresAtTick));
            }
            else
            {
                world.Events.Publish(new StatusRefreshed(sourceId, to.Id, existing.StatusId, existing.Id, existing.Stacks, existing.ExpiresAtTick));
            }

            return existing;
        }

        var inst = StatusManager.Apply(world, source, to, from.StatusId, (int)Math.Min(remaining, int.MaxValue));
        if (inst is not null)
        {
            HunterMechanics.AddStacks(world, sourceId, to, inst, from.Stacks - inst.Stacks);
        }

        return inst;
    }
}

/// <summary>
/// T-432 (Praga): quando um status acaba pelo tempo numa entidade viva, se quem o aplicou tem um status ativo com
/// <c>jumpOnExpire</c> que casa com as etiquetas dele, o status pula (mesmos acúmulos, duração padrão, mesma fonte) para o
/// inimigo mais perto que ainda não tem um status com essas etiquetas. Chamado pelo <see cref="StatusSystem"/>.
/// </summary>
public static class StatusJump
{
    internal static void OnExpired(World world, Entity entity, StatusInstance expired, StatusDefinition def)
    {
        if (def.Tags.Count == 0 || !entity.IsAlive || world.Find(expired.Source) is not { } holder || holder == entity
            || entity.TryGet<Transform>() is not { } t)
        {
            return;
        }

        foreach (var rule in StatusManager.Active(world, holder))
        {
            if (StatusManager.Def(world, rule).JumpOnExpire is not { } jump)
            {
                continue;
            }

            var tags = jump.Tags.Where(def.Tags.Contains).ToList();
            if (tags.Count == 0)
            {
                continue;
            }

            var center = t.Position;
            var next = world.Spatial.InRadius(
                    center,
                    (float)jump.Radius,
                    x => x != entity && x.Has<Health>() && TargetResolver.MatchesFaction(holder, x, FactionFilter.Enemy)
                        && TargetResolver.CanBeHitBy(world, holder, x) && HunterMechanics.HasLos(world, center, x)
                        && !StatusManager.Active(world, x).Any(o => HunterMechanics.HasTag(world, o, tags)))
                .OrderBy(x => Vector2.Distance(center, x.Get<Transform>().Position))
                .ThenBy(x => x.Id.Value)
                .FirstOrDefault();
            if (next is not null && StatusManager.Apply(world, holder, next, expired.StatusId) is { } inst)
            {
                HunterMechanics.AddStacks(world, holder.Id, next, inst, expired.Stacks - inst.Stacks);
            }

            return; // um pulo por status que acaba, mesmo com várias Pragas
        }
    }
}

/// <summary>
/// T-432 (Ferida Aberta): <c>damage.movingBonus</c> nos pulsos de status em que o alvo andou. A posição (do Core) do alvo é
/// guardada quando o status pega e a cada pulso; o pulso conta como "andou" se ela mudou mais de <see cref="MinDistance"/>.
/// </summary>
public static class MovingDamage
{
    /// <summary>Quanto o alvo precisa ter saído do lugar (tiles) desde o pulso anterior para contar como andou.</summary>
    public const float MinDistance = 0.1f;

    /// <summary>Multiplicador do dano: 1 + <c>movingBonus</c> se este é o pulso de um status em que o alvo andou; senão 1.</summary>
    public static double Factor(EffectContext c, DamageEffect e) =>
        e.MovingBonus > 0 && c.Target?.TryGet<PulseMotion>() is { } m && m.MovedNow.Contains(c.SourceId) ? 1 + e.MovingBonus : 1;

    /// <summary>Status novo com dano que depende do movimento: guarda a posição do alvo agora.</summary>
    internal static void OnApplied(Entity target, StatusDefinition def)
    {
        if (Tracks(def) && target.TryGet<Transform>() is { } t)
        {
            Motion(target).LastPosition[def.Id] = t.Position;
        }
    }

    /// <summary>Antes de um pulso: marca se o alvo andou desde o pulso anterior e guarda a posição de agora.</summary>
    internal static void BeginPulse(Entity entity, StatusInstance s, PeriodicEffectDefinition pulse)
    {
        if (!pulse.Effects.Any(x => x is DamageEffect { MovingBonus: > 0 }) || entity.TryGet<Transform>() is not { } t)
        {
            return;
        }

        var m = Motion(entity);
        if (m.LastPosition.TryGetValue(s.StatusId, out var last) && Vector2.Distance(last, t.Position) > MinDistance)
        {
            m.MovedNow.Add(s.StatusId);
        }

        m.LastPosition[s.StatusId] = t.Position;
    }

    /// <summary>Depois do pulso: tira a marca (outros danos com a mesma origem não ganham o bônus).</summary>
    internal static void EndPulse(Entity entity, StatusInstance s) => entity.TryGet<PulseMotion>()?.MovedNow.Remove(s.StatusId);

    private static bool Tracks(StatusDefinition def) =>
        def.Periodic.Any(p => p.Effects.Any(x => x is DamageEffect { MovingBonus: > 0 }));

    private static PulseMotion Motion(Entity e)
    {
        if (e.TryGet<PulseMotion>() is { } m)
        {
            return m;
        }

        m = new PulseMotion();
        e.Add(m);
        return m;
    }
}

/// <summary>
/// T-432: posição do alvo no último pulso de cada status com <c>damage.movingBonus</c> (por id do status) e os pulsos em
/// andamento em que ele andou. Interno do servidor (sem <see cref="IReplicated"/>): o cliente só vê o dano.
/// </summary>
public sealed class PulseMotion : IComponent
{
    public SortedDictionary<string, Vector2> LastPosition { get; } = new(StringComparer.Ordinal);

    public SortedSet<string> MovedNow { get; } = new(StringComparer.Ordinal);

    public IComponent Clone()
    {
        var c = new PulseMotion();
        foreach (var (id, pos) in LastPosition)
        {
            c.LastPosition[id] = pos;
        }

        c.MovedNow.UnionWith(MovedNow);
        return c;
    }

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(LastPosition.Count);
        foreach (var (id, pos) in LastPosition)
        {
            hasher.Add(id);
            hasher.Add(pos.X);
            hasher.Add(pos.Y);
        }

        hasher.Add(MovedNow.Count);
        foreach (var id in MovedNow)
        {
            hasher.Add(id);
        }
    }
}
