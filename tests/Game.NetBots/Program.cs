using System.Diagnostics;
using System.Globalization;
using Game.Bots;
using Game.Core.Combat;
using Game.Data;
using Game.NetBots;

// T-109: bots de rede para teste de carga. Uso: ver Usage.
const string Usage = """
    Bots de rede (T-109): jogadores sem Godot que se conectam ao servidor de verdade e jogam com as estratégias do Game.Bots.

    Uso: dotnet run --project tests/Game.NetBots -c Release -- [opções]
      --server <host:porta>[,<host:porta>...]  servidores (padrão 127.0.0.1:7777); os bots enchem uma sala de cada vez
      --bots <n>              quantos bots (padrão 4)
      --per-room <n>          bots por servidor antes de passar para o próximo (padrão 4 = vagas da Matriarca)
      --class <id|mix>        classe de todos, ou mix = Guerreiro, Arqueiro, Mago, Clérigo em rodízio (padrão mix)
      --strategy <nome>       estratégia do Game.Bots (padrão kite+dodge)
      --duration <s>          quanto tempo rodar (padrão 300)
      --until-end             para quando todas as salas terminarem o encontro (vitória ou derrota)
      --data <dir>            data/ (padrão: a do repositório); precisa ser igual à do servidor
      --ticket <token>[,...]  tokens de jogo da API (T-212): o bot i entra com o token i, no personagem salvo
                              (sem token, entra só com nome: o servidor precisa de --dev-login)
    """;

var a = args.ToList();
string Opt(string name, string fallback)
{
    var i = a.IndexOf(name);
    return i >= 0 && i + 1 < a.Count ? a[i + 1] : fallback;
}

if (a.Contains("--help") || a.Contains("-h"))
{
    Console.WriteLine(Usage);
    return 0;
}

var servers = Opt("--server", "127.0.0.1:7777").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(s => (Host: s[..s.LastIndexOf(':')], Port: int.Parse(s[(s.LastIndexOf(':') + 1)..], CultureInfo.InvariantCulture)))
    .ToList();
var count = int.Parse(Opt("--bots", "4"), CultureInfo.InvariantCulture);
var perRoom = int.Parse(Opt("--per-room", "4"), CultureInfo.InvariantCulture);
var classOpt = Opt("--class", "mix");
var strategy = Opt("--strategy", "kite+dodge");
var duration = double.Parse(Opt("--duration", "300"), CultureInfo.InvariantCulture);
var untilEnd = a.Contains("--until-end");
var dataDir = Opt("--data", FindData());

var db = GameDataLoader.LoadDirectory(dataDir).GetDatabaseOrThrow();
var content = new ContentCatalog(db.Classes.Values, db.Abilities.Values, db.Statuses.Values, db.Areas.Values,
    db.Monsters.Values, db.Arenas.Values, db.Encounters.Values, db.Items.Values)
{
    Rules = db.Rules,
    Threat = db.Threat,
    Fingerprint = db.Fingerprint,
};
var mix = new[] { "class.warrior", "class.archer", "class.mage", "class.cleric" };
var options = new BotOptions(new Dictionary<string, string> { ["class.mage:explosion"] = "mage.elemental_blast" });
var clock = Stopwatch.StartNew();
var tickets = Opt("--ticket", "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

Console.WriteLine($"{count} bots ({strategy}) em {servers.Count} servidor(es), {perRoom} por sala, por até {duration:0} s; conteúdo {content.Fingerprint}");
var bots = new List<NetBot>();
for (var i = 0; i < count; i++)
{
    var (host, port) = servers[Math.Min(i / perRoom, servers.Count - 1)];
    var cls = classOpt == "mix" ? mix[i % mix.Length] : classOpt;
    bots.Add(new NetBot(i, host, port, cls, strategy, content, options, clock, i < tickets.Length ? tickets[i] : null));
}

var lastReport = 0.0;
var lastBytes = 0L;
var lastIntents = 0L;
while (clock.Elapsed.TotalSeconds < duration)
{
    foreach (var b in bots)
    {
        b.Update();
    }

    var t = clock.Elapsed.TotalSeconds;
    if (t - lastReport >= 10)
    {
        var bytes = bots.Sum(b => b.BytesReceived);
        var intents = bots.Sum(b => b.IntentsSent);
        var span = t - lastReport;
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"[{t,5:0}s] conectados {bots.Count(b => b.Welcome is not null && b.Connected)}/{count} · recebido {(bytes - lastBytes) / span / 1024 / count:0.00} KB/s por bot · "
            + $"intenções {(intents - lastIntents) / span / count:0.0}/s por bot · ping médio {Avg(bots.Select(b => b.PingMs).Where(p => p >= 0)):0} ms · "
            + $"perda {bots.Average(b => b.LossPercent):0.0}% · vitórias {bots.Count(b => b.Outcome == BotOutcome.Won)} derrotas {bots.Count(b => b.Outcome == BotOutcome.Lost)}"));
        lastReport = t;
        lastBytes = bytes;
        lastIntents = intents;
    }

    if (untilEnd && bots.All(b => b.Outcome != BotOutcome.Playing || b.Rejected is not null))
    {
        break;
    }

    if (bots.All(b => b.Rejected is not null))
    {
        Console.WriteLine("Todos os bots foram recusados: " + bots[0].LastError);
        return 2;
    }

    Thread.Sleep(2);
}

var seconds = clock.Elapsed.TotalSeconds;
Console.WriteLine();
Console.WriteLine("bot  servidor              classe         resultado  KB/s   intenções/s  ping   perda  quedas");
foreach (var b in bots)
{
    Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
        $"{b.Index,3}  {b.Server,-20}  {b.ClassId,-13}  {b.Outcome,-6}{(b.EndedAfterSeconds is { } s ? $" {s,4:0}s" : "      ")}  {b.BytesReceived / seconds / 1024,5:0.00}  "
        + $"{b.IntentsSent / seconds,10:0.0}  {b.PingMs,4:0}  {b.LossPercent,4:0.0}%  {b.Disconnects}{(b.Rejected is not null ? $"  recusado: {b.LastError}" : "")}"));
}

Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
    $"\nTOTAL {count} bots, {seconds:0} s: {bots.Sum(b => b.BytesReceived) / seconds / 1024 / count:0.00} KB/s por bot "
    + $"({bots.Sum(b => b.BytesReceived) / seconds / 1024:0.0} KB/s no total) · intenções {bots.Sum(b => b.IntentsSent) / seconds / count:0.0}/s por bot · "
    + $"ping médio {Avg(bots.Select(b => b.PingMs).Where(p => p >= 0)):0} ms · perda {bots.Average(b => b.LossPercent):0.0}% · quedas {bots.Sum(b => b.Disconnects)} · "
    + $"vitórias {bots.Count(b => b.Outcome == BotOutcome.Won)} · derrotas {bots.Count(b => b.Outcome == BotOutcome.Lost)}"));

foreach (var b in bots)
{
    b.Dispose();
}

return 0;

static double Avg(IEnumerable<double> values) => values.DefaultIfEmpty(-1).Average();

static string FindData()
{
    for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
    {
        if (File.Exists(Path.Combine(dir.FullName, "MmoRpg.sln")))
        {
            return Path.Combine(dir.FullName, "data");
        }
    }

    return "data";
}
