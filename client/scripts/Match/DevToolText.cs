using Game.Protocol;

namespace MmoRpgClient.Match;

/// <summary>
/// Textos da ferramenta de teste do menu do Esc (T-411, servidor da T-405): o rótulo de cada pedido e a resposta do
/// servidor como frase para o jogador. Sem Godot, para os testes do cliente.
/// </summary>
public static class DevToolText
{
    /// <summary>Os pedidos, na ordem dos botões do menu.</summary>
    public static IReadOnlyList<DevToolAction> Actions { get; } =
    [
        DevToolAction.LevelUp1,
        DevToolAction.LevelUp5,
        DevToolAction.ToSecondClassLevel,
        DevToolAction.ToMaxLevel,
        DevToolAction.UndoSecondClass,
        DevToolAction.FreeReset,
    ];

    /// <summary>A entrada "Teste" do menu só existe com o servidor em <c>--dev-tools</c> (avisado no <see cref="Welcome"/>).</summary>
    public static bool Enabled(Welcome? welcome) => welcome?.DevTools ?? false;

    /// <summary>Pedidos que mexem numa escolha definitiva e pedem confirmação antes de sair.</summary>
    public static bool NeedsConfirmation(DevToolAction action) => action == DevToolAction.UndoSecondClass;

    public static string Label(DevToolAction action) => action switch
    {
        DevToolAction.LevelUp1 => "+1 nível",
        DevToolAction.LevelUp5 => "+5 níveis",
        DevToolAction.ToSecondClassLevel => "Ir ao nível 20",
        DevToolAction.ToMaxLevel => "Ir ao nível 40",
        DevToolAction.UndoSecondClass => "Desfazer a segunda classe",
        DevToolAction.FreeReset => "Redistribuir pontos (grátis)",
        _ => action.ToString(),
    };

    public const string ConfirmUndo =
        "Desfazer a segunda classe?\nAs habilidades dela saem e os pontos voltam.\nDepois dá para escolher outra.";

    /// <summary>A resposta do servidor; <c>Ok</c> diz se foi feito (cor de sucesso) ou recusado (cor de aviso).</summary>
    public static (string Text, bool Ok) Result(DevToolResult result) => result.Outcome switch
    {
        DevToolOutcome.Done => (result.Action switch
        {
            DevToolAction.UndoSecondClass => "Segunda classe desfeita. Os pontos voltaram.",
            DevToolAction.FreeReset => "Pontos redistribuídos.",
            _ => $"Agora no nível {result.Level}.",
        }, true),
        DevToolOutcome.Disabled => ("A ferramenta de teste está desligada neste servidor.", false),
        DevToolOutcome.InCombat => ("Não dá em combate.", false),
        DevToolOutcome.Dead => ("Não dá morto.", false),
        DevToolOutcome.NoProgression => ("Este personagem não tem progressão.", false),
        DevToolOutcome.AlreadyAtLevel => ($"Já está no nível {result.Level}.", false),
        DevToolOutcome.NoSecondClass => ("Ainda não há segunda classe para desfazer.", false),
        _ => ("O servidor recusou o pedido.", false),
    };
}
