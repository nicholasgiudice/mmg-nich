using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Mini mapa (T-392), no canto superior direito: o recorte em volta do jogador, com os portões e o grupo, numa moldura
/// fina de pedra. Clique abre o mapa grande da zona (<see cref="ZoneMapWindow"/>, também na tecla M).
/// </summary>
public partial class MiniMapPanel : PanelContainer
{
    private readonly MiniMapCanvas _canvas = new();

    /// <summary>Clique no mini mapa.</summary>
    public event Action? Clicked;

    public MiniMapState State
    {
        get => _canvas.State;
        set => _canvas.State = value;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = "";
        AddThemeStyleboxOverride("panel", HudTheme.MemberFrame());
        _canvas.Whole = false;
        _canvas.CustomMinimumSize = new Vector2(MiniMapView.PanelPixels, MiniMapView.PanelPixels);
        AddChild(_canvas);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            Clicked?.Invoke();
            AcceptEvent();
        }
    }
}
