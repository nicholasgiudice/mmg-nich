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
    public const int Current = 1;
}
