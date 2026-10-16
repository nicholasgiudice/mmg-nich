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
        b.AddSystem(SystemOrder.Abilities, new Combat.AbilitySystem());
        b.AddSystem(SystemOrder.Telegraphs, new Combat.Telegraphs.TelegraphSystem()); // T-012
        b.AddSystem(SystemOrder.Areas, new Combat.Areas.AreaSystem()); // T-012
        b.AddSystem(SystemOrder.Phases, new Combat.Phases.PhaseSystem()); // T-012
        b.AddSystem(SystemOrder.Status, new Combat.Status.StatusSystem());
        b.AddSystem(SystemOrder.StatusResourceBridge, new Combat.Integration.StatusRemovalBridgeSystem());
        b.AddSystem(SystemOrder.Resources, new Combat.Resources.ResourceSystem());
        b.AddSystem(SystemOrder.Ai, new Combat.Ai.AiSystem()); // T-011
        b.AddSystem(SystemOrder.Death, new Systems.DeathSystem());
    }
}
