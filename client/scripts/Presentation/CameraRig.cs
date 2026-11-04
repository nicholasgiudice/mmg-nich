using Godot;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Câmera que segue o jogador com suavização leve, limitada às bordas da arena. Arena menor que a tela é ampliada até
/// cobrir a tela inteira, sem faixa vazia em volta. O mouse não move a câmera: ela fica parada enquanto o jogador está parado.
/// </summary>
public partial class CameraRig : Camera2D
{
    /// <summary>Rapidez da suavização (1/s); maior = mais rígida.</summary>
    private const float FollowSharpness = 10f;

    private Rect2? _bounds;
    private bool _snapped;

    /// <summary>Define as bordas (em pixels) a partir do mapa da arena; a câmera vai direto ao jogador no próximo <see cref="Follow"/>.</summary>
    public void SetBounds(GridMap map)
    {
        _bounds = new Rect2(0, 0, map.Width * Units.PixelsPerUnit, map.Height * Units.PixelsPerUnit);
        _snapped = false;
    }

    /// <summary>Chamar uma vez por frame com a posição desenhada do jogador (pixels).</summary>
    public void Follow(Vector2 playerPx, double delta)
    {
        Zoom = Vector2.One * CoverZoom();
        var target = Clamp(playerPx);
        if (!_snapped)
        {
            Position = target;
            _snapped = true;
            return;
        }

        var t = 1f - Mathf.Exp(-FollowSharpness * (float)delta);
        Position = Clamp(Position.Lerp(target, t));
    }

    /// <summary>Aproximação mínima para a arena cobrir a tela (1 quando ela já é maior que a tela).</summary>
    private float CoverZoom()
    {
        if (_bounds is not { } b || b.Size.X <= 0 || b.Size.Y <= 0)
        {
            return 1f;
        }

        var screen = GetViewportRect().Size;
        return Mathf.Max(1f, Mathf.Max(screen.X / b.Size.X, screen.Y / b.Size.Y));
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
