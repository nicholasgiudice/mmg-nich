using System.Collections.Concurrent;
using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Spatial;
using Game.Sim;
using static Game.Design.Tests.DesignHarness;

namespace Game.Design.Tests;

/// <summary>Um jogador do perfil: a classe base e, se já tem, a segunda classe (T-403).</summary>
internal sealed record PaceMember(string Base, string? Second = null);

/// <summary>Resultado de uma luta contra um grupo: tempo médio de vitória, taxa de vitória e se o tempo é do limite (ninguém venceu).</summary>
internal readonly record struct Fight(double Seconds, double WinRate);

/// <summary>Um nível do ritmo de campo.</summary>
/// <param name="Tier">Nível dos grupos caçados (o maior nível de grupo que não passa do nível do jogador).</param>
internal sealed record FieldLevelPace(
    int Level, int Tier, int Groups, double XpPerGroup, double FightSeconds, double WalkSeconds, double MinWinRate, double XpPerHour, double Minutes);

/// <summary>Ritmo de campo de uma faixa de níveis: por nível e no total.</summary>
internal sealed record FieldPace(string Zone, int From, int To, int PartySize, IReadOnlyList<FieldLevelPace> Levels)
{
    public double TotalMinutes => Levels.Sum(l => l.Minutes);

    /// <summary>XP por pessoa por hora na faixa inteira (XP total para subir ÷ tempo total).</summary>
    public double XpPerHour => Levels.Sum(l => l.XpPerHour * l.Minutes / 60) / (TotalMinutes / 60);

    public double MinWinRate => Levels.Min(l => l.MinWinRate);

    public string Describe() => $"{Zone} nv{From}→{To} (grupo de {PartySize}): {TotalMinutes / 60:F2} h, {XpPerHour:F0} XP/h por pessoa, vitória mín. {MinWinRate:P0}; "
        + string.Join("; ", Levels.Select(l => $"nv{l.Level} (grupos nv{l.Tier} ×{l.Groups}): {l.XpPerHour:F0} XP/h, luta {l.FightSeconds:F0} s + caminhada {l.WalkSeconds:F0} s"));
}

/// <summary>Ritmo de uma ida completa a uma masmorra.</summary>
internal sealed record DungeonPace(string Zone, int Level, int PartySize, int Groups, double XpPerPerson, double FightSeconds, double RestSeconds, double WalkSeconds, double MinWinRate)
{
    public double RunSeconds => FightSeconds + RestSeconds + WalkSeconds;

    /// <summary>XP por pessoa por hora, repetindo a ida (sem contar o tempo de reentrar).</summary>
    public double XpPerHour => XpPerPerson / RunSeconds * 3600;

    public string Describe() => $"{Zone} nv{Level} (grupo de {PartySize}): {XpPerPerson:F0} XP por pessoa em {RunSeconds / 60:F1} min "
        + $"(lutas {FightSeconds / 60:F1} + pausas {RestSeconds / 60:F1} + caminhada {WalkSeconds / 60:F1}) = {XpPerHour:F0} XP/h por pessoa, vitória mín. {MinWinRate:P0}";
}

/// <summary>
/// T-505: a medida de XP por hora de uma zona (<c>design/segunda-classe-v0.1.md</c> §3: "o que decide se fica bom é a XP por
/// minuto, não a XP por monstro"). Aceita qualquer zona do mundo aberto pelo id (a fase 6 reaproveita), um perfil (classes e
/// segundas classes, nível) e um tamanho de grupo, e conta o que o modelo de ritmo dos Campos já contava:
/// <list type="bullet">
/// <item>luta: o tempo médio de vitória do grupo de monstros nos níveis do mapa (lote do simulador, perfil esperado do nível do
/// grupo, <see cref="FieldGroups.Encounter(FieldGroup, IReadOnlyList{string}, int)"/>);</item>
/// <item>deslocamento: caminhada pelo mapa (<see cref="Pathfinder"/>) num circuito dos grupos caçados, pela velocidade das classes;</item>
/// <item>descanso: <see cref="FieldRestSeconds"/> por grupo (a parte do <see cref="FieldsDesignTests.OverheadSeconds"/> que não é
/// caminhada), ou <see cref="CryptDesignTests.PauseSeconds"/> por sala numa masmorra;</item>
/// <item>renascimento: no campo, no máximo o que renasce (todos os grupos do nível, cada um no seu tempo);</item>
/// <item>divisão de XP do grupo (T-310): cada pessoa leva <see cref="ProgressionRules.Share"/> da XP do monstro (com o bônus
/// de grupo), com o modificador de nível calculado pelo nível do jogador.</item>
/// </list>
/// A luta usa o perfil do nível do <b>grupo</b> (como os modelos de ritmo da Floresta e das Ruínas); a XP usa o nível do jogador.
/// </summary>
internal static class ZonePace
{
    /// <summary>Descanso por grupo no campo: os 60 s do modelo dos Campos menos ~10 s de caminhada (agora medida no mapa).</summary>
    public const double FieldRestSeconds = FieldsDesignTests.OverheadSeconds - 10;

    /// <summary>Limite de tempo de uma luta de campo solo e em grupo (como os testes de zona).</summary>
    public const int SoloTimeLimit = 120;
    public const int PartyTimeLimit = 180;
    public const int BossTimeLimit = 360;

    public static readonly string[] BaseClasses = ["class.warrior", "class.archer", "class.mage", "class.cleric"];

    /// <summary>Grupo de referência com segunda classe: tanque, caçador (área), piromante (área) e curador.</summary>
    public static readonly string[] PartySecondClasses =
    [
        SecondClassDesignTests.Guardian, SecondClassDesignTests.Hunter, SecondClassDesignTests.Pyromancer, SecondClassDesignTests.Priest,
    ];

    private static readonly Dictionary<string, string> Strategy = new()
    {
        // Como nos testes de zona: todos desviam dos telegráfos; Arqueiro e Mago atacam de longe.
        ["class.warrior"] = "rotation+dodge",
        ["class.archer"] = "kite+dodge",
        ["class.mage"] = "kite+dodge",
        ["class.cleric"] = "rotation+dodge",
    };

    private static int SecondClassLevel => Content.Rules.Progression.SecondClassLevel;

    /// <summary>Quem caça sozinho no nível: as 4 bases antes da segunda classe; depois as 8 segundas classes (a média delas).</summary>
    public static IReadOnlyList<IReadOnlyList<PaceMember>> SoloProfiles(int level) =>
        level < SecondClassLevel
            ? BaseClasses.Select(c => (IReadOnlyList<PaceMember>)[new PaceMember(c)]).ToList()
            : SecondClassDesignTests.Specs.Select(s => (IReadOnlyList<PaceMember>)[new PaceMember(s.Value.BaseClass, s.Key)]).ToList();

    /// <summary>O grupo de 4 no nível: as 4 bases antes da segunda classe; depois <see cref="PartySecondClasses"/>.</summary>
    public static IReadOnlyList<PaceMember> Party(int level) =>
        level < SecondClassLevel
            ? BaseClasses.Select(c => new PaceMember(c)).ToList()
            : PartySecondClasses.Select(s => new PaceMember(SecondClassDesignTests.BaseOf(s), s)).ToList();

    /// <summary>Velocidade média do grupo, em tiles por segundo.</summary>
    public static double Speed(IEnumerable<PaceMember> members) => members.Average(m => Content.Classes[m.Base].Speed);

    public static double Speed(int level, int partySize) =>
        (partySize == 1 ? SoloProfiles(level).Select(p => p[0]) : Party(level)).Average(m => Content.Classes[m.Base].Speed);

    // ---------- luta ----------

    private static readonly ConcurrentDictionary<string, Lazy<BatchSummary>> Cache = new();

    /// <summary>Luta a partir de lotes já feitos (média dos tempos de vitória e a menor taxa); sem vitória conta o limite.</summary>
    public static Fight FightOf(IEnumerable<BatchSummary> batches, int timeLimit)
    {
        var list = batches.ToList();
        return new Fight(list.Select(b => b.WinRate > 0 ? b.TimeToWinMean : timeLimit).Average(), list.Min(b => b.WinRate));
    }

    /// <summary>
    /// Luta do perfil esperado do nível do grupo contra o grupo de monstros: sozinho (média dos perfis de
    /// <see cref="SoloProfiles"/>) ou o grupo de 4 de <see cref="Party"/>.
    /// </summary>
    public static Fight FightOf(FieldGroup group, int partySize, int? timeLimit = null)
    {
        var level = group.Level;
        var limit = timeLimit ?? (partySize == 1 ? SoloTimeLimit : PartyTimeLimit);
        var batches = partySize == 1
            ? SoloProfiles(level).Select(p => RunBatch(group, level, p, limit)).ToList()
            : [RunBatch(group, level, Party(level), limit)];

        // Sem vitória não há tempo de vitória: conta o limite (e a taxa de vitória denuncia).
        return FightOf(batches, limit);
    }

    private static BatchSummary RunBatch(FieldGroup group, int level, IReadOnlyList<PaceMember> members, int timeLimit) =>
        Cache.GetOrAdd($"{group.Key}|{level}|{timeLimit}|{string.Join(",", members.Select(m => $"{m.Base}/{m.Second}"))}", _ => new Lazy<BatchSummary>(() =>
        {
            var solo = members.Count == 1;
            var classes = members.Select(m => m.Base).ToList();
            var (content, encounter) = FieldGroups.Encounter(group, classes, timeLimit);
            var options = new Dictionary<string, string>(Options);
            foreach (var m in members.Where(m => m.Second is not null))
            {
                var spec = SecondClassDesignTests.Specs[m.Second!];
                foreach (var (k, v) in (solo ? spec.SoloOptions : spec.GroupOptions) ?? new Dictionary<string, string>())
                {
                    options[k] = v;
                }
            }

            return Batch.Run(content, new RunSpec
            {
                EncounterId = encounter,
                Strategy = Strategy[classes[0]],
                Roles = classes.Skip(1).Where(c => Strategy[c] != Strategy[classes[0]]).ToDictionary(c => c, c => Strategy[c]),
                Options = new BotOptions(options),
                Runs = Runs,
                HazardSourceFactory = () => new TelegraphHazardSource(),
                Profile = ProfileOf(level, members),
            });
        })).Value;

    /// <summary>Perfil esperado do nível, com a segunda classe de quem a tem (os pontos de habilidade da base e da segunda como em <see cref="SecondClassDesignTests.WithSecondClass"/>).</summary>
    public static CharacterProfile ProfileOf(int level, IReadOnlyList<PaceMember> members)
    {
        var profile = CharacterProfile.Expected(level);
        var solo = members.Count == 1;
        if (solo)
        {
            profile = profile with { Focus = SoloFocus };
        }

        var ranks = new Dictionary<string, Dictionary<string, int>>();
        var second = new Dictionary<string, string>();
        var secondRanks = new Dictionary<string, Dictionary<string, int>>();
        foreach (var m in members.Where(m => m.Second is not null))
        {
            var p = SecondClassDesignTests.WithSecondClass(level, m.Second!, solo: solo);
            ranks[m.Base] = p.Ranks[m.Base];
            second[m.Base] = m.Second!;
            secondRanks[m.Base] = p.SecondClassRanks[m.Base];
        }

        return second.Count == 0 ? profile : profile with { Ranks = ranks, SecondClass = second, SecondClassRanks = secondRanks };
    }

    // ---------- XP ----------

    /// <summary>XP de uma pessoa de nível <paramref name="level"/> por grupo morto, numa divisão entre <paramref name="partySize"/> (objetivos não dão XP).</summary>
    public static double XpOf(FieldGroup group, int level, int partySize)
    {
        var rules = Content.Rules.Progression;
        return group.Points.Where(p => !p.Objective).Sum(p =>
            p.Count * rules.Share(rules.MonsterXp(p.Level ?? 1, Content.Monsters[p.Monster].Rank, level), partySize));
    }

    // ---------- deslocamento ----------

    /// <summary>
    /// Distância média, em tiles, de um grupo ao próximo num circuito fechado pelos pontos (sempre o mais perto que falta, a
    /// pé pelo mapa; sem caminho, a distância reta × 1,3).
    /// </summary>
    public static double MeanLeg(string zone, IReadOnlyList<PositionDefinition> points)
    {
        if (points.Count < 2)
        {
            return 0;
        }

        var map = EntityFactory.BuildMap(Content.Arenas[Content.Zones[zone].Arena]);
        var left = points.Select(Vec).ToList();
        var at = left[0];
        left.RemoveAt(0);
        var total = 0.0;
        var first = at;
        while (left.Count > 0)
        {
            var next = left.OrderBy(p => Vector2.Distance(at, p)).First();
            total += Walk(map, at, next);
            left.Remove(next);
            at = next;
        }

        return (total + Walk(map, at, first)) / points.Count;
    }

    /// <summary>Comprimento do caminho a pé entre dois pontos do mapa.</summary>
    public static double Walk(GridMap map, Vector2 from, Vector2 to)
    {
        var path = Pathfinder.FindPath(map, from, to);
        return path.Count == 0 ? Vector2.Distance(from, to) * 1.3 : path.Prepend(from).Zip(path, Vector2.Distance).Sum();
    }

    public static Vector2 Vec(PositionDefinition p) => new((float)p.X, (float)p.Y);

    // ---------- campo ----------

    /// <summary>Grupos do campo que o jogador caça: sem chefe e sem objetivo.</summary>
    public static bool IsHuntable(FieldGroup g) =>
        g.Points.All(p => !p.Objective && Content.Monsters[p.Monster].Rank != MonsterRank.Boss);

    /// <summary>
    /// XP por pessoa por hora caçando <paramref name="zone"/> dos níveis <paramref name="from"/> a <paramref name="to"/> − 1 (o
    /// tempo para subir de <paramref name="from"/> a <paramref name="to"/>). No nível L o jogador caça os grupos do maior nível
    /// de grupo que não passa de L (ou o menor, se L está abaixo de todos), um a um, com o ciclo luta + caminhada + descanso,
    /// limitado pelo renascimento. <paramref name="fight"/> troca a luta (testes do modelo); <paramref name="include"/> escolhe os
    /// grupos (padrão: <see cref="IsHuntable"/>).
    /// </summary>
    public static FieldPace Field(string zone, int from, int to, int partySize, Func<FieldGroup, Fight>? fight = null, Func<FieldGroup, bool>? include = null)
    {
        fight ??= g => FightOf(g, partySize);
        include ??= IsHuntable;
        var rules = Content.Rules.Progression;
        var groups = FieldGroups.Of(zone).Where(include).ToList();
        if (groups.Count == 0)
        {
            throw new InvalidOperationException($"Zona {zone} sem grupos para caçar.");
        }

        var levels = new List<FieldLevelPace>();
        for (var level = from; level < to; level++)
        {
            var tier = groups.Where(g => g.Level <= level).Select(g => g.Level).DefaultIfEmpty(groups.Min(g => g.Level)).Max();
            var hunted = groups.Where(g => g.Level == tier).ToList();
            var fights = hunted.Select(fight).ToList();
            var xpPerGroup = hunted.Average(g => XpOf(g, level, partySize));
            var fightSeconds = fights.Average(f => f.Seconds);
            var walkSeconds = MeanLeg(zone, hunted.Select(g => g.Points[0].Position).ToList()) / Speed(tier, partySize);
            var cycle = fightSeconds + walkSeconds + FieldRestSeconds;
            var groupsPerSecond = Math.Min(1 / cycle, hunted.Sum(g => 1 / g.RespawnSeconds));
            var xpPerHour = xpPerGroup * groupsPerSecond * 3600;
            levels.Add(new FieldLevelPace(level, tier, hunted.Count, xpPerGroup, fightSeconds, walkSeconds,
                fights.Min(f => f.WinRate), xpPerHour, rules.XpToNext(level) / (xpPerHour / 60)));
        }

        return new FieldPace(zone, from, to, partySize, levels);
    }

    /// <summary>
    /// Chefe de campo (só a medida, sem meta): XP por pessoa por hora matando o chefe de <paramref name="zone"/> toda vez que
    /// renasce (ou, se a luta é mais longa que o renascimento, em seguida). A luta é a de <see cref="FightOf"/> com o limite de
    /// <see cref="BossTimeLimit"/>.
    /// </summary>
    public static (string Boss, double XpPerPerson, double CycleSeconds, double XpPerHour, double WinRate) FieldBoss(
        string zone, int level, int partySize, Func<FieldGroup, Fight>? fight = null)
    {
        fight ??= g => FightOf(g, partySize, BossTimeLimit);
        var boss = FieldGroups.Of(zone).First(g => g.Points.Any(p => Content.Monsters[p.Monster].Rank == MonsterRank.Boss && !p.Objective));
        var f = fight(boss);
        var walk = MeanLeg(zone, [Content.Zones[zone].Spawn, boss.Points[0].Position]) / Speed(level, partySize);
        var cycle = Math.Max(f.Seconds + walk + FieldRestSeconds, boss.RespawnSeconds);
        var xp = XpOf(boss, level, partySize);
        return (boss.Key, xp, cycle, xp / cycle * 3600, f.WinRate);
    }

    // ---------- masmorra ----------

    /// <summary>
    /// Uma ida completa a uma masmorra (instância): um passeio por todas as salas (<paramref name="include"/>) na ordem dos
    /// dados, saindo da entrada, e o chefe por último (<paramref name="boss"/>, com a luta dada). Tempo = lutas +
    /// <see cref="CryptDesignTests.PauseSeconds"/> por sala + caminhada pela velocidade do grupo (como
    /// <c>CryptDesignTests.A_full_run_takes_20_to_40_minutes</c>). XP de cada pessoa no nível <paramref name="level"/> dividida
    /// entre <paramref name="partySize"/>.
    /// </summary>
    public static DungeonPace Dungeon(
        string zone, int level, int partySize, Func<FieldGroup, bool>? include = null, (FieldGroup Group, Fight Fight)? boss = null,
        Func<FieldGroup, Fight>? fight = null)
    {
        fight ??= g => FightOf(g, partySize);
        include ??= IsHuntable;
        var rooms = FieldGroups.Of(zone).Where(include).ToList();
        var steps = rooms.Select(g => (Group: g, Fight: fight(g))).ToList();
        if (boss is { } b)
        {
            steps.Add(b);
        }

        var crypt = Content.Zones[zone];
        var map = EntityFactory.BuildMap(Content.Arenas[crypt.Arena]);
        var walk = 0.0;
        var at = Vec(crypt.Spawn);
        foreach (var (g, _) in steps)
        {
            var to = Vec(g.Points[0].Position);
            walk += Walk(map, at, to);
            at = to;
        }

        var speed = Speed(level, partySize);
        return new DungeonPace(zone, level, partySize, steps.Count, steps.Sum(s => XpOf(s.Group, level, partySize)),
            steps.Sum(s => s.Fight.Seconds), rooms.Count * CryptDesignTests.PauseSeconds, walk / speed, steps.Min(s => s.Fight.WinRate));
    }
}
