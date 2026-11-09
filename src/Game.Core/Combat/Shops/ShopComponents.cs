using Game.Core.Simulation;

namespace Game.Core.Combat.Shops;

/// <summary>
/// NPC (T-321): entidade com <see cref="Simulation.Components.Transform"/> e este componente, sem vida, facção nem IA. Não é
/// alvo de habilidade nem de monstro (o alvo exige facção) e não colide. <see cref="Shop"/> = loja de <c>data/shops.json</c>
/// (null = NPC sem loja). Visível para todos.
/// </summary>
public sealed class NpcInfo : IComponent, IReplicated
{
    /// <summary>Id de definição (ex.: <c>npc.priest</c>).</summary>
    public required string DefinitionId { get; init; }

    public required string Name { get; init; }
    public string? Shop { get; init; }

    public IComponent Clone() => new NpcInfo { DefinitionId = DefinitionId, Name = Name, Shop = Shop };

    public void WriteHash(StateHasher hasher)
    {
        hasher.Add(DefinitionId);
        hasher.Add(Name);
        hasher.Add(Shop ?? "");
    }
}
