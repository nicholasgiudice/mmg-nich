namespace MmoRpgClient.Presentation;

/// <summary>Conversão entre unidades do mundo (1 unidade = 1 tile) e pixels.</summary>
public static class Units
{
    /// <summary>Única constante de escala do cliente.</summary>
    public const float PixelsPerUnit = 32f;

    public static Godot.Vector2 ToPixels(System.Numerics.Vector2 world) =>
        new(world.X * PixelsPerUnit, world.Y * PixelsPerUnit);

    public static System.Numerics.Vector2 ToWorld(Godot.Vector2 pixels) =>
        new(pixels.X / PixelsPerUnit, pixels.Y / PixelsPerUnit);
}
