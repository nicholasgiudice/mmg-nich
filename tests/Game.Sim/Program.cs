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
              --ranks <perfil>      NH de todas as classes: nh1 (padrão), focado (5+5+1+1, assinatura 3), espalhado (3+3+3+3, assinatura 3)
              --rank <c>:<hab>=<n>  NH de uma habilidade (repetível, por cima do perfil); ex.: --rank class.archer:archer.power_shot=5
              --level <n>           perfil de personagem (T-206): jogadores com progressão nesse nível (sem a opção: protótipo)
              --attrs <plano>       pontos de atributo do perfil: sugerido (padrão) ou espalhado
              --gear <raridade>     equipamento completo do perfil: comum (padrão), incomum ou raro
              --gear-level <n>      nível dos itens do perfil (padrão: o do personagem)
              --skills <plano>      pontos de habilidade do perfil: focado (padrão) ou espalhado
              --focus <c>=<h1>,<h2> par do NH focado de uma classe (repetível); ex.: --focus class.cleric=cleric.holy_chain,cleric.sacred_shield
              --monster-level <n>   nível de todos os monstros (vida e potência por rules.monsters)
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
                        var ranks = RankProfiles.For(o.RankProfile, content);
                        foreach (var r in o.RankPairs)
                        {
                            RankProfiles.AddPair(ranks, r);
                        }

                        var spec = new RunSpec
                        {
                            Ranks = ranks,
                            Profile = o.Level is { } level
                                ? new CharacterProfile
                                {
                                    Level = level,
                                    Attributes = o.Attrs,
                                    Gear = o.Gear,
                                    GearLevel = o.GearLevel,
                                    Skills = o.Skills,
                                    Focus = o.Focus,
                                }
                                : null,
                            MonsterLevel = o.MonsterLevel,
                            RankLabel = o.RankPairs.Count == 0
                                ? (o.RankProfile == RankProfiles.Nh1 ? "" : o.RankProfile)
                                : $"{o.RankProfile}+{string.Join("+", o.RankPairs)}",
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
            public string RankProfile { get; set; } = RankProfiles.Nh1;
            public List<string> RankPairs { get; } = [];
            public int? Level { get; set; }
            public AttributePlan Attrs { get; set; } = AttributePlan.Suggested;
            public Game.Core.Definitions.ItemRarity Gear { get; set; } = Game.Core.Definitions.ItemRarity.Common;
            public int? GearLevel { get; set; }
            public string Skills { get; set; } = RankProfiles.Focused;
            public int? MonsterLevel { get; set; }
            public Dictionary<string, string[]> Focus { get; } = new(StringComparer.Ordinal);
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
                    case "--ranks": o.RankProfile = Next(); break;
                    case "--rank": o.RankPairs.Add(Next()); break;
                    case "--level": o.Level = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--attrs":
                        o.Attrs = Next() switch
                        {
                            "sugerido" => AttributePlan.Suggested,
                            "espalhado" => AttributePlan.Spread,
                            var v => throw new ArgumentException($"--attrs espera sugerido ou espalhado, recebeu '{v}'."),
                        };
                        break;
                    case "--gear":
                        o.Gear = Next() switch
                        {
                            "comum" => Game.Core.Definitions.ItemRarity.Common,
                            "incomum" => Game.Core.Definitions.ItemRarity.Uncommon,
                            "raro" => Game.Core.Definitions.ItemRarity.Rare,
                            var v => throw new ArgumentException($"--gear espera comum, incomum ou raro, recebeu '{v}'."),
                        };
                        break;
                    case "--gear-level": o.GearLevel = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--skills": o.Skills = Next(); break;
                    case "--focus":
                        {
                            var v = Next();
                            var eq = v.IndexOf('=', StringComparison.Ordinal);
                            var pair = eq > 0 ? Split(v[(eq + 1)..]).ToArray() : [];
                            o.Focus[eq > 0 ? v[..eq] : v] = pair.Length == 2 ? pair : throw new ArgumentException($"--focus espera classe=hab1,hab2, recebeu '{v}'.");
                            break;
                        }

                    case "--monster-level": o.MonsterLevel = int.Parse(Next(), CultureInfo.InvariantCulture); break;
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
