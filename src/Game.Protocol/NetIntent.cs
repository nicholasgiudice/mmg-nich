using System.Numerics;
using Game.Core.Intents;
using Game.Core.Simulation;
using MemoryPack;

namespace Game.Protocol;

public enum NetIntentKind : byte
{
    Move,
    Stop,
    UseAbility,
}

/// <summary>
/// Intenção como vai pela rede. Não leva a entidade: o servidor usa sempre a entidade controlada pela conexão
/// (um cliente não pode mandar intenção por outra entidade).
/// </summary>
[MemoryPackable]
public sealed partial record NetIntent(NetIntentKind Kind, Vector2 Direction, string? AbilityId, EntityId? Target, Vector2? AimPoint)
{
    public static NetIntent From(Intent intent) => intent switch
    {
        MoveIntent m => new NetIntent(NetIntentKind.Move, m.Direction, null, null, null),
        StopIntent => new NetIntent(NetIntentKind.Stop, default, null, null, null),
        UseAbilityIntent u => new NetIntent(NetIntentKind.UseAbility, default, u.AbilityId, u.Target, u.AimPoint),
        _ => throw new NotSupportedException($"Intenção sem formato de rede: {intent.GetType().Name}"),
    };

    /// <summary>A intenção do Core para a entidade da conexão; null se a mensagem é inválida.</summary>
    public Intent? ToIntent(EntityId entity) => Kind switch
    {
        NetIntentKind.Move => float.IsFinite(Direction.X) && float.IsFinite(Direction.Y) ? new MoveIntent(entity, Direction) : null,
        NetIntentKind.Stop => new StopIntent(entity),
        NetIntentKind.UseAbility when !string.IsNullOrEmpty(AbilityId) => new UseAbilityIntent(entity, AbilityId, Target, AimPoint),
        _ => null,
    };
}
