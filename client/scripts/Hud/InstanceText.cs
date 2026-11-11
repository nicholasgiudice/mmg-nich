using Game.Core.Simulation;
using Game.Protocol;

namespace MmoRpgClient.Hud;

/// <summary>Textos dos avisos de instância (T-350) para o jogador (T-345).</summary>
public static class InstanceText
{
    public static string Notice(InstanceNoticeKind kind, int ticks) => kind switch
    {
        InstanceNoticeKind.LeavingSoon => $"Você não é mais do grupo desta masmorra: sairá em {Math.Ceiling(ticks / (double)World.TickRate):0} s.",
        InstanceNoticeKind.StayCancelled => "Você voltou ao grupo: continua na masmorra.",
        InstanceNoticeKind.Removed => "Você foi levado para fora da masmorra.",
        _ => "Aviso da masmorra.",
    };

    public static string Rejection(InstanceRejectReason reason) => reason switch
    {
        InstanceRejectReason.InstanceFull => "A masmorra do seu grupo está cheia.",
        InstanceRejectReason.TooManyInstances => "Masmorras lotadas no servidor. Tente de novo em instantes.",
        _ => "Entrada na masmorra recusada.",
    };
}
