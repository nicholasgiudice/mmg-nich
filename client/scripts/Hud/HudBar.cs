using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Barra simples desenhada à mão: fundo, preenchimento, sobreposição (escudo) e texto central.</summary>
public partial class HudBar : Control
{
    private float _fill;
    private float _overlay;
    private Color _fillColor = HudTheme.Health;
    private string _text = "";

    public Color OverlayColor { get; set; } = HudTheme.Shield;

    public int FontSize { get; set; } = 12;

    /// <summary>Atualiza (redesenha só se algo mudou). Frações em 0..1; overlay começa onde o fill termina.</summary>
    public void Set(float fill, Color color, string text, float overlay = 0f)
    {
        fill = Mathf.Clamp(fill, 0f, 1f);
        overlay = Mathf.Clamp(overlay, 0f, 1f);
        if (Mathf.IsEqualApprox(fill, _fill) && Mathf.IsEqualApprox(overlay, _overlay) && color == _fillColor && text == _text)
        {
            return;
        }

        _fill = fill;
        _overlay = overlay;
        _fillColor = color;
        _text = text;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), HudTheme.BarBack);
        DrawRect(new Rect2(Vector2.Zero, new Vector2(size.X * _fill, size.Y)), _fillColor);
        if (_overlay > 0)
        {
            var start = Mathf.Min(_fill, 1f - _overlay);
            DrawRect(new Rect2(new Vector2(size.X * start, 0), new Vector2(size.X * _overlay, size.Y)), OverlayColor);
        }

        DrawRect(new Rect2(Vector2.Zero, size), Colors.Black, filled: false, width: 1f);
        if (_text.Length > 0)
        {
            var font = ThemeDB.FallbackFont;
            var y = (size.Y + font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2f;
            DrawStringOutline(font, new Vector2(0, y), _text, HorizontalAlignment.Center, size.X, FontSize, 3, Colors.Black);
            DrawString(font, new Vector2(0, y), _text, HorizontalAlignment.Center, size.X, FontSize, Colors.White);
        }
    }
}
