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
/// T-512 (o Capataz Forjado): habilidades de papel <c>ai.ranged</c>, na mesma ordem, só saem com o alvo a pelo menos
/// <c>farDistance</c> (3) tiles de borda, como no <c>ai.brute</c>: punem quem foge do chefe. Parâmetro opcional
/// <c>meleeTargetRange</c>: ver <see cref="SelectTarget"/>.
/// </summary>
public sealed class BossProfile : MeleeChaserProfile
{
    public const string SummonTag = "ai.summon";
    public const string AreaTag = "ai.area";
    public const string ObjectiveTag = "ai.objective";
    public const string RangedTag = "ai.ranged";

    /// <summary>
    /// Com <c>meleeTargetRange</c> &gt; 0 (T-512), o chefe bate em quem está à frente dele: entre as entradas da tabela de ameaça a
    /// até essa distância (de borda a borda), escolhe a de maior ameaça, com a mesma folga de troca da tabela
    /// (<c>overtakeRatio</c>); sem ninguém tão perto, segue a tabela. Sem o parâmetro (todo outro chefe), é a tabela.
    /// A provocação continua valendo antes disto (o <see cref="AiSystem"/> não pergunta ao perfil com provocação ativa).
    /// </summary>
    public override EntityId SelectTarget(AiContext ctx, EntityId fromTable)
    {
        var range = ctx.Param("meleeTargetRange", 0);
        if (range <= 0)
        {
            return fromTable;
        }

        bool Near(int id) =>
            ctx.World.Find(new EntityId(id)) is { IsAlive: true } e && !ObjectiveService.IsObjective(e) && ctx.DistanceTo(e) <= range;

        var best = -1;
        var bestValue = float.NegativeInfinity;
        foreach (var (id, value) in ctx.Threat.Entries)
        {
            if (value > bestValue && Near(id))
            {
                best = id;
                bestValue = value;
            }
        }

        if (best < 0)
        {
            return fromTable;
        }

        var current = ctx.State.Target;
        if (!current.IsNone && current.Value != best && ctx.Threat.Entries.TryGetValue(current.Value, out var cur) && Near(current.Value)
            && bestValue <= cur * ctx.Rules.OvertakeRatio)
        {
            return current;
        }

        return new EntityId(best);
    }

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

            if (a.Tags.Contains(RangedTag))
            {
                if (ctx.DistanceTo(target) >= ctx.Param("farDistance", 3) && ctx.Use(a, target))
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
