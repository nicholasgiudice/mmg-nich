using Game.Core.Combat.Areas;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Área persistente no chão (poça, teia, armadilha). Estilo pelo id da área; áreas desconhecidas usam
/// roxo (hostil) ou azul (aliada). Fica tracejada enquanto não está armada e some aos poucos no fim.
/// Com arte (T-054, <see cref="EffectCatalog.Area"/>), os quadros do pacote vão por baixo do contorno, que não muda.
/// </summary>
public partial class AreaView : Node2D
{
    private const float FadeTicks = 10f;

    private AreaState? _state;
    private float _alpha = 1f;
    private bool _armed;
    private bool _hostile;
    private double _time;
    private AreaEffect? _art;
    private int _seed;

    /// <summary>Arte do pacote para esta área (null = só a forma desenhada).</summary>
    public void SetArt(AreaEffect? art, int seed)
    {
        _art = art;
        _seed = seed;
    }

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
        if (kind == Kind.Hidden)
        {
            return; // áreas que só acompanham outra no mesmo lugar (a cura da Consagração, T-424; o veneno da Nuvem Tóxica, T-434): a outra já desenha
        }

        var a = _alpha;
        var fill = new Color(color, 0.28f * a);
        var edge = new Color(color, (_armed ? 0.9f : 0.45f) * a);

        if (_art is not null && kind == Kind.Pool)
        {
            // Hostil: o chão perigoso continua forte. T-444: o fogo do Piromante também, para ser lido de longe.
            var strong = _hostile || _state.AreaId.StartsWith("area.burning_ground", StringComparison.Ordinal)
                || _state.AreaId.StartsWith("area.meteor_flames", StringComparison.Ordinal);
            DrawCircle(Vector2.Zero, r, new Color(color, (strong ? 0.26f : 0.12f) * a));
            DrawArt(_art, r, a * EffectSettings.AreaAlpha * (_armed ? 1f : 0.5f));
            kind = Kind.None;
        }

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

            case Kind.Spin:
                // T-412, Tempestade de Aço: lâminas girando em volta de quem a usa (a área acompanha o dono).
                DrawCircle(Vector2.Zero, r, new Color(color, 0.10f * a));
                var spin = (float)_time * 9f;
                for (var i = 0; i < 4; i++)
                {
                    var from = spin + i * Mathf.Tau / 4;
                    DrawArc(Vector2.Zero, r * 0.82f, from, from + 0.9f, 10, new Color(color, 0.95f * a), 4f);
                    DrawArc(Vector2.Zero, r * 0.55f, from + 0.4f, from + 1.1f, 8, new Color(color, 0.55f * a), 2f);
                    var tip = Vector2.FromAngle(from + 0.9f) * r * 0.82f;
                    DrawCircle(tip, 2.5f, new Color(1f, 0.45f, 0.3f, a));
                }

                break;

            case Kind.Consecrate:
                DrawConsecration(r, color, a);
                break;

            case Kind.Cloud:
                DrawCloud(r, color, a);
                break;

            case Kind.None:
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

    /// <summary>
    /// Quadros do pacote dentro da área: um só do tamanho dela (<c>fill</c>) ou vários espalhados em posições fixas
    /// pela semente (<c>scatter</c>), cada um com fase própria para não piscarem juntos.
    /// </summary>
    private void DrawArt(AreaEffect art, float r, float alpha)
    {
        var modulate = new Color(1, 1, 1, alpha);
        if (!art.Scatter)
        {
            art.Animation.Draw(this, Vector2.Zero, r * 2, _time, modulate);
            return;
        }

        var size = Units.PixelsPerUnit * 1.1f;
        var count = Math.Clamp((int)(Mathf.Pi * r * r / (size * size) * 0.9f), 3, 28);
        var rng = new RandomNumberGenerator { Seed = (ulong)_seed };
        for (var i = 0; i < count; i++)
        {
            // Raiz da distância: distribuição uniforme no disco, sem passar da borda.
            var at = Vector2.FromAngle(rng.Randf() * Mathf.Tau) * Mathf.Sqrt(rng.Randf()) * (r - size * 0.4f);
            var rotate = art.Animation.Rotate ? Mathf.Pi / 2 : 0f; // flechas caindo
            var t = _time + rng.Randf() * art.Animation.Duration;
            DrawSetTransform(at, rotate);
            art.Animation.Draw(this, Vector2.Zero, size, t, modulate);
        }

        DrawSetTransform(Vector2.Zero);
    }

    /// <summary>
    /// T-424, Consagração: chão dourado com um anel de dentro e oito marcas girando devagar entre os anéis, e fiapos de
    /// luz subindo em pontos fixos (pela semente), cada um com fase própria.
    /// </summary>
    private void DrawConsecration(float r, Color color, float a)
    {
        var breath = 0.5f + 0.5f * Mathf.Sin((float)_time * 2.2f);
        DrawCircle(Vector2.Zero, r, new Color(color, (0.16f + 0.06f * breath) * a));
        DrawCircle(Vector2.Zero, r * 0.72f, new Color(color.Lightened(0.4f), 0.08f * a));
        DrawArc(Vector2.Zero, r * 0.72f, 0, Mathf.Tau, 40, new Color(color, 0.55f * a), 1.5f);
        var turn = (float)_time * 0.35f;
        for (var i = 0; i < 8; i++)
        {
            var dir = Vector2.FromAngle(turn + i * Mathf.Tau / 8);
            DrawLine(dir * r * 0.78f, dir * r * 0.94f, new Color(color.Lightened(0.3f), 0.85f * a), 3f);
        }

        var rng = new RandomNumberGenerator { Seed = (ulong)_seed };
        var count = Math.Clamp((int)(r / Units.PixelsPerUnit * 3), 5, 14);
        for (var i = 0; i < count; i++)
        {
            var at = Vector2.FromAngle(rng.Randf() * Mathf.Tau) * Mathf.Sqrt(rng.Randf()) * r * 0.85f;
            var k = (float)((_time * 0.8 + rng.Randf()) % 1.0);
            var rise = k * 14f;
            var mote = new Color(1f, 0.95f, 0.7f, (1f - k) * 0.9f * a);
            DrawLine(at - new Vector2(0, rise), at - new Vector2(0, rise + 5f), mote, 2f);
        }
    }

    /// <summary>
    /// T-434, Nuvem Tóxica: gás verde baixo, de bolotas que se mexem devagar e se misturam, com bolhas subindo. Sem arte
    /// do pacote: só círculos, para ficar simples e ler de longe.
    /// </summary>
    private void DrawCloud(float r, Color color, float a)
    {
        DrawCircle(Vector2.Zero, r, new Color(color.Darkened(0.55f), 0.30f * a));
        var t = (float)_time;
        const int puffs = 12;
        var rim = new Color(color.Darkened(0.5f), 0.55f * a);
        var body = new Color(color, 0.42f * a);
        var shine = new Color(color.Lightened(0.45f), 0.35f * a);
        // Bolotas espalhadas por igual (ângulo de ouro), girando devagar; primeiro as bordas escuras, depois o miolo, para
        // as bolotas se fundirem numa nuvem só com um contorno recortado.
        var spots = new (Vector2 At, float Size)[puffs];
        for (var i = 0; i < puffs; i++)
        {
            var angle = i * 2.39996f + t * 0.18f + _seed;
            var dist = Mathf.Sqrt((i + 0.5f) / puffs) * r * 0.66f;
            var size = r * 0.30f * (0.9f + 0.1f * Mathf.Sin(t * 1.6f + i * 1.3f));
            spots[i] = (Vector2.FromAngle(angle) * dist, size);
        }

        foreach (var (at, size) in spots)
        {
            DrawCircle(at, size + 2f, rim);
        }

        foreach (var (at, size) in spots)
        {
            DrawCircle(at, size, body);
        }

        foreach (var (at, size) in spots)
        {
            DrawCircle(at + new Vector2(-size * 0.25f, -size * 0.3f), size * 0.45f, shine);
        }

        for (var i = 0; i < 7; i++)
        {
            var k = (t * 0.45f + i / 7f) % 1f;
            var x = Mathf.Sin(i * 2.9f) * r * 0.6f;
            var y = Mathf.Cos(i * 1.7f) * r * 0.45f - k * Units.PixelsPerUnit * 0.8f;
            DrawCircle(new Vector2(x, y), 2f + 1.5f * (1f - k), new Color(color.Lightened(0.6f), 0.8f * (1f - k) * a));
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
        None,
        Pool,
        Web,
        Trap,
        Spin,

        /// <summary>T-424: chão consagrado (dourado).</summary>
        Consecrate,

        /// <summary>Gás no chão (T-434, Nuvem Tóxica).</summary>
        Cloud,

        /// <summary>Não desenha nada, nem o contorno: área que acompanha outra no mesmo lugar (a cura da Consagração, T-424; o veneno da Nuvem Tóxica, T-434).</summary>
        Hidden,
    }

    private static (Color, Kind) Style(string areaId, bool hostile) => areaId switch
    {
        "area.venom_pool" => (new Color(0.45f, 0.85f, 0.25f), Kind.Pool),
        "area.sticky_web" => (new Color(0.92f, 0.92f, 0.85f), Kind.Web),
        "area.containment_trap" => (new Color(0.95f, 0.75f, 0.30f), Kind.Trap),
        _ when areaId.StartsWith("area.toxic_cloud_venom", StringComparison.Ordinal) => (Colors.Transparent, Kind.Hidden),
        _ when areaId.StartsWith("area.toxic_cloud", StringComparison.Ordinal) => (new Color(0.62f, 0.86f, 0.18f), Kind.Cloud),
        _ when areaId.StartsWith("area.steel_storm", StringComparison.Ordinal) => (new Color(0.85f, 0.88f, 0.95f), Kind.Spin),
        _ when areaId.StartsWith("area.consecration_harm", StringComparison.Ordinal) => (new Color(1.0f, 0.80f, 0.32f), Kind.Consecrate),
        _ when areaId.StartsWith("area.consecration_mend", StringComparison.Ordinal) => (Colors.White, Kind.Hidden),
        _ when areaId.StartsWith("area.burning_ground", StringComparison.Ordinal)
            || areaId.StartsWith("area.meteor_flames", StringComparison.Ordinal) => (new Color(1f, 0.45f, 0.12f), Kind.Pool), // T-444
        _ when areaId.Contains("trap", StringComparison.Ordinal) => (new Color(0.95f, 0.75f, 0.30f), Kind.Trap),
        _ when areaId.Contains("web", StringComparison.Ordinal) => (new Color(0.92f, 0.92f, 0.85f), Kind.Web),
        _ => (hostile ? new Color(0.75f, 0.35f, 0.95f) : new Color(0.35f, 0.65f, 1.0f), Kind.Pool),
    };
}
