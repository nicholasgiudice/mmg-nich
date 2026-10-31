using Godot;

namespace MmoRpgClient.Input;

/// <summary>
/// Roteiro de input para verificação sem teclado (<c>--demo-input=attack,ability_1,...</c>):
/// aperta uma ação do Input Map a cada intervalo, pelo mesmo caminho do teclado/mouse. Itens com <c>:</c> (ex.:
/// <c>spend:str</c>) ou <c>reset_points</c> vão para <see cref="Command"/> em vez do Input Map (T-221).
/// </summary>
public sealed class DebugInputScript
{
    private const double Interval = 0.6;
    private const double StartDelay = 0.5;

    private readonly Queue<string> _actions;
    private readonly HashSet<string> _held;
    private string? _pressedLastFrame;
    private double _next = StartDelay;
    private double _elapsed;

    /// <param name="script">Ações separadas por vírgula, apertadas uma por vez.</param>
    /// <param name="held">Ações mantidas o tempo todo (ex.: movimento de <c>--auto-move</c>).</param>
    public DebugInputScript(string script, IEnumerable<string> held)
    {
        _actions = new Queue<string>(script.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        _held = held.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Recebe os itens do roteiro que não são ações do Input Map (ex.: botões das telas, T-221).</summary>
    public Action<string>? Command { get; init; }

    /// <summary>Todas as ações do roteiro já foram apertadas.</summary>
    public bool Finished => _actions.Count == 0;

    /// <summary>Chamar no começo do frame, antes de ler o input.</summary>
    public void Update(double delta)
    {
        _elapsed += delta;
        foreach (var a in _held)
        {
            Godot.Input.ActionPress(a);
        }

        if (_pressedLastFrame is not null)
        {
            Godot.Input.ActionRelease(_pressedLastFrame);
            _pressedLastFrame = null;
        }

        if (_elapsed >= _next && _actions.TryDequeue(out var action))
        {
            _next = _elapsed + Interval;
            GD.Print($"[demo] {action}");
            if (!InputMap.HasAction(action) && Command is not null)
            {
                Command(action);
                return;
            }

            Godot.Input.ActionPress(action);
            _pressedLastFrame = action;
        }
    }
}
