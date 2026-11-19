using MemoryPack;

namespace Game.Protocol;

// Saída do mundo para trocar de personagem (T-395). Só no mundo aberto. Fluxo:
//   cliente → LogoutRequest
//   servidor → LogoutStatus(Waiting, tick final)   (só em combate; o personagem continua no mundo, vulnerável)
//   cliente → LogoutCancel, ou se move / usa habilidade → servidor → LogoutStatus(Cancelled)
//   servidor → LogoutStatus(Done)                  (personagem fora do mundo e gravado; a conexão fecha logo depois)

/// <summary>
/// O jogador quer sair do mundo (trocar de personagem). Fora de combate, ou morto, sai na hora; em combate espera
/// <c>rules.logout.combatDelay</c> e recebe <see cref="LogoutStatus"/> com <see cref="LogoutState.Waiting"/>. Repetido
/// durante a espera, o servidor só reenvia o <see cref="LogoutState.Waiting"/> com o mesmo tick.
/// </summary>
[MemoryPackable]
public sealed partial record LogoutRequest : INetMessage;

/// <summary>Desiste da saída pedida. O servidor responde <see cref="LogoutState.Cancelled"/> se havia uma saída em espera.</summary>
[MemoryPackable]
public sealed partial record LogoutCancel : INetMessage;

public enum LogoutState : byte
{
    /// <summary>Em combate: o personagem sai no tick <see cref="LogoutStatus.Tick"/> (tick da zona, o mesmo dos snapshots).</summary>
    Waiting,

    /// <summary>A espera acabou sem sair: o jogador se moveu, usou habilidade ou ataque, ou mandou <see cref="LogoutCancel"/>.</summary>
    Cancelled,

    /// <summary>
    /// O personagem saiu do mundo e a gravação terminou (ou falhou, com o erro no log do servidor: o cliente nunca fica
    /// preso). A lista de personagens da API já está atualizada. O servidor fecha a conexão logo depois.
    /// </summary>
    Done,
}

/// <summary>
/// Andamento da saída pedida com <see cref="LogoutRequest"/>. <paramref name="Tick"/>: em <see cref="LogoutState.Waiting"/>,
/// o tick da zona em que a saída acontece (20 por segundo; o que falta = <c>Tick</c> − tick atual do snapshot); nos
/// outros estados, 0.
/// </summary>
[MemoryPackable]
public sealed partial record LogoutStatus(LogoutState State, long Tick) : INetMessage;
