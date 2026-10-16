namespace Game.Core.Simulation;

/// <summary>
/// ÚNICO lugar onde os sistemas do jogo são registrados. Cada tarefa adiciona UMA linha em
/// <see cref="Register"/> (ou troca a implementação de uma linha existente).
/// </summary>
public static class SystemsRegistry
{
    public static void Register(WorldBuilder b)
    {
        b.AddSystem(SystemOrder.Movement, new Systems.MovementSystem());
        b.AddSystem(SystemOrder.Death, new Systems.DeathSystem());
    }
}
