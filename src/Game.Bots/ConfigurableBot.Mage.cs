using System.Numerics;
using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Bots;

/// <summary>
/// T-443: quando usar as habilidades das mecânicas da segunda classe do Mago (T-441, T-442). O tipo sai dos efeitos e da
/// mira, não do id:
/// <list type="bullet">
/// <item><b>Detonar estados</b> (efeito <c>if_status</c> em volta de si, Combustão): com <c>minAoe</c> (2, ou todos os
/// inimigos se forem menos) inimigos no raio com o estado. Assim o bot queima antes de detonar.</item>
/// <item><b>Linha que explode no primeiro</b> (<c>explodeRadius</c>, Bola de Fogo): no alvo do bot ao alcance (o primeiro da
/// linha recebe; não precisa de vários na linha).</item>
/// <item><b>Área no chão sem dano direto</b> (só <c>spawn_area</c> com dano, Chão em Chamas): como a área de dano (centro no
/// inimigo com mais vizinhos, <c>minAoe</c>).</item>
/// <item><b>Cone</b> (arco de dano com alcance ≥ 3, Rajada de Chamas): só com um inimigo ao alcance do cone.</item>
/// <item><b>Mínimo por habilidade</b> (<c>minAoeOf</c>, ex.: <c>pyromancer.meteor=3</c>): área no ponto só com esse número de
/// inimigos no raio, sem baixar quando restam menos (o Meteoro com 3 ou mais).</item>
/// <item><b>Estado que acumula dano</b> (unidade inimiga, estado com dano periódico e pilhas, Marca Arcana): no alvo do bot;
/// com as pilhas no máximo, só renova quando falta menos da metade da duração.</item>
/// <item><b>Disparos no alvo atual</b> (estado em si com <c>current_target</c>, Orbes Arcanos): com o alvo do bot ao alcance
/// dos disparos e os orbes ainda não ativos.</item>
/// <item><b>Mana por segundo</b> (dano que põe em si um estado com <c>stats.manaRegen</c>, Sifão Arcano): no alvo do bot,
/// com a Mana em até <c>manaRegenBelow</c> (0,5) do máximo.</item>
/// </list>
/// Canalizações (Raio Arcano, Feixe do Vazio) seguem o papel de dano: o bot para enquanto canaliza, ou continua se movendo
/// quando a canalização deixa andar (<see cref="ChannelAllowsMoving"/>).
/// </summary>
public sealed partial class ConfigurableBot
{
    /// <summary>Null se a habilidade não é de um dos tipos acima (segue o papel); senão, se deve ser usada agora e em quem.</summary>
    private bool? PlanMageAbility(
        BotContext ctx, AbilityInfo info, List<Entity> enemies, Entity? focus, out EntityId? target, out Vector2? aim)
    {
        target = null;
        aim = null;
        var def = info.Definition;
        var content = ctx.World.Content;
        var cls = ctx.ClassId;

        if (def.Channel is { MoveSpeedMultiplier: null } channel && !SafeToChannel(ctx, enemies, channel))
        {
            return false;
        }

        if (MinAoeOf(ctx, info.Id) is { } minOf && def.Targeting is CircleAtPointTargeting)
        {
            return enemies.Count > 0 && BestCluster(ctx, (CircleAtPointTargeting)def.Targeting, enemies, minOf, out target, out aim);
        }

        if (def.Effects.OfType<IfStatusEffect>().FirstOrDefault() is { } detonate && def.Targeting is CircleSelfTargeting self)
        {
            if (enemies.Count == 0)
            {
                return false;
            }

            var pos = ctx.Position;
            var burning = enemies.Count(e => Vector2.Distance(pos, BotContext.PositionOf(e)) <= self.Radius + BotContext.RadiusOf(e)
                && detonate.Tags.Any(t => StatusManager.HasTag(ctx.World, e, t)));
            var min = (int)Math.Min(ctx.Options.GetDouble(cls, "minAoe", 2), enemies.Count);
            return burning >= Math.Max(1, min);
        }

        if (def.Targeting is LineTargeting { ExplodeRadius: not null } line)
        {
            var first = Prioritize(focus, enemies).FirstOrDefault(e => ctx.EdgeDistanceTo(e) <= line.Length);
            target = first?.Id;
            return first is not null;
        }

        if (info.DamageTotal <= 0 && def.Targeting is CircleAtPointTargeting zone
            && def.Effects.OfType<SpawnAreaEffect>().Any(s => content.Areas.TryGetValue(s.Area, out var a) && a.Effects.OfType<DamageEffect>().Any()))
        {
            var min = (int)Math.Min(ctx.Options.GetDouble(cls, "minAoe", 2), enemies.Count);
            return enemies.Count > 0 && BestCluster(ctx, zone, enemies, Math.Max(1, min), out target, out aim);
        }

        if (info.DamageTotal > 0 && def.Targeting is MeleeArcTargeting { Range: >= 3 } cone)
        {
            var near = Prioritize(focus, enemies).FirstOrDefault(e => ctx.EdgeDistanceTo(e) <= cone.Range);
            target = near?.Id;
            return near is not null;
        }

        if (info.DamageTotal <= 0 && def.Targeting is UnitTargeting { Faction: FactionFilter.Enemy } unit
            && def.Effects.OfType<ApplyStatusEffect>().Where(e => e.Target == EffectTarget.Targets)
                .Select(e => content.Statuses.TryGetValue(e.Status, out var s) ? s : null)
                .FirstOrDefault(s => s is { Stacking: StackingMode.Stack } && s.Periodic.Any(p => p.Effects.OfType<DamageEffect>().Any())) is { } dot)
        {
            var mark = Prioritize(focus, enemies).FirstOrDefault(e => ctx.EdgeDistanceTo(e) <= unit.Range);
            if (mark is null)
            {
                return false;
            }

            target = mark.Id;
            var current = StatusManager.Get(ctx.World, mark, dot.Id);
            return current is null || current.Stacks < dot.MaxStacks
                || current.ExpiresAtTick - ctx.World.Tick < dot.DefaultDurationTicks / 2;
        }

        if (info.DamageTotal <= 0 && def.Targeting is SelfTargeting
            && def.Effects.OfType<ApplyStatusEffect>().Select(e => content.Statuses.TryGetValue(e.Status, out var s) ? s : null)
                .FirstOrDefault(s => s is not null && s.Periodic.Any(p => p.Effects.OfType<CurrentTargetEffect>().Any())) is { } orbs)
        {
            var range = orbs.Periodic.SelectMany(p => p.Effects.OfType<CurrentTargetEffect>()).Max(c => c.Range);
            var aimAt = focus ?? (enemies.Count > 0 ? enemies[0] : null);
            return aimAt is not null && ctx.EdgeDistanceTo(aimAt) <= range && !StatusManager.Has(ctx.World, ctx.Self, orbs.Id);
        }

        if (info.DamageTotal > 0 && def.Targeting is UnitTargeting
            && def.Effects.OfType<ApplyStatusEffect>().Any(e => e.Target == EffectTarget.Self
                && content.Statuses.TryGetValue(e.Status, out var s) && s.Stats?.ManaRegen is not null))
        {
            var below = ctx.Options.GetDouble(cls, "manaRegenBelow", 0.5);
            if (enemies.Count == 0 || ctx.Self.TryGet<ResourcePool>() is not { Max: > 0 } pool || pool.Current / pool.Max > below)
            {
                return false;
            }

            target = Prioritize(focus, enemies).First().Id;
            return true;
        }

        return null;
    }

    /// <summary>
    /// Canalização parada só começa se nenhum inimigo que pode andar chega antes de <c>channelSafety</c> (0,5) da
    /// canalização: distância até ele maior que velocidade × duração × fração. Enraizado ou atordoado (Prisão de Gelo) não conta.
    /// </summary>
    private static bool SafeToChannel(BotContext ctx, List<Entity> enemies, ChannelDefinition channel)
    {
        var seconds = channel.Pulses * channel.IntervalTicks / (double)World.TickRate;
        var fraction = ctx.Options.GetDouble(ctx.ClassId, "channelSafety", 0.5);
        foreach (var e in enemies)
        {
            if (StatusManager.IsRooted(ctx.World, e) || StatusManager.IsStunned(ctx.World, e))
            {
                continue;
            }

            var speed = e.TryGet<Movement>()?.Speed ?? 0;
            if (ctx.EdgeDistanceTo(e) < speed * seconds * fraction)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary><c>minAoeOf</c>: lista <c>id=n</c> com o mínimo de inimigos no raio de uma habilidade de área (sem limite pelo total).</summary>
    private static int? MinAoeOf(BotContext ctx, string abilityId)
    {
        foreach (var item in ctx.Options.GetList(ctx.ClassId, "minAoeOf"))
        {
            var parts = item.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0] == abilityId
                && int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                return n;
            }
        }

        return null;
    }

    /// <summary>Centro no inimigo ao alcance com mais vizinhos no raio, se forem pelo menos <paramref name="min"/>.</summary>
    private static bool BestCluster(BotContext ctx, CircleAtPointTargeting c, List<Entity> enemies, int min, out EntityId? target, out Vector2? aim)
    {
        target = null;
        aim = null;
        Entity? best = null;
        var bestCount = 0;
        foreach (var e in enemies)
        {
            if (ctx.EdgeDistanceTo(e) > c.Range)
            {
                continue;
            }

            var p = BotContext.PositionOf(e);
            var n = enemies.Count(o => Vector2.Distance(p, BotContext.PositionOf(o)) <= c.Radius + BotContext.RadiusOf(o));
            if (n > bestCount)
            {
                best = e;
                bestCount = n;
            }
        }

        if (best is null || bestCount < min)
        {
            return false;
        }

        target = best.Id;
        aim = BotContext.PositionOf(best);
        return true;
    }

    /// <summary>Canalização que deixa andar (Raio Arcano NH5, Feixe do Vazio): o bot segue o movimento da estratégia.</summary>
    private static bool ChannelAllowsMoving(AbilityDefinition def) => def.Channel is { MoveSpeedMultiplier: not null };

    /// <summary>Durante uma canalização que deixa andar: só o movimento (sem usar habilidades).</summary>
    private void MoveWhileChanneling(BotContext ctx, List<Intent> output)
    {
        var enemies = ctx.Enemies();
        var focus = ChooseFocus(ctx, enemies);
        var fleeing = plan.Movement == MovementMode.Kite && enemies.Count > 0 && ctx.EdgeDistanceTo(enemies[0]) < RetreatDistance(ctx);
        if (focus is null && !fleeing)
        {
            output.Add(new StopIntent(ctx.Self.Id));
            return;
        }

        Move(ctx, enemies, focus, fleeing, output);
    }
}
