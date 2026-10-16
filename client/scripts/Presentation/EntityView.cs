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

    private const float BarWidth = 36f;
    private const float BarHeight = 4f;

    private readonly Label _nameLabel = new();
    private float _radiusPx = 0.4f * Units.PixelsPerUnit;
    private Color _color = MonsterColor;
    private Godot.Vector2 _facing = Godot.Vector2.Down;
    private float _healthFraction = 1f;
    private bool _alive = true;

    public EntityId Id { get; private set; }

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
    public void ApplyState(EntitySnapshot state, bool controlled)
    {
        var radius = (state.TryGet<Collider>()?.Radius ?? 0.4f) * Units.PixelsPerUnit;
        var health = state.TryGet<Health>();
        var alive = health?.IsAlive ?? true;
        var fraction = health is { Max: > 0 } ? Math.Clamp(health.Current / health.Max, 0f, 1f) : 1f;
        var faction = state.TryGet<FactionTag>()?.Faction ?? Faction.Monster;
        var color = !alive ? DeadColor
            : faction == Faction.Player ? (controlled ? ControlledColor : PlayerColor)
            : MonsterColor;

        if (radius != _radiusPx || alive != _alive || fraction != _healthFraction || color != _color)
        {
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
        ZIndex = alive ? 1 : 0;
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
        DrawCircle(Godot.Vector2.Zero, _radiusPx, _color);
        DrawArc(Godot.Vector2.Zero, _radiusPx, 0, Mathf.Tau, 24, Colors.Black, 1.5f);
        if (_alive)
        {
            DrawLine(Godot.Vector2.Zero, _facing * (_radiusPx + 6), Colors.White, 2f);
        }

        var barPos = new Godot.Vector2(-BarWidth / 2, -_radiusPx - 8);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth, BarHeight)), BarBack);
        DrawRect(new Rect2(barPos, new Godot.Vector2(BarWidth * _healthFraction, BarHeight)), BarFill);
    }
}
