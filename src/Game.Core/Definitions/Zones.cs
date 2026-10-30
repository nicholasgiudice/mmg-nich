namespace Game.Core.Definitions;

/// <summary>Tipo de zona aberta (T-214).</summary>
public enum ZoneKind
{
    /// <summary>Cidade: sem monstros, regenera mais rápido; onde o personagem nasce e renasce.</summary>
    Safe,

    /// <summary>Área externa com monstros que renascem; nunca termina.</summary>
    Field,
}

/// <summary>
/// Zona do mundo aberto (T-214, <c>data/zones.json</c>): uma arena (mapa), onde se chega, portões e pontos de surgimento.
/// Cada zona roda num <c>World</c> próprio no servidor.
/// </summary>
public sealed record ZoneDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arena { get; init; }
    public ZoneKind Kind { get; init; } = ZoneKind.Field;

    /// <summary>Onde o personagem aparece ao entrar sem posição salva (e, na zona inicial, ao renascer).</summary>
    public required PositionDefinition Spawn { get; init; }

    /// <summary>Fração da vida e da Mana máximas recuperada por segundo (descanso; só zona segura). 0 = nada.</summary>
    public double RestPerSecond { get; init; }

    /// <summary>Quanto o corpo de um monstro fica no chão antes de sumir (segundos no JSON: <c>corpse</c>).</summary>
    public int CorpseTicks { get; init; } = 100;

    public IReadOnlyList<GateDefinition> Gates { get; init; } = [];
    public IReadOnlyList<MonsterSpawnDefinition> MonsterSpawns { get; init; } = [];
}

/// <summary>Retângulo em tiles (x, y = canto superior esquerdo).</summary>
public sealed record RectDefinition(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
}

/// <summary>Portão: entrar em <see cref="Area"/> leva à zona <see cref="To"/>, na posição <see cref="Arrival"/>.</summary>
public sealed record GateDefinition
{
    public required RectDefinition Area { get; init; }
    public required string To { get; init; }
    public required PositionDefinition Arrival { get; init; }
}

/// <summary>
/// Ponto de surgimento: <see cref="Count"/> monstros em volta de <see cref="Position"/> (até <see cref="Radius"/> tiles),
/// cada um renasce <see cref="RespawnTicks"/> depois de morrer (segundos no JSON: <c>respawn</c>).
/// </summary>
public sealed record MonsterSpawnDefinition
{
    /// <summary>Subárea da zona (nome para o jogador e para os testes de design, T-215); opcional.</summary>
    public string? Area { get; init; }

    public required string Monster { get; init; }
    public required PositionDefinition Position { get; init; }
    public int Count { get; init; } = 1;
    public double Radius { get; init; }

    /// <summary>Nível do monstro; null = o da definição.</summary>
    public int? Level { get; init; }

    public int RespawnTicks { get; init; } = 600;
}
