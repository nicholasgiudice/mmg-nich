namespace Game.Server;

/// <summary>O que o loop e o console do <c>Program</c> usam: sala de encontro (<see cref="ZoneRoom"/>) ou mundo aberto (T-214).</summary>
public interface IGameHost
{
    int PlayerCount { get; }

    /// <summary>Um tick de 20 Hz.</summary>
    void Step();

    /// <summary>Comando <c>restart</c> do console.</summary>
    void Restart(string reason);

    /// <summary>Linha do comando <c>status</c>.</summary>
    string Status();

    /// <summary>Grava os personagens com conta conectados (parada do servidor).</summary>
    void SaveAll(string reason);
}
