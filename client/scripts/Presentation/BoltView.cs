using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Projétil desenhado pelo cliente (T-444): bola brilhante com rastro, de um ponto a outro, e some ao chegar. Com
/// <see cref="OnArrive"/>, chama quem criou na chegada (a explosão da Bola de Fogo). Só visual: o acerto no Core já
/// aconteceu.
/// </summary>
public partial class BoltView : Node2D
{
    /// <summary>Velocidade (pixels/s) e tempo máximo de voo: mais lento que as flechas, para a bola ser vista.</summary>
    private const float SpeedPx = 650f;
    private const double MaxFlightSeconds = 0.35;

    /// <summary>Pedaços do rastro atrás da bola.</summary>
    private const int TailParts = 5;

    private SpellBolt _bolt = new(Colors.White, 8f, false);
    private Vector2 _from;
    private Vector2 _to;
    private double _flight;
    private double _age;

    /// <summary>Chamado uma vez, quando a bola chega (posição de chegada, pixels).</summary>
    public Action<Vector2>? OnArrive { get; set; }

    /// <summary>Alvo que a bola persegue (a chegada acompanha o corpo dele, que anda durante o voo); null = ponto fixo.</summary>
    public Node2D? Target { get; set; }

    /// <summary>Ponto de chegada relativo ao pivô (pés) de <see cref="Target"/>.</summary>
    public Vector2 TargetOffset { get; set; }

    public static BoltView Create(SpellBolt bolt, Vector2 fromPx, Vector2 toPx) => new()
    {
        _bolt = bolt,
        _from = fromPx,
        _to = toPx,
        _flight = FlightSeconds(fromPx, toPx),
        Position = fromPx,
        ZAsRelative = false,
        ZIndex = ZLayers.Effects,
    };

    public static double FlightSeconds(Vector2 fromPx, Vector2 toPx) => Math.Min(fromPx.DistanceTo(toPx) / SpeedPx, MaxFlightSeconds);

    public override void _Process(double delta)
    {
        _age += delta;
        if (Target is not null && IsInstanceValid(Target))
        {
            _to = Target.Position + TargetOffset;
        }

        if (_age >= _flight)
        {
            OnArrive?.Invoke(_to);
            QueueFree();
            return;
        }

        Position = _from.Lerp(_to, (float)(_age / _flight));
        QueueRedraw();
    }

    public override void _Draw()
    {
        var dir = (_to - _from).Normalized();
        var s = _bolt.SizePx;
        var flicker = 0.85f + 0.15f * Mathf.Sin((float)_age * 40f);
        for (var i = TailParts; i >= 1; i--)
        {
            var k = i / (float)TailParts;
            DrawCircle(-dir * s * 0.9f * i, s * (1f - k * 0.7f) * 0.8f, new Color(_bolt.Color, 0.55f * (1f - k)));
        }

        DrawCircle(Vector2.Zero, s * 1.35f * flicker, new Color(_bolt.Color, 0.35f));
        DrawCircle(Vector2.Zero, s, _bolt.Color);
        DrawCircle(Vector2.Zero, s * 0.5f, _bolt.Color.Lightened(0.6f));
    }
}
