namespace MmoRpgClient.Match;

/// <summary>Escolhas do menu para a próxima partida (sobrevive à troca de cena).</summary>
public sealed record MatchSettings(string EncounterId, string? ClassId, ulong Seed)
{
    /// <summary>Partida atual/próxima; null = nada escolhido (o jogo usa os argumentos ou o padrão).</summary>
    public static MatchSettings? Current { get; set; }

    /// <summary>Seed aleatória para quando o jogador não escolhe uma (o cliente não é Core; a simulação recebe a seed pronta).</summary>
    public static ulong RandomSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);
}
