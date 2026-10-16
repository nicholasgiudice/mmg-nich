using Game.Core.Combat.Areas;
using Game.Core.Combat.Phases;
using Game.Core.Combat.Telegraphs;
using Game.Core.CombatLog;

namespace Game.Core.Combat;

/// <summary>Módulo da T-012: efeitos <c>spawn_area</c>/<c>summon</c>, gate de fases de boss e formatadores de log dos novos eventos.</summary>
public static class EncounterModule
{
    public static void Register(CombatServices s)
    {
        AreaService.Register(s);
        SummonModule.Register(s);
        s.Gates.Add(new PhaseGate());
    }

    /// <summary>Formatadores de log: TelegraphStarted/Resolved, AreaSpawned/Triggered/Expired, PhaseChanged, MonsterSummoned.</summary>
    public static CombatLogFormatters RegisterLog(CombatLogFormatters f)
    {
        f.Register<TelegraphStarted>((c, e) => c.Entry(
            e,
            LogCategory.Habilidade,
            $"{c.Name(e.Source)} prepara {c.SourceLabel(e.AbilityId)} ({ShapeName(e.Shape)}), resolve em " +
            $"{CombatLogContext.Seconds(e.ResolveAtTick - e.Tick)} s",
            e.Source));

        f.Register<TelegraphResolved>((c, e) =>
        {
            var ability = c.SourceLabel(e.AbilityId);
            if (e.Cancelled)
            {
                return c.Entry(e, LogCategory.Habilidade, $"{ability} de {c.Name(e.Source)} foi cancelada", e.Source);
            }

            var who = e.Targets.Count == 0 ? "ninguém foi atingido" : "atingiu " + string.Join(", ", e.Targets.Select(c.Name));
            return c.Entry(e, LogCategory.Habilidade, $"{ability} de {c.Name(e.Source)} {who}", [e.Source, .. e.Targets]);
        });

        f.Register<AreaSpawned>((c, e) => c.Entry(
            e,
            LogCategory.Habilidade,
            $"{c.SourceLabel(e.AreaId)} surgiu" + (e.Owner.IsNone ? "" : $" ({c.Name(e.Owner)})"),
            e.Owner));

        f.Register<AreaTriggered>((c, e) => c.Entry(
            e,
            LogCategory.Habilidade,
            $"{c.SourceLabel(e.AreaId)} atingiu " + string.Join(", ", e.Targets.Select(c.Name)),
            [e.Owner, .. e.Targets]));

        f.Register<AreaExpired>((c, e) => c.Entry(
            e,
            LogCategory.Habilidade,
            e.Reason == AreaExpireReason.Triggered
                ? $"{c.SourceLabel(e.AreaId)} foi disparada"
                : $"{c.SourceLabel(e.AreaId)} desapareceu"));

        f.Register<PhaseChanged>((c, e) => c.Entry(
            e,
            LogCategory.Fase,
            $"{c.Name(e.Entity)} entrou na fase {e.Index + 1} ({e.Phase}, {CombatLogContext.Number(Math.Round(e.HealthPercent, 1))}% de vida)",
            e.Entity));

        f.Register<MonsterSummoned>((c, e) => c.Entry(
            e,
            LogCategory.Habilidade,
            $"{c.Name(e.Owner)} invocou {c.Name(e.Summon)}",
            e.Owner,
            e.Summon));

        return f;
    }

    private static string ShapeName(Definitions.TelegraphShape shape) => shape switch
    {
        Definitions.TelegraphShape.Circle => "círculo",
        Definitions.TelegraphShape.Cone => "cone",
        _ => "linha",
    };
}
