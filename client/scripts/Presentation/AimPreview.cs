using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Prévia da habilidade de área com a tecla apertada (T-028): anel do alcance máximo em volta do jogador e a
/// forma que será atingida. Ciano discreto, para não ser confundida com telegráfos inimigos (laranja).
/// </summary>
public partial class AimPreview : Node2D
{
    private static readonly Color Fill = new(0.35f, 0.9f, 1f, 0.16f);
    private static readonly Color Edge = new(0.35f, 0.9f, 1f, 0.8f);
    private static readonly Color RangeRing = new(1f, 1f, 1f, 0.25f);

    private AbilityDefinition? _ability;
    private Vector2 _playerPx;
    private Vector2 _pointPx;

    /// <summary>Mostra a prévia (ou esconde com <paramref name="ability"/> null). Posições em pixels.</summary>
    public void Show(AbilityDefinition? ability, Vector2 playerPx, Vector2 pointPx)
    {
        ZIndex = ZLayers.Telegraphs;
        Visible = ability is not null;
        if (ability == _ability && playerPx.IsEqualApprox(_playerPx) && pointPx.IsEqualApprox(_pointPx))
        {
            return;
        }

        _ability = ability;
        _playerPx = playerPx;
        _pointPx = pointPx;
        QueueRedraw();
    }

    public override void _Draw()
    {
        const float s = Units.PixelsPerUnit;
        switch (_ability?.Targeting)
        {
            case CircleAtPointTargeting point:
                DrawDashedRing(_playerPx, (float)point.Range * s);
                DrawCircle(_pointPx, (float)point.Radius * s, Fill);
                DrawArc(_pointPx, (float)point.Radius * s, 0, Mathf.Tau, 48, Edge, 2f);
                DrawLine(_playerPx, _pointPx, RangeRing, 1f);
                break;

            case LineTargeting line:
                var dir = (_pointPx - _playerPx).LengthSquared() > 1e-4f ? (_pointPx - _playerPx).Normalized() : Vector2.Down;
                var side = dir.Orthogonal() * (float)line.Width * s / 2f;
                var tip = _playerPx + dir * (float)line.Length * s;
                Vector2[] rect = [_playerPx + side, tip + side, tip - side, _playerPx - side];
                DrawColoredPolygon(rect, Fill);
                DrawPolyline([.. rect, rect[0]], Edge, 2f);
                break;
        }
    }

    private void DrawDashedRing(Vector2 center, float radius)
    {
        const int dashes = 36;
        for (var i = 0; i < dashes; i++)
        {
            var from = i * Mathf.Tau / dashes;
            DrawArc(center, radius, from, from + Mathf.Tau / dashes * 0.5f, 4, RangeRing, 1.5f);
        }
    }
}
