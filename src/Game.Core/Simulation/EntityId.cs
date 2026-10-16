namespace Game.Core.Simulation;

/// <summary>Identificador estável de entidade. Ids são sequenciais (a partir de 1) e nunca reutilizados.</summary>
public readonly record struct EntityId(int Value) : IComparable<EntityId>
{
    public static readonly EntityId None = new(0);

    public bool IsNone => Value == 0;

    public int CompareTo(EntityId other) => Value.CompareTo(other.Value);

    public override string ToString() => $"#{Value}";
}
