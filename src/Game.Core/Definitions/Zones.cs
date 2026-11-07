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
/// <para>T-331: o que é posição vem do mapa do Tiled da zona e é montado pelo carregador (<c>Game.Data</c>):
/// <see cref="Spawn"/>, <see cref="Gates"/>, <see cref="MonsterSpawns"/> e <see cref="Npcs"/>. O <c>zones.json</c> só tem o
/// resto (id, nome, tipo, arena, descanso, corpo, faixa de nível) e recusa esses campos.</para>
/// </summary>
public sealed record ZoneDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arena { get; init; }
    public ZoneKind Kind { get; init; } = ZoneKind.Field;

    /// <summary>
    /// Onde o personagem aparece ao entrar sem posição salva (e, na zona inicial, ao renascer). Do mapa: o
    /// <c>player_spawn</c> chamado <c>spawn</c>.
    /// </summary>
    public PositionDefinition Spawn { get; init; } = new(0, 0);

    /// <summary>Fração da vida e da Mana máximas recuperada por segundo (descanso; só zona segura). 0 = nada.</summary>
    public double RestPerSecond { get; init; }

    /// <summary>Quanto o corpo de um monstro fica no chão antes de sumir (segundos no JSON: <c>corpse</c>).</summary>
    public int CorpseTicks { get; init; } = 100;

    /// <summary>Faixa de nível mostrada ao jogador (ex.: 1–4); null = sem faixa (Cidade). JSON: <c>levels</c>.</summary>
    public LevelRangeDefinition? Levels { get; init; }

    /// <summary>Do mapa: objetos da camada <c>transitions</c>, na ordem do arquivo.</summary>
    public IReadOnlyList<GateDefinition> Gates { get; init; } = [];

    /// <summary>
    /// Do mapa: objetos <c>monster_spawn</c> da camada <c>spawns</c>, na ordem do arquivo (o índice é o grupo de aggro,
    /// T-301).
    /// </summary>
    public IReadOnlyList<MonsterSpawnDefinition> MonsterSpawns { get; init; } = [];

    /// <summary>Do mapa: objetos de classe <c>npc</c> da camada <c>areas</c> (T-331; a lógica de NPC é da T-321).</summary>
    public IReadOnlyList<NpcPlacementDefinition> Npcs { get; init; } = [];
}

/// <summary>Faixa de nível de uma zona, só para mostrar (T-331).</summary>
public sealed record LevelRangeDefinition(int Min, int Max);

/// <summary>
/// NPC posto no mapa (T-331): objeto de classe <c>npc</c> com a propriedade <c>npc</c> (id de definição) e, opcionalmente,
/// <c>shop</c>. <see cref="Position"/> é o centro do retângulo; <see cref="Properties"/> guarda as demais propriedades
/// (ex.: <c>name</c>, <c>building</c>) como texto.
/// </summary>
public sealed record NpcPlacementDefinition
{
    /// <summary>Nome do objeto no mapa.</summary>
    public required string Name { get; init; }

    public required string Npc { get; init; }
    public string? Shop { get; init; }
    public required PositionDefinition Position { get; init; }
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
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
