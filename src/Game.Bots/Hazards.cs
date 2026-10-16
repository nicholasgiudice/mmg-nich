using System.Numerics;
using Game.Core.Simulation;

namespace Game.Bots;

/// <summary>
/// Perigo no chão (telegráfo, área de dano) que um bot "que se move" tenta evitar.
/// A T-016 liga uma <see cref="IHazardSource"/> que lê os eventos de telegráfo da T-012; os bots só dependem desta interface.
/// </summary>
public abstract record Hazard(long ResolveTick)
{
    /// <summary>True se o ponto (com folga <paramref name="margin"/>, tipicamente o raio do corpo) está dentro da área.</summary>
    public abstract bool Contains(Vector2 point, float margin);
}

public sealed record CircleHazard(Vector2 Center, float Radius, long ResolveTick) : Hazard(ResolveTick)
{
    public override bool Contains(Vector2 point, float margin) =>
        Vector2.Distance(point, Center) <= Radius + margin;
}

/// <summary>Retângulo orientado: começa em <see cref="Origin"/> e segue <see cref="Direction"/> por <see cref="Length"/>.</summary>
public sealed record RectHazard(Vector2 Origin, Vector2 Direction, float Length, float Width, long ResolveTick) : Hazard(ResolveTick)
{
    public override bool Contains(Vector2 point, float margin)
    {
        var dir = Direction.LengthSquared() > 0 ? Vector2.Normalize(Direction) : new Vector2(0, 1);
        var rel = point - Origin;
        var along = Vector2.Dot(rel, dir);
        var side = MathF.Abs((rel.X * -dir.Y) + (rel.Y * dir.X));
        return along >= -margin && along <= Length + margin && side <= (Width / 2) + margin;
    }
}

/// <summary>Cone/arco: <see cref="AngleRadians"/> é a abertura total.</summary>
public sealed record ConeHazard(Vector2 Origin, Vector2 Direction, float AngleRadians, float Range, long ResolveTick) : Hazard(ResolveTick)
{
    public override bool Contains(Vector2 point, float margin)
    {
        var rel = point - Origin;
        var dist = rel.Length();
        if (dist <= margin)
        {
            return true;
        }

        if (dist > Range + margin)
        {
            return false;
        }

        var dir = Direction.LengthSquared() > 0 ? Vector2.Normalize(Direction) : new Vector2(0, 1);
        var cos = Vector2.Dot(rel / dist, dir);
        var slack = MathF.Asin(MathF.Min(1f, margin / dist));
        return MathF.Acos(Math.Clamp(cos, -1f, 1f)) <= (AngleRadians / 2) + slack;
    }
}

/// <summary>Fornece os perigos ativos no tick atual (já anunciados e ainda não resolvidos).</summary>
public interface IHazardSource
{
    void Collect(World world, List<Hazard> into);
}

public sealed class NoHazards : IHazardSource
{
    public static readonly NoHazards Instance = new();

    public void Collect(World world, List<Hazard> into)
    {
    }
}
