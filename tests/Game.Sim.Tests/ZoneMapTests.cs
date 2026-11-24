using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Data;

namespace Game.Sim.Tests;

/// <summary>
/// T-055: Cidade e Campos são mapas do Tiled. Do nascimento de cada zona, um personagem chega andando (células livres,
/// 4 vizinhos) a todos os pontos de surgimento, chegadas e portões; e os portões da zona batem com as transições do mapa.
/// Desde a T-331 a zona inteira (nascimento, portões, surgimentos) é montada a partir do mapa. T-343: Ruínas da Estrada Antiga. T-352: Cripta do Vigia. T-342: Floresta Baixa. T-530: Brejo dos Afogados. T-531: Forte do Passo.
/// </summary>
public class ZoneMapTests
{
    private static readonly Lazy<GameDatabase> Db = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir!.FullName, "MmoRpg.sln")))
        {
            dir = dir.Parent;
        }

        return GameDataLoader.LoadDirectory(Path.Combine(dir.FullName, "data")).GetDatabaseOrThrow();
    });

    public static TheoryData<string> Zones() => new() { "zone.city", "zone.vigil_fields", "zone.matriarch_lair", "zone.old_road_ruins", "zone.watcher_crypt", "zone.lower_forest", "zone.drowned_marsh", "zone.pass_fort" };

    [Theory]
    [MemberData(nameof(Zones))]
    public void Zone_uses_a_tiled_map(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        Assert.NotNull(Db.Value.Arenas[zone.Arena].Source);
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Every_spawn_and_gate_is_reachable_on_foot_from_the_zone_spawn(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        var reached = Reached(zone);

        foreach (var s in zone.MonsterSpawns)
        {
            Assert.True(reached.Contains(((int)s.Position.X, (int)s.Position.Y)), $"{zoneId}: {s.Monster} em ({s.Position.X}, {s.Position.Y}) inalcançável");
        }

        foreach (var g in zone.Gates)
        {
            var cells = Enumerable.Range((int)g.Area.X, (int)g.Area.Width)
                .SelectMany(x => Enumerable.Range((int)g.Area.Y, (int)g.Area.Height).Select(y => (x, y)));
            Assert.True(cells.Any(reached.Contains), $"{zoneId}: portão para {g.To} inalcançável");
        }
    }

    /// <summary>T-343: a entrada da Cripta (área <c>crypt_entrance</c> das Ruínas) é alcançável a pé a partir do nascimento.</summary>
    [Fact]
    public void Crypt_entrance_in_the_ruins_is_reachable_on_foot()
    {
        var zone = Db.Value.Zones["zone.old_road_ruins"];
        var entrance = Assert.Single(Db.Value.Arenas[zone.Arena].Areas, a => a.Kind == "crypt_entrance");
        var reached = Reached(zone);
        var cells = Enumerable.Range((int)entrance.Area.X, (int)entrance.Area.Width)
            .SelectMany(x => Enumerable.Range((int)entrance.Area.Y, (int)entrance.Area.Height).Select(y => (x, y)));
        Assert.Contains(cells, reached.Contains);
    }

    /// <summary>T-352: o portão para a Cripta fica dentro da área <c>crypt_entrance</c> das Ruínas.</summary>
    [Fact]
    public void Crypt_gate_is_at_the_crypt_entrance_of_the_ruins()
    {
        var ruins = Db.Value.Zones["zone.old_road_ruins"];
        var entrance = Assert.Single(Db.Value.Arenas[ruins.Arena].Areas, a => a.Kind == "crypt_entrance").Area;
        var gate = Assert.Single(ruins.Gates, g => g.To == "zone.watcher_crypt").Area;
        Assert.True(gate.X >= entrance.X && gate.Y >= entrance.Y && gate.X + gate.Width <= entrance.X + entrance.Width
            && gate.Y + gate.Height <= entrance.Y + entrance.Height, "portão fora da entrada da Cripta");
    }

    /// <summary>
    /// T-352: a Cripta é uma instância cuja saída leva de volta às Ruínas, com as salas em sequência e, no fim, a Sala do
    /// Vigia: o Vigia nível 15 e três selos (objetivos), todos alcançáveis a pé da entrada.
    /// </summary>
    [Fact]
    public void Crypt_is_an_instance_with_the_rooms_in_sequence_and_the_watcher_with_three_seals()
    {
        var crypt = Db.Value.Zones["zone.watcher_crypt"];
        Assert.Equal(ZoneKind.Instance, crypt.Kind);
        Assert.Equal("zone.old_road_ruins", crypt.ExitGate()!.To);

        var rooms = crypt.MonsterSpawns.Select(s => s.Area).Distinct().ToList();
        Assert.Equal(["Salão dos Escudos", "Galeria das Cinzas", "Câmara dos Juramentos", "Rota dos Mortos", "Passagem Estreita", "Antessala", "Sala do Vigia"], rooms);

        var boss = crypt.MonsterSpawns.Where(s => s.Area == "Sala do Vigia").ToList();
        var watcher = Assert.Single(boss, s => s.Monster == "monster.faceless_watcher");
        Assert.Equal(15, watcher.Level);
        var seals = boss.Where(s => s.Objective).ToList();
        Assert.Equal(3, seals.Count);
        Assert.All(seals, s => Assert.Equal("monster.watcher_seal", s.Monster));
        Assert.All(crypt.MonsterSpawns.Where(s => s.Monster != "monster.faceless_watcher" && !s.Objective), s => Assert.InRange(s.Level ?? 0, 12, 14));
    }

    /// <summary>
    /// T-342: a Floresta Baixa sai pelo portão norte da Cidade e tem as quatro subáreas, com os monstros nos níveis 5–8.
    /// O Covil da Matriarca sai dos Campos e passa a ter o portão a partir da Floresta (a caverna do Ninho), ida e volta.
    /// </summary>
    [Fact]
    public void Forest_is_north_of_the_city_with_the_four_subareas_and_the_lair_gate()
    {
        var forest = Db.Value.Zones["zone.lower_forest"];
        Assert.Equal(ZoneKind.Field, forest.Kind);
        var city = Db.Value.Zones["zone.city"];
        var north = Assert.Single(city.Gates, g => g.To == "zone.lower_forest");
        Assert.Equal(0, north.Area.Y);
        Assert.Contains(forest.Gates, g => g.To == "zone.city");

        Assert.Equal(["Bosque das Teias", "Clareira da Névoa", "Ninho da Matriarca", "Trilha dos Caçadores"],
            forest.MonsterSpawns.Select(s => s.Area).Distinct().Order(StringComparer.Ordinal));
        Assert.All(forest.MonsterSpawns, s => Assert.InRange(s.Level ?? 0, 5, 8));
        Assert.Single(forest.MonsterSpawns, s => s.Monster == "monster.spider_big" && s.Area == "Bosque das Teias");
        Assert.Contains(forest.MonsterSpawns, s => s.Monster == "monster.corrupted_ent" && s.Area == "Clareira da Névoa");

        Assert.Equal("zone.matriarch_lair", Assert.Single(forest.Gates, g => g.To != "zone.city").To);
        Assert.Equal(["zone.lower_forest"], Db.Value.Zones["zone.matriarch_lair"].Gates.Select(g => g.To));
        Assert.DoesNotContain(Db.Value.Zones["zone.vigil_fields"].Gates, g => g.To == "zone.matriarch_lair");
    }

    /// <summary>T-342: os grupos de monstros da Floresta ficam a pelo menos 12 tiles uns dos outros.</summary>
    [Fact]
    public void Forest_groups_are_at_least_12_tiles_apart()
    {
        var spawns = Db.Value.Zones["zone.lower_forest"].MonsterSpawns;
        foreach (var a in spawns)
        {
            foreach (var b in spawns.Where(b => !ReferenceEquals(a, b)))
            {
                var d = Math.Sqrt(Math.Pow(a.Position.X - b.Position.X, 2) + Math.Pow(a.Position.Y - b.Position.Y, 2));
                Assert.True(d >= 12, $"{a.Monster} ({a.Position.X}, {a.Position.Y}) a {d:F1} de {b.Monster} ({b.Position.X}, {b.Position.Y})");
            }
        }
    }

    /// <summary>
    /// T-530: o Brejo dos Afogados sai pelo portão sul da Cidade (e volta por ele), com as quatro subáreas e os monstros
    /// nos níveis 16–20. A Bruxa do Brejo é o único chefe: nível 20, anunciada à zona e renasce em 10 min. Quem morre no
    /// Brejo volta à Cidade (zona sem <c>respawn</c>, T-501).
    /// </summary>
    [Fact]
    public void Marsh_is_south_of_the_city_with_the_four_subareas_and_the_witch()
    {
        var marsh = Db.Value.Zones[Marsh];
        Assert.Equal(ZoneKind.Field, marsh.Kind);
        Assert.Null(marsh.Respawn);
        var city = Db.Value.Zones["zone.city"];
        var cityMap = EntityFactory.BuildMap(Db.Value.Arenas[city.Arena]);
        var south = Assert.Single(city.Gates, g => g.To == Marsh);
        Assert.Equal(cityMap.Height, (int)(south.Area.Y + south.Area.Height));
        var back = Assert.Single(marsh.Gates, g => g.To == "zone.city");
        Assert.Equal(0, back.Area.Y);

        Assert.Equal(["Aldeia das Palafitas", "Cabana da Bruxa", "Charco Fundo", "Estrada dos Juncos"],
            marsh.MonsterSpawns.Select(s => s.Area).Distinct().Order(StringComparer.Ordinal));
        Assert.All(marsh.MonsterSpawns, s => Assert.InRange(s.Level ?? 0, 16, 20));
        var witch = Assert.Single(marsh.MonsterSpawns, s => Db.Value.Monsters[s.Monster].Rank == MonsterRank.Boss);
        Assert.Equal("monster.marsh_witch", witch.Monster);
        Assert.Equal("Cabana da Bruxa", witch.Area);
        Assert.Equal(20, witch.Level);
        Assert.True(witch.Announce);
        Assert.Equal(600 * Core.Simulation.World.TickRate, witch.RespawnTicks);
        Assert.All(marsh.MonsterSpawns.Where(s => s != witch), s => Assert.False(s.Announce));
    }

    /// <summary>
    /// T-530: os grupos do Brejo (pontos da mesma subárea a até 4 tiles, como nos testes de design) ficam a pelo menos 12
    /// tiles uns dos outros, e parte deles tem de 4 a 6 monstros.
    /// </summary>
    [Fact]
    public void Marsh_groups_are_at_least_12_tiles_apart_and_some_have_4_to_6_monsters()
    {
        var groups = new List<List<MonsterSpawnDefinition>>();
        foreach (var p in Db.Value.Zones[Marsh].MonsterSpawns)
        {
            var near = groups.FirstOrDefault(g => g[0].Area == p.Area && g.Any(q => Distance(p.Position, q.Position) <= 4));
            if (near is null)
            {
                groups.Add([p]);
            }
            else
            {
                near.Add(p);
            }
        }

        for (var i = 0; i < groups.Count; i++)
        {
            for (var j = i + 1; j < groups.Count; j++)
            {
                var d = groups[i].SelectMany(a => groups[j].Select(b => Distance(a.Position, b.Position))).Min();
                Assert.True(d >= 12, $"grupos em ({groups[i][0].Position.X}, {groups[i][0].Position.Y}) e ({groups[j][0].Position.X}, {groups[j][0].Position.Y}) a {d:F1} tiles");
            }
        }

        Assert.Contains(groups, g => g.Sum(p => p.Count) is >= 4 and <= 6);
        Assert.All(groups, g => Assert.InRange(g.Sum(p => p.Count), 1, 6));
    }

    /// <summary>
    /// T-530: a água funda do Brejo é terreno lento (T-502) que o Homem-lagarto não sente, e a estrada seca liga o portão
    /// norte ao portão sul (para o Forte do Passo, T-531) sem pisar em <c>slow_terrain</c>. Há água funda onde se luta: em
    /// todas as subáreas menos a Estrada há grupo a até 8 tiles dela.
    /// </summary>
    [Fact]
    public void Marsh_dry_road_links_the_north_gate_to_the_south_gate_without_deep_water()
    {
        var zone = Db.Value.Zones[Marsh];
        var arena = Db.Value.Arenas[zone.Arena];
        var map = EntityFactory.BuildMap(arena);
        var water = arena.Areas.Where(a => a.Kind == "slow_terrain").ToList();
        Assert.NotEmpty(water);
        Assert.All(water, a =>
        {
            Assert.Equal("status.deep_water", a.Properties["status"]);
            Assert.Equal("amphibious", a.Properties["ignoreTags"]);
        });
        bool Wet(int x, int y) => water.Any(a => x >= a.Area.X && x < a.Area.X + a.Area.Width && y >= a.Area.Y && y < a.Area.Y + a.Area.Height);

        var gate = Assert.Single(zone.Gates, g => g.To == "zone.city").Area;
        var fortGate = Assert.Single(zone.Gates, g => g.To == Fort).Area;
        var queue = new Queue<(int X, int Y)>();
        var seen = new HashSet<(int, int)>();
        for (var x = (int)gate.X; x < gate.X + gate.Width; x++)
        {
            for (var y = (int)gate.Y; y < gate.Y + gate.Height; y++)
            {
                if (!map.IsBlocked(x, y) && seen.Add((x, y)))
                {
                    queue.Enqueue((x, y));
                }
            }
        }

        var south = false;
        while (queue.Count > 0 && !south)
        {
            var (x, y) = queue.Dequeue();
            south = fortGate.Contains(x, y);
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx >= 0 && ny >= 0 && nx < map.Width && ny < map.Height && !map.IsBlocked(nx, ny) && !Wet(nx, ny) && seen.Add((nx, ny)))
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }

        Assert.True(south, "a estrada seca não chega ao portão sul");
        foreach (var area in new[] { "Aldeia das Palafitas", "Charco Fundo", "Cabana da Bruxa" })
        {
            Assert.Contains(zone.MonsterSpawns, s => s.Area == area && water.Any(a =>
                Distance(s.Position, new PositionDefinition(Math.Clamp(s.Position.X, a.Area.X, a.Area.X + a.Area.Width), Math.Clamp(s.Position.Y, a.Area.Y, a.Area.Y + a.Area.Height))) <= 8));
        }
    }

    /// <summary>T-530: no Brejo, toda célula livre é alcançável a pé e não há fresta sem saída (como nas Ruínas).</summary>
    [Fact]
    public void Marsh_has_no_sealed_pockets_or_dead_end_slits() => AssertNoPocketsOrSlits(Marsh);

    private const string Marsh = "zone.drowned_marsh";

    private const string Fort = "zone.pass_fort";

    /// <summary>
    /// T-531: o Forte do Passo é uma zona segura (descanso igual ao da Cidade, sem monstros) ao sul do Brejo: o portão sul
    /// do Brejo, na borda de baixo, leva ao portão norte do Forte, na borda de cima, e volta. É o único portão aberto: o
    /// leste (Serra Partida, T-532) e o sul (Planalto, fase 6) ficam fechados, com o muro inteiro. Zona segura não leva
    /// <c>respawn</c>: quem morre na Serra e nas Minas renasce no nascimento dela (T-501).
    /// </summary>
    [Fact]
    public void Fort_is_a_safe_zone_south_of_the_marsh_without_monsters_and_with_only_the_north_gate_open()
    {
        var fort = Db.Value.Zones[Fort];
        Assert.Equal(ZoneKind.Safe, fort.Kind);
        Assert.Equal(Db.Value.Zones["zone.city"].RestPerSecond, fort.RestPerSecond);
        Assert.True(fort.RestPerSecond > 0);
        Assert.Null(fort.Respawn);
        Assert.Empty(fort.MonsterSpawns);

        var marsh = Db.Value.Zones[Marsh];
        var marshMap = EntityFactory.BuildMap(Db.Value.Arenas[marsh.Arena]);
        var south = Assert.Single(marsh.Gates, g => g.To == Fort);
        Assert.Equal(marshMap.Height, (int)(south.Area.Y + south.Area.Height));
        var north = Assert.Single(fort.Gates);
        Assert.Equal(Marsh, north.To);
        Assert.Equal(0, north.Area.Y);

        var map = EntityFactory.BuildMap(Db.Value.Arenas[fort.Arena]);
        Assert.Equal((60, 45), (map.Width, map.Height));
        for (var y = 0; y < map.Height; y++)
        {
            Assert.True(map.IsBlocked(map.Width - 1, y), $"muro leste aberto em y = {y}");
            Assert.True(map.IsBlocked(0, y), $"muro oeste aberto em y = {y}");
        }

        for (var x = 0; x < map.Width; x++)
        {
            Assert.True(map.IsBlocked(x, map.Height - 1), $"muro sul aberto em x = {x}");
            Assert.True(map.IsBlocked(x, 0) || north.Area.Contains(x + 0.5, 0.5), $"muro norte aberto fora do portão em x = {x}");
        }
    }

    /// <summary>
    /// T-531: os NPCs do Forte: o Intendente com a loja do Forte (T-513), o Sacerdote com o Templo (poções), o mestre das
    /// classes (abre a escolha da segunda classe no cliente, T-541) e o estalajadeiro, só decorativo. Todos alcançáveis a pé
    /// do nascimento da zona.
    /// </summary>
    [Fact]
    public void Fort_has_the_quartermaster_priest_class_master_and_innkeeper_reachable_on_foot()
    {
        var fort = Db.Value.Zones[Fort];
        var expected = new (string Npc, string? Shop)[]
        {
            ("npc.class_master", null), ("npc.innkeeper", null), ("npc.priest", "shop.temple"), ("npc.quartermaster", "shop.pass_fort"),
        };
        Assert.Equal(expected, fort.Npcs.Select(n => (n.Npc, n.Shop)).OrderBy(n => n.Npc, StringComparer.Ordinal));
        Assert.Equal("Intendente", Assert.Single(fort.Npcs, n => n.Npc == "npc.quartermaster").Properties["name"]);

        var reached = Reached(fort);
        var map = EntityFactory.BuildMap(Db.Value.Arenas[fort.Arena]);
        foreach (var npc in fort.Npcs)
        {
            var cell = ((int)npc.Position.X, (int)npc.Position.Y);
            Assert.False(map.IsBlocked(cell.Item1, cell.Item2), $"{npc.Npc} numa célula bloqueada");
            Assert.True(reached.Contains(cell), $"{npc.Npc} inalcançável");
        }
    }

    /// <summary>T-531: no Forte, toda célula livre é alcançável a pé e não há fresta sem saída.</summary>
    [Fact]
    public void Fort_has_no_sealed_pockets_or_dead_end_slits() => AssertNoPocketsOrSlits(Fort);

    private static double Distance(PositionDefinition a, PositionDefinition b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    /// <summary>
    /// Legibilidade das Ruínas: toda célula livre é alcançável a pé (nenhum bolsão fechado, como a borda das muralhas antes
    /// de ser bloqueada) e nenhuma célula livre fora dos portões é ponta de fresta (no máximo 1 vizinho livre).
    /// </summary>
    [Fact]
    public void Ruins_have_no_sealed_pockets_or_dead_end_slits() => AssertNoPocketsOrSlits("zone.old_road_ruins");

    private static void AssertNoPocketsOrSlits(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        var map = EntityFactory.BuildMap(Db.Value.Arenas[zone.Arena]);
        var reached = Reached(zone);
        var gates = zone.Gates.SelectMany(g => Enumerable.Range((int)g.Area.X, (int)g.Area.Width)
            .SelectMany(x => Enumerable.Range((int)g.Area.Y, (int)g.Area.Height).Select(y => (x, y)))).ToHashSet();
        bool Free(int x, int y) => x >= 0 && y >= 0 && x < map.Width && y < map.Height && !map.IsBlocked(x, y);
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                if (!Free(x, y))
                {
                    continue;
                }

                Assert.True(reached.Contains((x, y)), $"célula livre ({x}, {y}) inalcançável");
                var exits = new[] { Free(x + 1, y), Free(x - 1, y), Free(x, y + 1), Free(x, y - 1) }.Count(f => f);
                Assert.True(exits >= 2 || gates.Contains((x, y)), $"fresta sem saída em ({x}, {y})");
            }
        }
    }

    /// <summary>Células livres alcançáveis a pé (4 vizinhos) a partir do nascimento da zona.</summary>
    private static HashSet<(int, int)> Reached(ZoneDefinition zone)
    {
        var map = EntityFactory.BuildMap(Db.Value.Arenas[zone.Arena]);
        var reached = new HashSet<(int, int)>();
        var queue = new Queue<(int X, int Y)>();
        var start = ((int)zone.Spawn.X, (int)zone.Spawn.Y);
        Assert.False(map.IsBlocked(start.Item1, start.Item2));
        reached.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
            {
                if (nx >= 0 && ny >= 0 && nx < map.Width && ny < map.Height && !map.IsBlocked(nx, ny) && reached.Add((nx, ny)))
                {
                    queue.Enqueue((nx, ny));
                }
            }
        }

        return reached;
    }

    [Theory]
    [MemberData(nameof(Zones))]
    public void Map_transitions_match_the_zone_gates(string zoneId)
    {
        var zone = Db.Value.Zones[zoneId];
        var arena = Db.Value.Arenas[zone.Arena];
        Assert.Equal(zone.Gates.Count, arena.Transitions.Count);
        foreach (var t in arena.Transitions)
        {
            var gate = Assert.Single(zone.Gates, g => g.To == t.To);
            Assert.Equal((gate.Area.X, gate.Area.Y, gate.Area.Width, gate.Area.Height), (t.Area.X, t.Area.Y, t.Area.Width, t.Area.Height));
            var target = Db.Value.Arenas[Db.Value.Zones[t.To].Arena];
            var arrival = Assert.Single(target.Spawns, s => s.Name == t.Arrival);
            Assert.Equal((gate.Arrival.X, gate.Arrival.Y), (arrival.Position.X, arrival.Position.Y));
        }
    }
}
