using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.CombatLog;

/// <summary>Formatadores dos eventos de <c>Game.Core.Combat</c> (T-008).</summary>
public static class CoreCombatLogFormatters
{
    public static CombatLogFormatters Register(CombatLogFormatters f)
    {
        f.Register<AbilityStarted>((c, e) =>
        {
            // Instantâneas (CastTicks = 0) são suprimidas: o efeito já aparece como linha própria.
            if (e.CastTicks == 0)
            {
                return null;
            }

            return c.Entry(e, LogCategory.Habilidade,
                $"{c.Name(e.Source)} começou a conjurar {c.SourceLabel(e.AbilityId)} ({CombatLogContext.Seconds(e.CastTicks)} s)",
                e.Source, e.Target ?? EntityId.None);
        });

        // Suprimida: o resultado de cada efeito (dano, cura, escudo, deslocamento) já gera a linha.
        f.Register<AbilityExecuted>((_, _) => null);

        f.Register<AbilityRejected>((c, e) => c.Entry(e, LogCategory.Habilidade,
            $"{c.Name(e.Source)} não pode usar {c.SourceLabel(e.AbilityId)}: {RejectText(e.Reason)}",
            e.Source));

        f.Register<CastInterrupted>((c, e) =>
        {
            var ability = c.SourceLabel(e.AbilityId);
            var text = e.Reason switch
            {
                CastInterruptReason.Interrupted when e.By is { } by && !by.IsNone =>
                    $"{c.Name(e.Entity)} teve {ability} interrompida por {c.Name(by)}",
                CastInterruptReason.Moved => $"{c.Name(e.Entity)} teve {ability} cancelada ao se mover",
                CastInterruptReason.Died => $"{c.Name(e.Entity)} teve {ability} cancelada ao morrer",
                CastInterruptReason.Disabled => $"{c.Name(e.Entity)} teve {ability} cancelada por impedimento",
                CastInterruptReason.Cancelled => $"{c.Name(e.Entity)} cancelou {ability}",
                _ => $"{c.Name(e.Entity)} teve {ability} interrompida",
            };
            return c.Entry(e, LogCategory.Habilidade, text, e.Entity, e.By ?? EntityId.None);
        });

        f.Register<DamageDealt>((c, e) =>
        {
            var absorbed = e.Absorbed > 0 ? $" ({CombatLogContext.Number(e.Absorbed)} absorvidos)" : "";
            var text =
                $"{c.Origin(e.Source, e.SourceId)} {Verb(c, e)} em {c.Name(e.Target)}: " +
                $"{CombatLogContext.Number(e.Amount)} de dano{(e.Critical ? " (crítico)" : "")}{absorbed}";
            return c.Entry(e, LogCategory.Dano, text, e.Source, e.Target);
        });

        f.Register<HealApplied>((c, e) =>
        {
            var over = e.Overheal > 0 ? $" ({CombatLogContext.Number(e.Overheal)} excedentes)" : "";
            var text =
                $"{c.Origin(e.Source, e.SourceId)} curou {c.Name(e.Target)} em " +
                $"{CombatLogContext.Number(e.Effective)} de vida{(e.Critical ? " (crítico)" : "")}{over}";
            return c.Entry(e, LogCategory.Cura, text, e.Source, e.Target);
        });

        f.Register<ShieldApplied>((c, e) => c.Entry(e, LogCategory.Status,
            $"{c.Origin(e.Source, e.SourceId)} concedeu escudo de {CombatLogContext.Number(e.Amount)}{(e.Critical ? " (crítico)" : "")} a " +
            $"{c.Name(e.Target)} por {CombatLogContext.Seconds(e.ExpiresAtTick - e.Tick)} s",
            e.Source, e.Target));

        f.Register<ShieldExpired>((c, e) =>
        {
            var text = e.Reason == ShieldEndReason.Depleted
                ? $"O escudo de {c.Name(e.Entity)} foi consumido"
                : $"O escudo de {c.Name(e.Entity)} expirou com {CombatLogContext.Number(e.Remaining)} de absorção restante";
            return c.Entry(e, LogCategory.Status, text, e.Entity);
        });

        f.Register<Displaced>((c, e) =>
        {
            var distance = CombatLogContext.Number(e.Distance);
            var text = e.Kind == DisplacementKind.Dash
                ? $"{c.Name(e.Entity)} avançou {distance} tiles com {c.SourceLabel(e.SourceId)}"
                : $"{c.Name(e.Entity)} foi empurrado {distance} tiles por {c.SourceLabel(e.SourceId)}";
            return c.Entry(e, LogCategory.Habilidade, text, e.Source, e.Entity);
        });

        f.Register<EntityDied>((c, e) => c.Entry(e, LogCategory.Morte, $"{c.Name(e.Entity)} foi derrotado", e.Entity));
        f.Register<EntityRevived>((c, e) => c.Entry(
            e, LogCategory.Morte,
            $"{c.Name(e.Entity)} foi levantado por {c.Name(e.By)} com {c.SourceLabel(e.SourceId)} ({CombatLogContext.Number(e.Health)} de vida)",
            e.By, e.Entity));

        return f;
    }

    /// <summary>"usou Mordida" quando a fonte é uma entidade e a origem é habilidade; senão, "causou dano".</summary>
    private static string Verb(CombatLogContext c, DamageDealt e) =>
        !e.Source.IsNone && c.Content.Abilities.ContainsKey(e.SourceId)
            ? $"usou {c.SourceLabel(e.SourceId)}"
            : "causou dano";

    private static string RejectText(AbilityRejectReason reason) => reason switch
    {
        AbilityRejectReason.Dead => "está morto",
        AbilityRejectReason.AlreadyCasting => "já está conjurando",
        AbilityRejectReason.CannotAct => "não pode agir",
        AbilityRejectReason.Stunned => "atordoado",
        AbilityRejectReason.Silenced => "silenciado",
        AbilityRejectReason.UnknownAbility => "habilidade desconhecida",
        AbilityRejectReason.OnCooldown => "em recarga",
        AbilityRejectReason.NotEnoughResource => "recurso insuficiente",
        AbilityRejectReason.InvalidTarget => "alvo inválido",
        AbilityRejectReason.OutOfRange => "fora de alcance",
        AbilityRejectReason.NoLineOfSight => "sem linha de visão",
        AbilityRejectReason.NoCharges => "sem cargas",
        _ => reason.ToString(),
    };
}
