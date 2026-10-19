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

/// <summary>
/// Marca um componente que o cliente precisa ver (vai nos snapshots replicados e, na fase 1, pela rede).
/// Componentes sem esta marca são estado interno do servidor (IA, ameaça, contabilidade de recurso...).
/// </summary>
public interface IReplicated
{
}

/// <summary>O que um snapshot inclui.</summary>
public enum SnapshotScope
{
    /// <summary>Todos os componentes (testes, depuração, hash).</summary>
    Full,

    /// <summary>Só componentes <see cref="IReplicated"/> (cliente local e servidor).</summary>
    Replicated,
}
