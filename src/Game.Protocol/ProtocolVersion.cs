namespace Game.Protocol;

/// <summary>
/// Versão do protocolo de rede (T-103).
///
/// <para><b>Regra de compatibilidade:</b> cliente e servidor só conversam com a <b>mesma</b> versão e o mesmo
/// <c>ContentFingerprint</c> (impressão digital de <c>data/</c>, T-101). O servidor recusa o <see cref="Hello"/> com
/// <see cref="RejectReason.ProtocolVersion"/> ou <see cref="RejectReason.Content"/>. Não há negociação nem modo
/// compatível: na fase 1 cliente e servidor saem do mesmo commit.</para>
///
/// <para><b>Quando subir a versão</b> (qualquer mudança nos bytes): mensagem nova ou removida; campo novo, removido ou
/// com tipo/ordem trocados numa mensagem; tag nova em <see cref="ComponentCodecs"/> ou <see cref="EventCodecs"/>;
/// campo de um componente replicável ou de um evento mudou. As tags existentes nunca são reaproveitadas.
/// Mudar só números de <c>data/</c> não sobe a versão (o fingerprint já pega).</para>
/// </summary>
public static class ProtocolVersion
{
    /// <summary>
    /// 2 (T-106): <c>SnapshotDelta</c>, <c>SnapshotAck</c> e época no <c>Welcome</c>.
    /// 3 (T-201): componente <c>CharacterStats</c>, defesa no <c>UnitInfo</c> e crítico nos eventos de dano, cura e escudo.
    /// 4 (T-203): componente <c>CharacterProgress</c>, nível no <c>UnitInfo</c>, eventos de XP, nível e pontos e intenções
    /// de pontos no <c>NetIntent</c>.
    /// 5 (T-204): componentes <c>Inventory</c>, <c>Equipment</c> e <c>GroundItem</c>, bônus de vida, crítico e Mana/s no
    /// <c>CharacterStats</c>, eventos de itens e intenções de equipar, desequipar, pegar e largar (campo <c>Slot</c>).
    /// 6 (T-212): token de jogo no <c>Hello</c> e motivos de recusa <c>Unauthorized</c>, <c>AlreadyConnected</c> e
    /// <c>Unavailable</c>.
    /// 7 (T-214): mensagem <c>RespawnRequest</c>; no mundo aberto o <c>Welcome</c> leva o id da zona e se repete a cada troca.
    /// 8 (T-310): componente <c>PartyMembership</c> e mensagens de grupo (<c>PartyInvite</c>, <c>PartyRespond</c>,
    /// <c>PartyLeave</c>, <c>PartyKick</c>, <c>PartyInviteReceived</c>, <c>PartyState</c>, <c>PartyNotice</c>,
    /// <c>PartyRejected</c>).
    /// 9 (T-320): componente <c>Wallet</c> (só o dono), quantidade (<c>Count</c>) nos itens, intenção <c>UseItem</c>, eventos
    /// <c>ItemUsed</c> e <c>GoldGained</c>, motivos novos em <c>ItemRejectReason</c>; saem os consumíveis e as cargas do
    /// <c>AbilityState</c>, as cargas restantes do <c>AbilityExecuted</c> e o motivo <c>NoCharges</c>.
    /// </summary>
    public const int Current = 9;
}
