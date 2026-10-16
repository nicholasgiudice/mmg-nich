using System.Globalization;
using Game.Core.Combat;
using Game.Core.Events;
using Game.Core.Simulation;

namespace Game.Core.CombatLog;

/// <summary>
/// Dados passados aos formatadores: mundo (para resolver nomes), jogador que lê o log (vira "Você")
/// e nomes de entidades já vistas (para entidades removidas depois do evento).
/// </summary>
public sealed class CombatLogContext
{
    private static readonly NumberFormatInfo Pt = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

    private readonly IReadOnlyDictionary<EntityId, string> _knownNames;

    public CombatLogContext(World world, EntityId? viewer, IReadOnlyDictionary<EntityId, string> knownNames)
    {
        World = world;
        Viewer = viewer;
        _knownNames = knownNames;
    }

    public World World { get; }

    /// <summary>Entidade do jogador que lê o log; ela aparece como "Você".</summary>
    public EntityId? Viewer { get; }

    /// <summary>Nome de exibição de uma entidade ("Você" para o viewer).</summary>
    public string Name(EntityId id)
    {
        if (id.IsNone)
        {
            return "";
        }

        if (Viewer == id)
        {
            return "Você";
        }

        var unit = World.Find(id)?.TryGet<UnitInfo>();
        if (unit is not null)
        {
            return unit.Name;
        }

        return _knownNames.TryGetValue(id, out var known) ? known : $"Entidade {id}";
    }

    /// <summary>Nome de uma habilidade ou status pelo id (áreas não têm nome: cai no próprio id).</summary>
    public string SourceLabel(string sourceId)
    {
        var content = World.Content;
        if (content.Abilities.TryGetValue(sourceId, out var ability))
        {
            return ability.Name;
        }

        if (content.Statuses.TryGetValue(sourceId, out var status))
        {
            return status.Name;
        }

        return sourceId;
    }

    /// <summary>Origem de um efeito: nome da entidade que o causou, ou o nome da habilidade/status/área se não houver entidade.</summary>
    public string Origin(EntityId source, string sourceId) =>
        source.IsNone ? SourceLabel(sourceId) : Name(source);

    /// <summary>Valor numérico em pt-BR ("12", "2,5").</summary>
    public static string Number(double value) => value.ToString("0.##", Pt);

    /// <summary>Duração em segundos com uma casa decimal ("2,0"). Ticks negativos viram 0.</summary>
    public static string Seconds(long ticks) =>
        (Math.Max(0, ticks) / (double)World.TickRate).ToString("0.0", Pt);

    /// <summary>Monta a entrada a partir de um evento; ids nulos são descartados.</summary>
    public CombatLogEntry Entry(GameEvent e, LogCategory category, string text, params EntityId[] involved) =>
        new(e.Tick, category, text, involved.Where(id => !id.IsNone).ToArray(), e);
}
