using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace MmoRpgClient.Match;

public enum MatchOutcome
{
    InProgress,
    Victory,
    Defeat,
    TimeUp,
}

/// <summary>
/// Acompanha a partida pelo que a autoridade publica (eventos e snapshot): resultado e números do resumo.
/// Mesmas regras do simulador: vitória = nenhum monstro vivo; derrota = nenhum jogador vivo ou tempo esgotado
/// (<c>defeat.timeLimit</c> do encontro).
/// </summary>
public sealed class MatchTracker(EntityId? player, int? timeLimitTicks)
{
    public MatchOutcome Outcome { get; private set; } = MatchOutcome.InProgress;

    public long Ticks { get; private set; }

    public double Seconds => Ticks / (double)World.TickRate;

    /// <summary>Números do jogador local.</summary>
    public double DamageDealt { get; private set; }

    public double DamageTaken { get; private set; }

    public double Absorbed { get; private set; }

    public double Healing { get; private set; }

    /// <summary>Números do grupo (todos os jogadores).</summary>
    public double GroupDamageDealt { get; private set; }

    public int PlayerDeaths { get; private set; }

    public int EnemiesKilled { get; private set; }

    public void Consume(IReadOnlyList<GameEvent> events, WorldSnapshot? latest)
    {
        if (Outcome != MatchOutcome.InProgress || latest is null)
        {
            return;
        }

        var players = latest.Entities
            .Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Player && e.TryGet<Health>() is not null)
            .Select(e => e.Id)
            .ToHashSet();

        foreach (var e in events)
        {
            switch (e)
            {
                case DamageDealt d:
                    if (d.Source == player)
                    {
                        DamageDealt += d.Amount;
                    }

                    if (players.Contains(d.Source))
                    {
                        GroupDamageDealt += d.Amount;
                    }

                    if (d.Target == player)
                    {
                        DamageTaken += d.Amount;
                        Absorbed += d.Absorbed;
                    }

                    break;
                case HealApplied h when h.Source == player:
                    Healing += h.Effective;
                    break;
                case EntityDied died:
                    if (players.Contains(died.Entity))
                    {
                        PlayerDeaths++;
                    }
                    else
                    {
                        EnemiesKilled++;
                    }

                    break;
            }
        }

        Ticks = latest.Tick;
        var playersAlive = latest.Entities.Any(e => players.Contains(e.Id) && e.TryGet<Health>() is { IsAlive: true });
        var enemiesAlive = latest.Entities.Any(e =>
            e.TryGet<FactionTag>()?.Faction == Faction.Monster && e.TryGet<Health>() is { IsAlive: true });

        Outcome = !playersAlive ? MatchOutcome.Defeat
            : !enemiesAlive ? MatchOutcome.Victory
            : timeLimitTicks is { } limit && Ticks >= limit ? MatchOutcome.TimeUp
            : MatchOutcome.InProgress;
    }
}
