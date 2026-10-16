using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Estado de um slot da barra de habilidades (calculado pelo HUD a partir do snapshot).</summary>
public readonly record struct AbilitySlotState(
    string Abbreviation,
    string Key,
    string Cost,
    float CooldownFraction,
    float CooldownSeconds,
    bool NoResource,
    bool OutOfRange,
    bool Casting,
    string? Charges);

/// <summary>Um slot: sigla, tecla, custo, varredura de recarga e estado indisponível.</summary>
public partial class AbilitySlot : Control
{
    public const float SlotSize = 52f;

    private static readonly Color Background = new(0.18f, 0.20f, 0.26f);
    private static readonly Color CooldownShade = new(0f, 0f, 0f, 0.65f);
    private static readonly Color NoResourceTint = new(0.25f, 0.30f, 0.75f, 0.55f);
    private static readonly Color OutOfRangeTint = new(0.80f, 0.20f, 0.20f, 0.40f);
    private static readonly Color CastingBorder = new(1f, 0.85f, 0.3f);

    private AbilitySlotState _state;

    public AbilitySlot()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        TooltipText = "";
    }

    public void SetState(AbilitySlotState state, string tooltip)
    {
        TooltipText = tooltip;
        if (state == _state)
        {
            return;
        }

        _state = state;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.Zero, Size);
        var font = ThemeDB.FallbackFont;
        DrawRect(rect, Background);
        DrawString(font, new Vector2(0, Size.Y / 2 + 6), _state.Abbreviation, HorizontalAlignment.Center, Size.X, 16, Colors.White);

        if (_state.NoResource)
        {
            DrawRect(rect, NoResourceTint);
        }
        else if (_state.OutOfRange)
        {
            DrawRect(rect, OutOfRangeTint);
        }

        if (_state.CooldownFraction > 0)
        {
            DrawSweep(_state.CooldownFraction);
            DrawStringOutline(font, new Vector2(0, Size.Y / 2 + 6), $"{_state.CooldownSeconds:0.0}", HorizontalAlignment.Center, Size.X, 15, 4, Colors.Black);
            DrawString(font, new Vector2(0, Size.Y / 2 + 6), $"{_state.CooldownSeconds:0.0}", HorizontalAlignment.Center, Size.X, 15, Colors.White);
        }

        DrawString(font, new Vector2(3, 12), _state.Key, HorizontalAlignment.Left, -1, 11, new Color(1, 1, 1, 0.9f));
        if (_state.Cost.Length > 0)
        {
            DrawString(font, new Vector2(0, Size.Y - 3), _state.Cost, HorizontalAlignment.Right, Size.X - 3, 10, new Color(0.8f, 0.9f, 1f));
        }

        if (_state.Charges is { } charges)
        {
            DrawString(font, new Vector2(0, 12), charges, HorizontalAlignment.Right, Size.X - 3, 11, new Color(1f, 0.9f, 0.5f));
        }

        DrawRect(rect, _state.Casting ? CastingBorder : Colors.Black, filled: false, width: _state.Casting ? 2.5f : 1f);
    }

    /// <summary>Setor escuro no sentido horário a partir do topo, proporcional à recarga restante.</summary>
    private void DrawSweep(float fraction)
    {
        var center = Size / 2;
        var points = new List<Vector2> { center };
        const int segments = 32;
        var start = -Mathf.Pi / 2;
        var sweep = Mathf.Tau * fraction;
        for (var i = 0; i <= segments; i++)
        {
            // Raio do centro até a borda do quadrado nesse ângulo.
            var dir = Vector2.FromAngle(start + sweep * i / segments);
            var t = Mathf.Min(
                Mathf.Abs(dir.X) > 1e-5f ? center.X / Mathf.Abs(dir.X) : float.MaxValue,
                Mathf.Abs(dir.Y) > 1e-5f ? center.Y / Mathf.Abs(dir.Y) : float.MaxValue);
            points.Add(center + dir * t);
        }

        if (fraction >= 0.999f)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), CooldownShade);
            return;
        }

        DrawColoredPolygon(points.ToArray(), CooldownShade);
    }
}
