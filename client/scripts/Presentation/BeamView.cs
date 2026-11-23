using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>
/// Raios das canalizações em andamento (T-444: Raio Arcano, Feixe do Vazio), de quem canaliza até o alvo. Refeitos a cada
/// frame pela <see cref="WorldView"/>, com as posições já interpoladas, como as linhas da <see cref="LinkView"/>. A força
/// (0 a 1) deixa o raio mais largo e mais claro (o Raio Arcano sobe a cada pulso).
/// </summary>
public partial class BeamView : Node2D
{
    private readonly List<(Vector2 From, Vector2 To, ChannelBeam Beam, float Strength)> _beams = new();
    private double _time;

    public override void _Ready()
    {
        ZAsRelative = false;
        ZIndex = ZLayers.Effects;
    }

    public void Begin() => _beams.Clear();

    public void Add(Vector2 fromPx, Vector2 toPx, ChannelBeam beam, float strength) => _beams.Add((fromPx, toPx, beam, strength));

    public override void _Process(double delta)
    {
        _time += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var (from, to, beam, strength) in _beams)
        {
            if (from.DistanceTo(to) < 1f)
            {
                continue;
            }

            var wobble = 0.85f + 0.15f * Mathf.Sin((float)_time * 25f);
            var width = beam.WidthPx * (beam.Grow ? 0.6f + 1.2f * strength : 1f) * wobble;
            var core = beam.Color.Lightened(beam.Grow ? 0.35f + 0.4f * strength : 0.5f);
            DrawLine(from, to, new Color(beam.Color, 0.35f), width * 2.4f);
            DrawLine(from, to, beam.Color, width);
            DrawLine(from, to, core, Math.Max(1.5f, width * 0.35f));
            DrawCircle(from, width * 0.9f, new Color(beam.Color, 0.8f));
            DrawCircle(to, width * 1.3f * wobble, new Color(beam.Color, 0.6f));
            DrawCircle(to, width * 0.6f, core);
        }
    }
}
