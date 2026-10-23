using Game.Core.Events;

namespace Game.Core.CombatLog;

/// <summary>
/// Formata um evento em uma linha. Retornar null = evento suprimido (não aparece no log, mas é considerado formatado).
/// </summary>
public delegate CombatLogEntry? LogFormatter<in T>(CombatLogContext ctx, T e)
    where T : GameEvent;

/// <summary>
/// Registro de formatadores por tipo de evento (Type -> função). Novos eventos de outras tarefas
/// são registrados no módulo de quem os criou, sem editar os formatadores centrais.
/// </summary>
public sealed class CombatLogFormatters
{
    private readonly Dictionary<Type, Func<CombatLogContext, GameEvent, CombatLogEntry?>> _formatters = new();

    /// <summary>Registra o formatador de um tipo. Registrar o mesmo tipo duas vezes lança erro.</summary>
    public CombatLogFormatters Register<T>(LogFormatter<T> format)
        where T : GameEvent
    {
        if (!_formatters.TryAdd(typeof(T), (ctx, e) => format(ctx, (T)e)))
        {
            throw new InvalidOperationException($"Log formatter for {typeof(T).Name} already registered");
        }

        return this;
    }

    public bool IsRegistered<T>()
        where T : GameEvent => _formatters.ContainsKey(typeof(T));

    /// <summary>
    /// Tenta formatar. Retorna false se não há formatador para o tipo (entry fica null);
    /// true com entry null significa "formatado, mas suprimido".
    /// </summary>
    public bool TryFormat(CombatLogContext ctx, GameEvent e, out CombatLogEntry? entry)
    {
        if (_formatters.TryGetValue(e.GetType(), out var format))
        {
            entry = format(ctx, e);
            return true;
        }

        entry = null;
        return false;
    }

    /// <summary>Registros com os formatadores dos eventos de combate do Core (T-008).</summary>
    public static CombatLogFormatters CreateDefault() =>
        Combat.Progression.ProgressionLog.Register(
            Combat.EncounterModule.RegisterLog(CoreCombatLogFormatters.Register(new CombatLogFormatters())));
}
