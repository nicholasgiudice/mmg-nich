using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Efeito de passagem (T-054): impacto parado num ponto ou projétil indo de um ponto a outro. Toca a animação e se
/// apaga. Fica abaixo do contorno dos telegráfos (<see cref="ZLayers.Effects"/>).
/// </summary>
public partial class EffectView : Node2D
{
    /// <summary>Velocidade do projétil desenhado (pixels/s); o acerto no Core já aconteceu, isto é só leitura.</summary>
    private const float ProjectileSpeedPx = 900f;
    private const double MaxFlightSeconds = 0.22;

    private EffectAnimation _anim = null!;
    private Vector2 _from;
    private Vector2 _to;
    private float _size;
    private double _delay;
    private double _flight;
    private double _age;
    private float _alpha = 1f;

    /// <summary>Impacto em <paramref name="atPx"/>, depois de <paramref name="delay"/> segundos (chegada do projétil).</summary>
    public static EffectView Impact(EffectAnimation anim, Vector2 atPx, float sizePx, double delay = 0, float alpha = 1f) =>
        new() { _anim = anim, _from = atPx, _to = atPx, _size = sizePx, _delay = delay, _alpha = alpha, ZIndex = ZLayers.Effects, Position = atPx };

    /// <summary>Projétil de <paramref name="fromPx"/> a <paramref name="toPx"/>; <see cref="FlightSeconds"/> diz quando chega.</summary>
    public static EffectView Projectile(EffectAnimation anim, Vector2 fromPx, Vector2 toPx, float sizePx, float alpha = 1f) =>
        new()
        {
            _anim = anim,
            _from = fromPx,
            _to = toPx,
            _size = sizePx,
            _alpha = alpha,
            ZIndex = ZLayers.Effects,
            Position = fromPx,
            _flight = FlightSeconds(fromPx, toPx),
        };

    public static double FlightSeconds(Vector2 fromPx, Vector2 toPx) => Math.Min(fromPx.DistanceTo(toPx) / ProjectileSpeedPx, MaxFlightSeconds);

    public override void _Ready() => TextureFilter = TextureFilterEnum.Nearest;

    public override void _Process(double delta)
    {
        _age += delta;
        var t = _age - _delay;
        var done = _flight > 0 ? t >= _flight : t >= _anim.Duration;
        if (done)
        {
            QueueFree();
            return;
        }

        if (_flight > 0 && t >= 0)
        {
            Position = _from.Lerp(_to, (float)(t / _flight));
            Rotation = _anim.Rotate ? (_to - _from).Angle() : 0;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = _age - _delay;
        if (t >= 0)
        {
            _anim.Draw(this, Vector2.Zero, _size, t, new Color(1, 1, 1, _alpha));
        }
    }
}
