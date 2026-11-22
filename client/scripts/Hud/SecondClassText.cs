using Game.Core.Combat.Progression;

namespace MmoRpgClient.Hud;

/// <summary>
/// Textos da segunda classe na tela de habilidades (T-410): o papel de cada uma (de <c>design/segunda-classe-v0.1.md</c>),
/// a confirmação da escolha definitiva e as recusas do servidor. Sem Godot, para os testes do cliente.
/// </summary>
public static class SecondClassText
{
    private static readonly Dictionary<string, string> Roles = new(StringComparer.Ordinal)
    {
        ["second_class.guardian"] = "Proteger o grupo e segurar chefes.",
        ["second_class.berserker"] = "Dano corpo a corpo alto, defesa baixa.",
        ["second_class.marksman"] = "Alvo único, de longe, rajadas e críticos.",
        ["second_class.hunter"] = "Venenos e sangramento em vários alvos.",
        ["second_class.pyromancer"] = "Dano em área e queimadura.",
        ["second_class.arcanist"] = "Dano constante em alvo único.",
        ["second_class.priest"] = "Cura do grupo.",
        ["second_class.inquisitor"] = "Dano sagrado corpo a corpo, com cura menor.",
    };

    /// <summary>O papel da segunda classe; vazio se ela não tem texto ainda.</summary>
    public static string Role(string secondClassId) => Roles.GetValueOrDefault(secondClassId, "");

    /// <summary>Antes do nível da segunda classe, o que a parte de baixo da tela mostra.</summary>
    public static string Locked(int secondClassLevel) => $"A segunda classe libera no nível {secondClassLevel}.";

    public static string Confirm(string name) =>
        $"Escolher {name} como segunda classe?\nA escolha é definitiva: não pode ser desfeita nem trocada depois.";

    /// <summary>Id de uma recusa que veio de um pedido de segunda classe (o <c>ProgressionRejected.Id</c> é a segunda classe).</summary>
    public static bool IsSecondClassRequest(string id) => id.StartsWith("second_class.", StringComparison.Ordinal);

    /// <summary>A recusa da escolha, como frase para o jogador.</summary>
    public static string Rejection(ProgressionRejectReason reason) => reason switch
    {
        ProgressionRejectReason.InCombat => "Só dá para escolher a segunda classe fora de combate.",
        ProgressionRejectReason.LevelTooLow => "A segunda classe só libera no nível 20.",
        ProgressionRejectReason.SecondClassAlreadyChosen => "A segunda classe já foi escolhida.",
        ProgressionRejectReason.NotASecondClass => "Essa não é uma segunda classe da sua classe.",
        ProgressionRejectReason.NoProgression => "Este personagem não tem progressão (encontro de teste).",
        _ => "O servidor recusou a escolha.",
    };
}
