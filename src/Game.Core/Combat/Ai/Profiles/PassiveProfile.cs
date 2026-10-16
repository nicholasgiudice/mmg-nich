namespace Game.Core.Combat.Ai.Profiles;

/// <summary>Perfil <c>ai.none</c>: acumula ameaça mas nunca age (alvos de treino).</summary>
public sealed class PassiveProfile : IAiProfile
{
    public void Think(AiContext ctx)
    {
    }
}
