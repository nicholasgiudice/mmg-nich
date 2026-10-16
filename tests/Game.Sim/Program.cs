using System.Globalization;
using Game.Bots;
using Game.Sim;

return Cli.Run(args, Console.Out, Console.Error);

namespace Game.Sim
{
    public static class Cli
    {
        private const string Usage = """
            Simulador headless de encontros (T-015).

            Uso: dotnet run --project tests/Game.Sim -- [opções]
              --data <dir>          pasta com os JSON de conteúdo (padrão: data/)
              --encounter <ids>     ids separados por vírgula, ou 'all' (padrão: all)
              --strategy <nomes>    estratégias separadas por vírgula (padrão: rotation); sufixo +dodge para sair de telegráfos
              --role <classe>=<est> estratégia de uma classe (repetível; '*' = todas); ex.: --role class.cleric=heal_only
              --opt <chave>=<valor> opção dos bots (repetível); ex.: --opt taunt=warrior.taunt  --opt class.mage:explosion=tag:aoe
              --runs <n>            execuções por combinação (padrão: 100)
              --seed <n>            seed base; execução i usa seed+i (padrão: 1)
              --max-seconds <s>     limite por execução se o encontro não definir (padrão: 300)
              --jitter <tiles>      variação aleatória (por seed) das posições iniciais, por eixo (padrão: 0.5; 0 desliga)
              --parallel <n>        threads (padrão: núcleos)
              --out <arquivo>       grava também JSON
              --per-run             inclui cada execução no JSON
              --list                lista encontros, classes e estratégias e sai
            """;

        public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
        {
            try
            {
                var o = Parse(args);
                if (o.Help)
                {
                    stdout.WriteLine(Usage);
                    return 0;
                }

                var content = ContentLoading.Load(o.Data);
                if (o.List)
                {
                    stdout.WriteLine("Encontros: " + string.Join(", ", content.Encounters.Keys.Order(StringComparer.Ordinal)));
                    stdout.WriteLine("Classes:   " + string.Join(", ", content.Classes.Keys.Order(StringComparer.Ordinal)));
                    stdout.WriteLine("Estratégias: " + string.Join(", ", BotStrategies.Names));
                    return 0;
                }

                var encounters = o.Encounters.Count == 0 || o.Encounters.Contains("all")
                    ? content.Encounters.Keys.Order(StringComparer.Ordinal).ToList()
                    : o.Encounters;
                if (encounters.Count == 0)
                {
                    stderr.WriteLine("Nenhum encontro encontrado em " + o.Data);
                    return 2;
                }

                foreach (var id in encounters.Where(id => !content.Encounters.ContainsKey(id)))
                {
                    stderr.WriteLine($"Encontro desconhecido: {id}. Use --list.");
                    return 2;
                }

                var summaries = new List<BatchSummary>();
                foreach (var enc in encounters)
                {
                    foreach (var strategy in o.Strategies)
                    {
                        _ = BotStrategies.Create(strategy); // valida o nome cedo
                        var spec = new RunSpec
                        {
                            EncounterId = enc,
                            Strategy = strategy,
                            Roles = o.Roles,
                            Options = new BotOptions(o.Options),
                            BaseSeed = o.Seed,
                            Runs = o.Runs,
                            MaxSeconds = o.MaxSeconds,
                            SpawnJitter = o.Jitter,
                            HazardSourceFactory = () => new TelegraphHazardSource(),
                        };
                        summaries.Add(Batch.Run(content, spec, o.Parallel, o.PerRun));
                    }
                }

                stdout.Write(Report.ToTable(summaries));
                if (o.Out is not null)
                {
                    File.WriteAllText(o.Out, Report.ToJson(o.Data, summaries));
                    stdout.WriteLine($"JSON gravado em {o.Out}");
                }

                return 0;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException or DirectoryNotFoundException or FormatException)
            {
                stderr.WriteLine(ex.Message);
                return 2;
            }
        }

        private sealed class Parsed
        {
            public string Data { get; set; } = "data";
            public List<string> Encounters { get; } = [];
            public List<string> Strategies { get; } = [];
            public Dictionary<string, string> Roles { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> Options { get; } = new(StringComparer.Ordinal);
            public int Runs { get; set; } = 100;
            public ulong Seed { get; set; } = 1;
            public double MaxSeconds { get; set; } = 300;
            public double Jitter { get; set; } = 0.5;
            public int Parallel { get; set; }
            public string? Out { get; set; }
            public bool PerRun { get; set; }
            public bool List { get; set; }
            public bool Help { get; set; }
        }

        private static Parsed Parse(string[] args)
        {
            var o = new Parsed();
            for (var i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Falta o valor de {args[i]}.");
                switch (args[i])
                {
                    case "--data": o.Data = Next(); break;
                    case "--encounter": o.Encounters.AddRange(Split(Next())); break;
                    case "--strategy": o.Strategies.AddRange(Split(Next())); break;
                    case "--role": AddPair(o.Roles, Next(), "--role"); break;
                    case "--opt": AddPair(o.Options, Next(), "--opt"); break;
                    case "--runs": o.Runs = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--seed": o.Seed = ulong.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--max-seconds": o.MaxSeconds = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--jitter": o.Jitter = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--parallel": o.Parallel = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--out": o.Out = Next(); break;
                    case "--per-run": o.PerRun = true; break;
                    case "--list": o.List = true; break;
                    case "--help" or "-h": o.Help = true; break;
                    default: throw new ArgumentException($"Opção desconhecida: {args[i]}. Use --help.");
                }
            }

            if (o.Strategies.Count == 0)
            {
                o.Strategies.Add("rotation");
            }

            if (o.Runs < 1)
            {
                throw new ArgumentException("--runs deve ser >= 1.");
            }

            return o;
        }

        private static IEnumerable<string> Split(string v) =>
            v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static void AddPair(Dictionary<string, string> into, string pair, string flag)
        {
            var idx = pair.IndexOf('=', StringComparison.Ordinal);
            if (idx <= 0)
            {
                throw new ArgumentException($"{flag} espera chave=valor, recebeu '{pair}'.");
            }

            into[pair[..idx]] = pair[(idx + 1)..];
        }
    }
}
