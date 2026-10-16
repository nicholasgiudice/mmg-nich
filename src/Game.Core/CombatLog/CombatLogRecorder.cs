using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.CombatLog;

/// <summary>
/// Log de combate: consome <see cref="World.LastTickEvents"/> depois de cada <see cref="World.Step"/>
/// (fora da simulação, sem sistema) e produz linhas formatadas, guardadas em ordem.
/// Chamar <see cref="Record"/> de novo para o mesmo tick não duplica linhas.
/// </summary>
public sealed class CombatLogRecorder
{
    public const int DefaultCapacity = 1000;
    public const int DefaultDeathEntries = 10;

    /// <summary>5 segundos de jogo (20 ticks/s).</summary>
    public const int DefaultDeathWindowTicks = 5 * World.TickRate;

    private readonly CombatLogFormatters _formatters;
    private readonly int _capacity;
    private readonly int _deathEntries;
    private readonly int _deathWindowTicks;
    private readonly List<CombatLogEntry> _entries = new();
    private readonly Dictionary<EntityId, string> _names = new();
    private readonly HashSet<Type> _unformatted = new();
    private long _lastRecordedTick = -1;

    public CombatLogRecorder(
        CombatLogFormatters? formatters = null,
        EntityId? viewer = null,
        int capacity = DefaultCapacity,
        int deathEntries = DefaultDeathEntries,
        int deathWindowTicks = DefaultDeathWindowTicks)
    {
        _formatters = formatters ?? CombatLogFormatters.CreateDefault();
        Viewer = viewer;
        _capacity = capacity;
        _deathEntries = deathEntries;
        _deathWindowTicks = deathWindowTicks;
    }

    /// <summary>Jogador que lê o log ("Você" nas linhas).</summary>
    public EntityId? Viewer { get; }

    /// <summary>Linhas em ordem de publicação (as mais antigas saem quando passa de <c>capacity</c>).</summary>
    public IReadOnlyList<CombatLogEntry> Entries => _entries;

    /// <summary>Tipos de evento recebidos que não têm formatador (ex.: EntitySpawned). Para conferir o que falta formatar.</summary>
    public IReadOnlyCollection<Type> UnformattedTypes => _unformatted;

    /// <summary>
    /// Lê os eventos do último tick concluído do mundo e devolve os resumos de morte gerados neles.
    /// </summary>
    public IReadOnlyList<DeathSummary> Record(World world)
    {
        var deaths = new List<DeathSummary>();
        if (world.Tick == _lastRecordedTick)
        {
            return deaths;
        }

        _lastRecordedTick = world.Tick;
        RefreshNames(world);
        var ctx = new CombatLogContext(world, Viewer, _names);

        foreach (var e in world.LastTickEvents)
        {
            if (!_formatters.TryFormat(ctx, e, out var entry))
            {
                _unformatted.Add(e.GetType());
            }
            else if (entry is not null)
            {
                Append(entry);
            }

            if (e is EntityDied died)
            {
                deaths.Add(BuildDeathSummary(ctx, died.Entity, e.Tick));
            }
        }

        return deaths;
    }

    /// <summary>
    /// Resumo de morte: as últimas <c>deathEntries</c> linhas que afetaram a vítima e ocorreram nos últimos
    /// <c>deathWindowTicks</c> ticks (as duas condições valem ao mesmo tempo), com dano total por fonte.
    /// A linha de morte já está registrada quando o resumo é montado.
    /// </summary>
    private DeathSummary BuildDeathSummary(CombatLogContext ctx, EntityId victim, long deathTick)
    {
        var cutoff = deathTick - _deathWindowTicks;
        var related = _entries
            .Where(x => x.Tick >= cutoff && x.Involved.Contains(victim))
            .TakeLast(_deathEntries)
            .ToList();

        var bySource = related
            .Where(x => x.Event is DamageDealt d && d.Target == victim)
            .Select(x => (DamageDealt)x.Event)
            .GroupBy(d => (d.Source, d.SourceId))
            .Select(g => new DamageBySource(
                g.Key.Source,
                g.Key.SourceId,
                ctx.Origin(g.Key.Source, g.Key.SourceId),
                g.Sum(d => d.Amount),
                g.Sum(d => d.Absorbed),
                g.Count()))
            .OrderByDescending(s => s.Amount)
            .ToList();

        return new DeathSummary(victim, deathTick, related, bySource);
    }

    private void Append(CombatLogEntry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > _capacity)
        {
            _entries.RemoveRange(0, _entries.Count - _capacity);
        }
    }

    private void RefreshNames(World world)
    {
        foreach (var e in world.Entities)
        {
            if (e.TryGet<UnitInfo>() is { } unit)
            {
                _names[e.Id] = unit.Name;
            }
        }
    }
}
