using Godot;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Câmera que segue o jogador com suavização leve, deslocada um pouco na direção da mira e
/// limitada às bordas da arena (arena menor que a tela fica centralizada).
/// </summary>
public partial class CameraRig : Camera2D
{
    /// <summary>Quanto da distância jogador→mira a câmera anda (0..1).</summary>
    private const float AimLead = 0.2f;

    /// <summary>Deslocamento máximo pela mira, em pixels.</summary>
    private const float MaxAimOffset = 80f;

    /// <summary>Rapidez da suavização (1/s); maior = mais rígida.</summary>
    private const float FollowSharpness = 10f;

    private Rect2? _bounds;
    private bool _snapped;

    /// <summary>Define as bordas (em pixels) a partir do mapa da arena.</summary>
    public void SetBounds(GridMap map) =>
        _bounds = new Rect2(0, 0, map.Width * Units.PixelsPerUnit, map.Height * Units.PixelsPerUnit);

    /// <summary>Chamar uma vez por frame com a posição desenhada do jogador e o ponto de mira (pixels).</summary>
    public void Follow(Vector2 playerPx, Vector2 aimPx, double delta)
    {
        var lead = (aimPx - playerPx) * AimLead;
        if (lead.Length() > MaxAimOffset)
        {
            lead = lead.Normalized() * MaxAimOffset;
        }

        var target = Clamp(playerPx + lead);
        if (!_snapped)
        {
            Position = target;
            _snapped = true;
            return;
        }

        var t = 1f - Mathf.Exp(-FollowSharpness * (float)delta);
        Position = Clamp(Position.Lerp(target, t));
    }

    private Vector2 Clamp(Vector2 p)
    {
        if (_bounds is not { } b)
        {
            return p;
        }

        var half = GetViewportRect().Size / (2f * Zoom);
        return new Vector2(ClampAxis(p.X, b.Position.X, b.End.X, half.X), ClampAxis(p.Y, b.Position.Y, b.End.Y, half.Y));
    }

    private static float ClampAxis(float v, float min, float max, float half) =>
        max - min <= 2 * half ? (min + max) / 2 : Mathf.Clamp(v, min + half, max - half);
}
