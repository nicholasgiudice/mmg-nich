using Game.Core.Definitions;

namespace Game.Core.Spatial;

/// <summary>Tipo de ponto de surgimento de um mapa do Tiled (classe do objeto na camada <c>spawns</c>).</summary>
public enum MapSpawnKind
{
    /// <summary><c>player_spawn</c>: onde um jogador aparece.</summary>
    Player,

    /// <summary><c>monster_spawn</c>: onde um monstro aparece (propriedade <c>monster</c>).</summary>
    Monster,
}

/// <summary>Ponto de surgimento lido da camada <c>spawns</c> (T-051). Posição em tiles.</summary>
public sealed record MapSpawn
{
    public required MapSpawnKind Kind { get; init; }
    public required string Name { get; init; }
    public required PositionDefinition Position { get; init; }

    /// <summary>Id do monstro (só <see cref="MapSpawnKind.Monster"/>).</summary>
    public string? Monster { get; init; }

    /// <summary>Nível do monstro; null = o da definição.</summary>
    public int? Level { get; init; }
}

/// <summary>
/// Área de evento lida da camada <c>areas</c> (T-051): altar, selo, teia... <see cref="Kind"/> é a classe do objeto no
/// Tiled; as propriedades do objeto ficam em <see cref="Properties"/> (como texto) para quem usar a área.
/// </summary>
public sealed record MapArea
{
    public required string Name { get; init; }
    public required string Kind { get; init; }
    public required RectDefinition Area { get; init; }
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Transição de zona lida da camada <c>transitions</c> (T-051): entrar em <see cref="Area"/> leva à zona <see cref="To"/>,
/// no ponto <c>player_spawn</c> chamado <see cref="Arrival"/> do mapa de destino (null = o nascimento da zona).
/// </summary>
public sealed record MapTransition
{
    public required string Name { get; init; }
    public required RectDefinition Area { get; init; }
    public required string To { get; init; }
    public string? Arrival { get; init; }
}
