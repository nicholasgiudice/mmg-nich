using Game.Auth;

namespace Game.Server.Persistence;

/// <summary>
/// Entrada com conta (T-212): valida o token de jogo da API (assinatura, servidor, expiração e uso único, sem rede) e
/// carrega/grava o personagem por <see cref="Persistence"/>.
/// </summary>
public sealed class AccountLogin(GameTicketReplayGuard tickets, CharacterPersistence persistence)
{
    public GameTicketReplayGuard Tickets { get; } = tickets;

    public CharacterPersistence Persistence { get; } = persistence;
}
