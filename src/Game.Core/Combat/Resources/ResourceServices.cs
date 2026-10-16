using Game.Core.Simulation;

namespace Game.Core.Combat.Resources;

/// <summary>
/// Consulta de status usada pelas condições das regras (<c>targetHasStatusTag</c>, <c>sourceHasStatusTag</c>).
/// A T-009 (ou o integrador) fornece a implementação real; o padrão sempre responde false.
/// </summary>
public interface IStatusQuery
{
    bool HasStatusWithTag(World world, Entity entity, string tag);
}

/// <summary>Implementação padrão: ninguém tem status.</summary>
public sealed class NoStatusQuery : IStatusQuery
{
    public static readonly NoStatusQuery Instance = new();

    public bool HasStatusWithTag(World world, Entity entity, string tag) => false;
}

/// <summary>Remoção de status que a T-009 informa ao sistema de recursos (gatilho <c>status_removed</c>).</summary>
public sealed record StatusRemovalNotice(
    EntityId Remover, EntityId Holder, string StatusId, IReadOnlyList<string> StatusTags, string SourceId);

/// <summary>
/// Pontos de ligação do módulo de recursos com outros módulos, por mundo (ligados ao <see cref="CombatServices"/>).
/// Obtenha com <see cref="ResourceModule.For"/>; funciona em qualquer ordem de registro.
/// </summary>
public sealed class ResourceServices
{
    private readonly List<StatusRemovalNotice> _removals = new();

    /// <summary>Troque pela implementação da T-009. Padrão: <see cref="NoStatusQuery"/>.</summary>
    public IStatusQuery StatusQuery { get; set; } = NoStatusQuery.Instance;

    /// <summary>
    /// Informe uma remoção de status no tick atual (a T-009 chama ao remover status, ou uma ponte lê o evento StatusRemoved).
    /// Consumido pelo sistema de recursos no mesmo tick (ordem 4000, depois do sistema de status).
    /// </summary>
    public void NotifyStatusRemoved(StatusRemovalNotice notice) => _removals.Add(notice);

    internal List<StatusRemovalNotice> TakeRemovals()
    {
        var copy = _removals.ToList();
        _removals.Clear();
        return copy;
    }
}
