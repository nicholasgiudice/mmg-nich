namespace MmoRpgClient.Match;

/// <summary>Servidor para jogar online (T-107): endereço e nome de desenvolvimento.</summary>
public sealed record OnlineSettings(string Host, int Port, string Name)
{
    /// <summary>Lê <c>host:porta</c> (porta padrão 7777). Null se o texto não serve.</summary>
    public static OnlineSettings? Parse(string address, string name)
    {
        var text = address.Trim();
        if (text.Length == 0)
        {
            return null;
        }

        var colon = text.LastIndexOf(':');
        var host = colon > 0 ? text[..colon] : text;
        var port = 7777;
        if (colon > 0 && !int.TryParse(text[(colon + 1)..], out port))
        {
            return null;
        }

        return new OnlineSettings(host, port, string.IsNullOrWhiteSpace(name) ? "jogador" : name.Trim());
    }
}

/// <summary>Escolhas do menu para a próxima partida (sobrevive à troca de cena). <c>Online</c> != null = jogar no servidor (T-107).</summary>
public sealed record MatchSettings(string EncounterId, string? ClassId, ulong Seed, OnlineSettings? Online = null)
{
    /// <summary>Partida atual/próxima; null = nada escolhido (o jogo usa os argumentos ou o padrão).</summary>
    public static MatchSettings? Current { get; set; }

    /// <summary>Seed aleatória para quando o jogador não escolhe uma (o cliente não é Core; a simulação recebe a seed pronta).</summary>
    public static ulong RandomSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);
}
