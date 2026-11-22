namespace MmoRpgClient.Account;

/// <summary>
/// Texto do contador de jogadores online da tela de login (<c>GET /status</c> da API). Sem Godot, para os testes do cliente.
/// null = ainda não consultou; negativo = a consulta falhou (servidor fora do ar ou contador indisponível).
/// </summary>
public static class OnlineCounterText
{
    /// <summary>Intervalo entre consultas enquanto a tela de login está aberta; a API guarda a contagem por 5 s.</summary>
    public const double RefreshSeconds = 30;

    public const int Unavailable = -1;

    public static string Describe(int? online) => online switch
    {
        null => "Consultando jogadores online...",
        < 0 => "Contador de jogadores indisponível",
        0 => "Nenhum jogador online agora",
        1 => "1 jogador online",
        _ => $"{online} jogadores online",
    };

    /// <summary>Com gente online o texto fica verde (como a bolinha de "online"); senão, apagado.</summary>
    public static bool IsLive(int? online) => online > 0;
}
