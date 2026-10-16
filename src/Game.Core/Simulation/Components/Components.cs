using System.Numerics;

namespace Game.Core.Simulation.Components;

public enum Faction
{
    Player = 0,
    Monster = 1,
}

/// <summary>Posição (unidades = tiles) e direção para onde a entidade olha.</summary>
public sealed class Transform : IComponent
{
    public Vector2 Position;
    public Vector2 Facing = new(0, 1);

    public IComponent Clone() => new Transform { Position = Position, Facing = Facing };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Position);
        hasher.Add(Facing);
    }
}

public sealed class Health : IComponent
{
    public float Current;
    public float Max;

    public Health(float max)
    {
        Max = max;
        Current = max;
    }

    /// <summary>Alterado somente por <see cref="World.Kill"/>.</summary>
    public bool IsAlive { get; internal set; } = true;

    public IComponent Clone() => new Health(Max) { Current = Current, IsAlive = IsAlive };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Current);
        hasher.Add(Max);
        hasher.Add(IsAlive);
    }
}

public sealed class FactionTag(Faction faction) : IComponent
{
    public Faction Faction { get; } = faction;

    public IComponent Clone() => new FactionTag(Faction);

    public void WriteHash(StateHasher hasher) => hasher.Add((int)Faction);
}

/// <summary>Recurso de classe (vigor, fe, foco...). O tipo é um id de dados, ex.: "resource.vigor".</summary>
public sealed class ResourcePool(string type, float max, float current) : IComponent
{
    public string Type { get; } = type;
    public float Max = max;
    public float Current = current;

    public IComponent Clone() => new ResourcePool(Type, Max, Current);

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Type);
        hasher.Add(Max);
        hasher.Add(Current);
    }
}

/// <summary>Velocidade (tiles/s) e direção de movimento desejada (comprimento 0..1; zero = parado).</summary>
public sealed class Movement(float speed) : IComponent
{
    public float Speed = speed;
    public Vector2 Direction;

    /// <summary>Quando true o movimento não altera <see cref="Transform.Facing"/> (ex.: mirando, strafe).</summary>
    public bool LockFacing;

    public IComponent Clone() => new Movement(Speed) { Direction = Direction, LockFacing = LockFacing };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(Speed);
        hasher.Add(Direction);
        hasher.Add(LockFacing);
    }
}

/// <summary>Círculo de colisão (raio em tiles). Usado pela T-007.</summary>
public sealed class Collider(float radius) : IComponent
{
    public float Radius = radius;

    public IComponent Clone() => new Collider(Radius);

    public void WriteHash(StateHasher hasher) => hasher.Add(Radius);
}
