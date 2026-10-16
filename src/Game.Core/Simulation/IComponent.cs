namespace Game.Core.Simulation;

/// <summary>
/// Dado de uma entidade. Componentes são classes simples (apenas estado, sem lógica de regra).
/// Para criar um novo: implemente esta interface e adicione-o com <c>entity.Add(...)</c> ou <c>world.Spawn(...)</c>.
/// </summary>
public interface IComponent
{
    /// <summary>Cópia profunda (usada pelos snapshots).</summary>
    IComponent Clone();

    /// <summary>Escreve TODO o estado que influencia a simulação no hasher (ordem fixa).</summary>
    void WriteHash(StateHasher hasher);
}
