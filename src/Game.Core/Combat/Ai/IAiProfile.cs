using Game.Core.Simulation;

namespace Game.Core.Combat.Ai;

/// <summary>
/// Perfil de IA (comportamento). Dados: <c>MonsterDefinition.Ai.Id</c> + <c>Parameters</c>.
/// Um perfil NÃO guarda estado próprio (use <see cref="AiState"/>) e só produz intenções via <see cref="AiContext"/>.
/// Novo perfil: implemente esta interface (ou herde um existente) e registre em <see cref="AiProfileRegistry.CreateDefault"/>.
/// </summary>
public interface IAiProfile
{
    /// <summary>
    /// Permite ao perfil alterar o alvo escolhido pela tabela de ameaça (já considerando provocação:
    /// com provocação ativa este método NÃO é chamado). <paramref name="fromTable"/> nunca é nulo aqui.
    /// </summary>
    EntityId SelectTarget(AiContext ctx, EntityId fromTable) => fromTable;

    /// <summary>Decide e emite intenções para este tick. Chamado só com monstro vivo, engajado, com alvo válido e sem conjuração em andamento.</summary>
    void Think(AiContext ctx);
}
