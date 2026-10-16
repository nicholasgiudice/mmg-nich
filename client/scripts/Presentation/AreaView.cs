using Game.Core.Combat.Areas;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Área persistente no chão (poça, teia, armadilha). Estilo pelo id da área; áreas desconhecidas usam
/// roxo (hostil) ou azul (aliada). Fica tracejada enquanto não está armada e some aos poucos no fim.
/// </summary>
public partial class AreaView : Node2D
{
    private const float FadeTicks = 10f;

    private AreaState? _state;
    private float _alpha = 1f;
    private bool _armed;
    private bool _hostile;
    private double _time;

    public void Apply(AreaState state, Vector2 centerPx, double now, bool hostile)
    {
        ZIndex = ZLayers.Areas;
        Position = centerPx;
        _state = state;
        _hostile = hostile;
        _armed = now >= state.ArmedAtTick;
        _alpha = Mathf.Clamp((float)((state.ExpiresAtTick - now) / FadeTicks), 0f, 1f);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw(); // bolhas da poça se mexem
    }

    public override void _Draw()
    {
        if (_state is null)
        {
            return;
        }

        var r = _state.Radius * Units.PixelsPerUnit;
        var (color, kind) = Style(_state.AreaId, _hostile);
        var a = _alpha;
        var fill = new Color(color, 0.28f * a);
        var edge = new Color(color, (_armed ? 0.9f : 0.45f) * a);

        switch (kind)
        {
            case Kind.Web:
                DrawCircle(Vector2.Zero, r, new Color(color, 0.16f * a));
                for (var i = 0; i < 8; i++)
                {
                    DrawLine(Vector2.Zero, Vector2.FromAngle(i * Mathf.Tau / 8) * r, new Color(color, 0.45f * a), 1f);
                }

                for (var ring = 1; ring <= 3; ring++)
                {
                    DrawArc(Vector2.Zero, r * ring / 3f, 0, Mathf.Tau, 8, new Color(color, 0.40f * a), 1f);
                }

                break;

            case Kind.Trap:
                DrawCircle(Vector2.Zero, r, new Color(color, (_armed ? 0.18f : 0.08f) * a));
                for (var i = 0; i < 8; i++)
                {
                    var ang = i * Mathf.Tau / 8;
                    DrawLine(Vector2.FromAngle(ang) * r * 0.55f, Vector2.FromAngle(ang) * r * 0.85f, edge, 2f);
                }

                break;

            default: // poça: bolhas lentas
                DrawCircle(Vector2.Zero, r, fill);
                for (var i = 0; i < 5; i++)
                {
                    var t = (float)_time * 0.6f + i * 1.7f;
                    var p = new Vector2(Mathf.Sin(t * 1.3f + i), Mathf.Cos(t + i * 2f)) * r * 0.55f;
                    DrawCircle(p, r * 0.08f, new Color(color.Lightened(0.3f), 0.35f * a));
                }

                break;
        }

        if (_armed)
        {
            DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 48, edge, 2f);
        }
        else
        {
            DrawDashedRing(r, edge);
        }
    }

    private void DrawDashedRing(float r, Color color)
    {
        const int dashes = 16;
        for (var i = 0; i < dashes; i++)
        {
            var from = i * Mathf.Tau / dashes;
            DrawArc(Vector2.Zero, r, from, from + Mathf.Tau / dashes * 0.5f, 4, color, 2f);
        }
    }

    private enum Kind
    {
        Pool,
        Web,
        Trap,
    }

    private static (Color, Kind) Style(string areaId, bool hostile) => areaId switch
    {
        "area.venom_pool" => (new Color(0.45f, 0.85f, 0.25f), Kind.Pool),
        "area.sticky_web" => (new Color(0.92f, 0.92f, 0.85f), Kind.Web),
        "area.containment_trap" => (new Color(0.95f, 0.75f, 0.30f), Kind.Trap),
        _ when areaId.Contains("trap", StringComparison.Ordinal) => (new Color(0.95f, 0.75f, 0.30f), Kind.Trap),
        _ when areaId.Contains("web", StringComparison.Ordinal) => (new Color(0.92f, 0.92f, 0.85f), Kind.Web),
        _ => (hostile ? new Color(0.75f, 0.35f, 0.95f) : new Color(0.35f, 0.65f, 1.0f), Kind.Pool),
    };
}
