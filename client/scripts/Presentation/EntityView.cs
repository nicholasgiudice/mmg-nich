using Game.Core.Combat;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Placeholder de uma unidade: círculo pela facção, indicador de direção, nome e barra de vida.
/// Só exibe o que recebe do snapshot; não altera nada na simulação.
/// </summary>
public partial class EntityView : Node2D
{
    private static readonly Color PlayerColor = new(0.30f, 0.55f, 0.95f);
    private static readonly Color ControlledColor = new(0.35f, 0.85f, 1.00f);
    private static readonly Color MonsterColor = new(0.85f, 0.30f, 0.25f);
    private static readonly Color DeadColor = new(0.45f, 0.45f, 0.45f);
    private static readonly Color BarBack = new(0.10f, 0.10f, 0.10f, 0.85f);
    private static readonly Color BarFill = new(0.35f, 0.85f, 0.35f);
    private static readonly Color SelectionColor = new(1f, 0.85f, 0.2f);

    private static readonly Color StunColor = new(1f, 0.9f, 0.3f);
    private static readonly Color RootColor = new(0.55f, 0.38f, 0.20f);
    private static readonly Color FrozenColor = new(0.65f, 0.9f, 1f);
    private static readonly Color SilenceColor = new(0.75f, 0.45f, 1f);

    private const float BarWidth = 36f;
    private const float BarHeight = 4f;
    private const float ChipWidth = 15f;
    private const float ChipHeight = 10f;
    private const double FlashSeconds = 0.12;

    private readonly Label _nameLabel = new();
    private float _radiusPx = 0.4f * Units.PixelsPerUnit;
    private Color _color = MonsterColor;
    private Godot.Vector2 _facing = Godot.Vector2.Down;
    private float _healthFraction = 1f;
    private bool _alive = true;
    private bool _selected;
    private IReadOnlyList<StatusChip> _chips = Array.Empty<StatusChip>();
    private ControlMarks _marks;
    private double _flash;
    private double _time;

    public EntityId Id { get; private set; }

    public bool IsAlive => _alive;

    public float RadiusPx => _radiusPx;

    public static EntityView Create(EntityId id) => new() { Id = id, Name = $"Entity{id.Value}" };

    public override void _Ready()
    {
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _nameLabel.AddThemeFontSizeOverride("font_size", 11);
        _nameLabel.AddThemeConstantOverride("outline_size", 3);
        _nameLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
        _nameLabel.Size = new Godot.Vector2(120, 16);
        AddChild(_nameLabel);
    }

    /// <summary>Atualiza dados lentos (vida, nome, cor) a partir do snapshot mais recente.</summary>
    public void ApplyState(EntitySnapshot state, bool controlled, bool selected)
    {
        var radius = (state.TryGet<Collider>()?.Radius ?? 0.4f) * Units.PixelsPerUnit;
        var health = state.TryGet<Health>();
        var alive = health?.IsAlive ?? true;
        var fraction = health is { Max: > 0 } ? Math.Clamp(health.Current / health.Max, 0f, 1f) : 1f;
        var faction = state.TryGet<FactionTag>()?.Faction ?? Faction.Monster;
        var color = !alive ? DeadColor
            : faction == Faction.Player ? (controlled ? ControlledColor : PlayerColor)
            : MonsterColor;

        if (radius != _radiusPx || alive != _alive || fraction != _healthFraction || color != _color || selected != _selected)
        {
            _selected = selected;
            _radiusPx = radius;
            _alive = alive;
            _healthFraction = fraction;
            _color = color;
            QueueRedraw();
        }

        var name = state.TryGet<UnitInfo>()?.Name ?? "";
        if (_nameLabel.Text != name)
        {
            _nameLabel.Text = name;
        }

        _nameLabel.Position = new Godot.Vector2(-_nameLabel.Size.X / 2, _radiusPx + 2);
        ZIndex = alive ? ZLayers.Units : ZLayers.DeadUnits;
    }

    /// <summary>Ícones de status e marcas de controle no corpo.</summary>
    public void ApplyStatus(IReadOnlyList<StatusChip> chips, ControlMarks marks)
    {
        if (!_alive)
        {
            chips = Array.Empty<StatusChip>();
            marks = ControlMarks.None;
        }

        if (marks != _marks || !chips.SequenceEqual(_chips))
        {
            _chips = chips;
            _marks = marks;
            QueueRedraw();
        }
    }

    /// <summary>Pisca em branco (recebeu dano).</summary>
    public void Flash()
    {
        _flash = FlashSeconds;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        if (_flash > 0)
        {
            _flash = Math.Max(0, _flash - delta);
            QueueRedraw();
        }
        else if ((_marks & ControlMarks.Stunned) != 0)
        {
            QueueRedraw(); // estrelas girando
        }
    }

    /// <summary>Pose interpolada deste frame (pixels e direção normalizada).</summary>
    public void SetPose(Godot.Vector2 positionPx, Godot.Vector2 facing)
    {
        Position = positionPx;
        if (facing.LengthSquared() > 1e-6f && !facing.IsEqualApprox(_facing))
        {
            _facing = facing.Normalized();
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_selected)
        {
            DrawArc(Godot.Vector2.Zero, _radiusPx + 5, 0, Mathf.Tau, 32, SelectionColor, 2.5f);
        }

        if ((_marks & ControlMarks.Rooted) != 0)
        {
            // Raízes: espinhos marrons em volta dos pés.
            for (var i = 0; i < 10; i++)
            {
                var dir = Godot.Vector2.FromAngle(i * Mathf.Tau / 10);
                DrawLine(dir * (_radiusPx + 1), dir * (_radiusPx + 7), RootColor, 3f);
            }
        }

        var body = _flash > 0 ? _color.Lerp(Colors.White, (float)(_flash / FlashSeconds) * 0.8f) : _color;
        DrawCircle(Godot.Vector2.Zero, _radiusPx, body);
        if ((_marks & ControlMarks.Frozen) != 0)
        {
            DrawCircle(Godot.Vector2.Zero, _radiusPx + 3, new Color(FrozenColor, 0.55f));
            DrawArc(Godot.Vector2.Zero, _radiusPx + 3, 0, Mathf.Tau, 6, Colors.White, 2f);
        }

        DrawArc(Godot.Vector2.Zero, _radiusPx, 0, Mathf.Tau, 24, Colors.Black, 1.5f);
        if (_alive)
        {
            DrawLine(Godot.Vector2.Zero, _facing * (_radiusPx + 6), Colors.White, 2f);
        }

        var barPos = new Godot.Vector2(-BarWidth / 2, -_radiusPx - 8);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth, BarHeight)), BarBack);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth * _healthFraction, BarHeight)), BarFill);

        if ((_marks & ControlMarks.Stunned) != 0)
        {
            // Estrelas girando sobre a cabeça.
            var center = new Godot.Vector2(0, -_radiusPx - 14);
            for (var i = 0; i < 3; i++)
            {
                var a = (float)_time * 4f + i * Mathf.Tau / 3;
                DrawCircle(center + new Godot.Vector2(Mathf.Cos(a) * 9f, Mathf.Sin(a) * 3f), 2.5f, StunColor);
            }
        }

        if ((_marks & ControlMarks.Silenced) != 0)
        {
            var c = new Godot.Vector2(_radiusPx + 4, -_radiusPx);
            DrawArc(c, 4f, 0, Mathf.Tau, 12, SilenceColor, 1.5f);
            DrawLine(c + new Godot.Vector2(-3, 3), c + new Godot.Vector2(3, -3), SilenceColor, 1.5f);
        }

        DrawChips(barPos.Y - 3);
    }

    private void DrawChips(float bottomY)
    {
        if (_chips.Count == 0)
        {
            return;
        }

        var font = ThemeDB.FallbackFont;
        var total = _chips.Count * (ChipWidth + 1) - 1;
        var x = -total / 2;
        var y = bottomY - ChipHeight;
        foreach (var chip in _chips)
        {
            var rect = new Rect2(x, y, ChipWidth, ChipHeight);
            DrawRect(rect, new Color(chip.Color, 0.85f));
            DrawRect(rect, Colors.Black, filled: false, width: 1f);
            DrawString(font, new Godot.Vector2(x, y + ChipHeight - 2), chip.Abbreviation, HorizontalAlignment.Center, ChipWidth, 8, Colors.Black);
            x += ChipWidth + 1;
        }
    }
}
