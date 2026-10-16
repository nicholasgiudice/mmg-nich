namespace Game.Core.Simulation;

/// <summary>
/// Sistema: lógica executada uma vez por tick, na ordem definida em <see cref="SystemOrder"/>.
/// Sistemas não guardam estado de jogo próprio (use componentes) para manter hash/snapshot completos.
/// </summary>
public interface ISimulationSystem
{
    void Update(World world);
}
