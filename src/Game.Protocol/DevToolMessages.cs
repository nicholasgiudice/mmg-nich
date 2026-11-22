using MemoryPack;

namespace Game.Protocol;

// Ferramenta de teste (T-405, design/segunda-classe-v0.1.md §4.2): o designer sobe de nível e desfaz a segunda classe pelo
// menu do Esc (T-411) enquanto não há áreas do nível 16 ao 40. Só funciona com o servidor em --dev-tools; o cliente sabe
// pelo Welcome.DevTools se mostra o botão. Fluxo:
//   cliente → DevToolRequest(ação)
//   servidor → DevToolResult(ação, resultado, nível)   (sempre, aceito ou recusado)
// O que mudou chega como sempre: eventos XpGained/LevelUp (subir de nível) ou PointsReset (redistribuir) e o componente
// CharacterProgress replicado (nível, pontos, segunda classe).

/// <summary>O que a ferramenta de teste faz (T-405).</summary>
public enum DevToolAction : byte
{
    /// <summary>Sobe 1 nível.</summary>
    LevelUp1,

    /// <summary>Sobe 5 níveis (até o máximo).</summary>
    LevelUp5,

    /// <summary>Vai ao nível da segunda classe (<c>progression.secondClassLevel</c>, hoje 20).</summary>
    ToSecondClassLevel,

    /// <summary>Vai ao nível máximo (<c>progression.maxLevel</c>, hoje 40).</summary>
    ToMaxLevel,

    /// <summary>
    /// Tira a segunda classe: devolve os pontos de habilidade gastos nela e os +5 de atributo da escolha (se já estavam
    /// distribuídos, devolve todos os pontos de atributo). Depois dá para escolher outra.
    /// </summary>
    UndoSecondClass,

    /// <summary>Redistribui os pontos de atributo e de habilidade, sem custo (o mesmo que a redistribuição normal).</summary>
    FreeReset,
}

/// <summary>Resultado de um <see cref="DevToolRequest"/>.</summary>
public enum DevToolOutcome : byte
{
    /// <summary>Feito; o personagem foi gravado (se tem conta).</summary>
    Done,

    /// <summary>Servidor sem <c>--dev-tools</c>: nada muda (o pedido fica no log do servidor).</summary>
    Disabled,

    /// <summary>Em combate ou conjurando (como a redistribuição).</summary>
    InCombat,

    /// <summary>Personagem morto.</summary>
    Dead,

    /// <summary>Personagem sem progressão (ex.: sala de encontro com login de desenvolvimento).</summary>
    NoProgression,

    /// <summary>Já está no nível pedido, acima dele ou no máximo: não sobe nada.</summary>
    AlreadyAtLevel,

    /// <summary>Desfazer a segunda classe sem ter escolhido uma.</summary>
    NoSecondClass,
}

/// <summary>Pedido da ferramenta de teste (T-405). A entidade é sempre a da conexão.</summary>
[MemoryPackable]
public sealed partial record DevToolRequest(DevToolAction Action) : INetMessage;

/// <summary>
/// Resposta a um <see cref="DevToolRequest"/>: a <paramref name="Action"/> pedida, o <paramref name="Outcome"/> e o
/// <paramref name="Level"/> do personagem depois do pedido (0 se não tem progressão).
/// </summary>
[MemoryPackable]
public sealed partial record DevToolResult(DevToolAction Action, DevToolOutcome Outcome, int Level) : INetMessage;
