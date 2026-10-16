using Game.Core.Events;
using Game.Core.Intents;
using Game.Core.Simulation;

namespace Game.Core.Authority;

/// <summary>
/// Quem decide o estado do jogo, visto pelo cliente. No protótipo é a simulação local
/// (<c>LocalAuthority</c> no cliente); na fase 1 será o servidor. A apresentação só conhece
/// esta interface: envia intenções e lê estado e eventos, nunca aplica efeitos.
/// </summary>
public interface IGameAuthority
{
    /// <summary>Entidade controlada pelo jogador local; null enquanto não houver uma.</summary>
    EntityId? ControlledEntity { get; }

    /// <summary>Tick do snapshot mais recente (<see cref="Latest"/>).</summary>
    long CurrentTick { get; }

    /// <summary>
    /// Fração (0..1) do caminho entre <see cref="Previous"/> e <see cref="Latest"/> que a
    /// apresentação deve desenhar neste frame.
    /// </summary>
    float InterpolationFraction { get; }

    /// <summary>Estado no fim do último tick; null antes do primeiro.</summary>
    WorldSnapshot? Latest { get; }

    /// <summary>Estado no fim do tick anterior ao <see cref="Latest"/>; null se ainda não existir.</summary>
    WorldSnapshot? Previous { get; }

    /// <summary>Avança a autoridade pelo tempo real decorrido (segundos) desde a chamada anterior.</summary>
    void Advance(double deltaSeconds);

    /// <summary>Envia uma intenção da entidade controlada. Aplicada no próximo tick, se válida.</summary>
    void Send(Intent intent);

    /// <summary>Eventos chegados desde a chamada anterior, em ordem de tick; cada evento é entregue uma vez.</summary>
    IReadOnlyList<GameEvent> TakeEvents();
}
