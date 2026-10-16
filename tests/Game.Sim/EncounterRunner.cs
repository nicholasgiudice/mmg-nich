using System.Numerics;
using Game.Bots;
using Game.Core.Combat;
using Game.Core.Combat.Resources;
using Game.Core.Definitions;
using Game.Core.Events;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;

namespace Game.Sim;

/// <summary>O que rodar: encontro, estratégia por classe, opções dos bots, seeds e limite de tempo.</summary>
public sealed record RunSpec
{
    public required string EncounterId { get; init; }

    /// <summary>Estratégia padrão para todo jogador (ex.: <c>rotation</c>, <c>rotation+dodge</c>).</summary>
    public string Strategy { get; init; } = "rotation";

    /// <summary>Papel por classe (<c>class.cleric</c> -> <c>heal_only</c>); a chave <c>*</c> é o padrão. Tem prioridade sobre <see cref="Strategy"/>.</summary>
    public IReadOnlyDictionary<string, string> Roles { get; init; } = new Dictionary<string, string>();

    public BotOptions Options { get; init; } = BotOptions.Empty;
    public ulong BaseSeed { get; init; } = 1;
    public int Runs { get; init; } = 100;

    /// <summary>Limite de tempo em segundos quando o encontro não define <c>defeat.timeLimit</c>.</summary>
    public double MaxSeconds { get; init; } = 300;

    /// <summary>Deslocamento aleatório máximo (tiles, por eixo) das posições iniciais, sorteado pela seed da execução. 0 desliga.</summary>
    public double SpawnJitter { get; init; } = 0.5;

    /// <summary>Fonte de perigos para estratégias <c>+dodge</c> (a T-016 liga a dos telegráfos). Deve ser thread-safe/sem estado entre mundos.</summary>
    public Func<IHazardSource>? HazardSourceFactory { get; init; }

    public string StrategyFor(string classId) =>
        Roles.TryGetValue(classId, out var s) ? s : Roles.TryGetValue("*", out var all) ? all : Strategy;

    /// <summary>Rótulo para relatórios: a estratégia padrão mais os papéis específicos.</summary>
    public string Label =>
        Roles.Count == 0 ? Strategy : $"{Strategy} [{string.Join(", ", Roles.OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}={r.Value}"))}]";
}

public enum RunOutcome
{
    Win,
    Loss,
    Timeout,
}

/// <summary>Medidas de uma execução (um mundo, uma seed).</summary>
public sealed record RunResult
{
    public required ulong Seed { get; init; }
    public required RunOutcome Outcome { get; init; }
    public required int Ticks { get; init; }
    public double Seconds => Ticks / (double)World.TickRate;

    /// <summary>Dano total recebido por jogadores (inclui o absorvido por escudos).</summary>
    public double DamageTaken { get; init; }
    public double DamageDealt { get; init; }

    /// <summary>Cura efetiva feita por jogadores e cura excedente (acima da vida máxima).</summary>
    public double Healing { get; init; }
    public double Overheal { get; init; }

    public double ResourceGained { get; init; }
    public double ResourceSpent { get; init; }

    /// <summary>Recurso desperdiçado acima do máximo: exato para ganhos de habilidades e parcial para regras (ver README).</summary>
    public double ResourceWasted { get; init; }

    /// <summary>Ticks-jogador com o recurso no máximo (indicador de geração desperdiçada).</summary>
    public int TicksAtResourceCap { get; init; }

    public int PlayerDeaths { get; init; }
    public int EnemyDeaths { get; init; }
    public int PhaseChanges { get; init; }
    public IReadOnlyDictionary<string, int> AbilityUses { get; init; } = new Dictionary<string, int>();

    /// <summary>Distância total andada pelos jogadores (tiles) e parcela dos usos de habilidade feitos parado.</summary>
    public double DistanceMoved { get; init; }
    public double StationaryUseShare { get; init; }

    public ulong StateHash { get; init; }
}

public static class EncounterRunner
{
    public static RunResult Run(ContentCatalog content, RunSpec spec, ulong seed)
    {
        var encounter = content.Encounters.TryGetValue(spec.EncounterId, out var enc)
            ? enc
            : throw new ArgumentException($"Encontro desconhecido: {spec.EncounterId}");
        var setup = EncounterSetup.Create(seed, content, encounter);
        var world = setup.World;
        ApplyJitter(setup, seed, spec.SpawnJitter);

        var director = new BotDirector(world, spec.HazardSourceFactory?.Invoke());
        foreach (var p in setup.Players)
        {
            var classId = p.Get<UnitInfo>().DefinitionId;
            director.Add(p, BotStrategies.Create(spec.StrategyFor(classId)), spec.Options);
        }

        var playerIds = setup.Players.Select(p => p.Id).ToHashSet();
        var maxTicks = encounter.Defeat.TimeLimitTicks ?? (int)Math.Round(spec.MaxSeconds * World.TickRate);
        var m = new Metrics(content, playerIds);
        var lastPos = setup.Players.ToDictionary(p => p.Id, p => p.Get<Transform>().Position);

        var outcome = RunOutcome.Timeout;
        while (world.Tick < maxTicks)
        {
            director.Tick();
            world.Step();
            m.Consume(world, setup.Players, lastPos);

            var playersAlive = setup.Players.Any(p => p.IsAlive);
            var enemiesAlive = world.Entities.Any(e => e.IsAlive && e.TryGet<FactionTag>() is { Faction: Faction.Monster });
            if (!playersAlive)
            {
                outcome = RunOutcome.Loss;
                break;
            }

            if (!enemiesAlive)
            {
                outcome = RunOutcome.Win;
                break;
            }
        }

        return m.Build(seed, outcome, (int)world.Tick, world.ComputeStateHash());
    }

    /// <summary>Varia as posições iniciais de forma determinística por seed (o simulador não é Core; usa Random com seed explícita).</summary>
    private static void ApplyJitter(EncounterWorld setup, ulong seed, double jitter)
    {
        if (jitter <= 0)
        {
            return;
        }

        var rng = new Random(unchecked((int)(seed ^ (seed >> 32)) ^ 0x5bd1e995));
        var map = setup.World.Map;
        foreach (var e in setup.Players.Concat(setup.Enemies))
        {
            var t = e.Get<Transform>();
            var moved = t.Position + new Vector2((float)(((rng.NextDouble() * 2) - 1) * jitter), (float)(((rng.NextDouble() * 2) - 1) * jitter));
            var r = e.TryGet<Collider>()?.Radius ?? 0f;
            if (map is null || (!map.IsBlocked(moved) && map.HasLineOfSight(t.Position, moved, r)))
            {
                t.Position = moved;
            }
        }
    }

    private sealed class Metrics(ContentCatalog content, HashSet<EntityId> players)
    {
        private readonly Dictionary<string, int> _uses = new(StringComparer.Ordinal);
        private double _taken, _dealt, _heal, _overheal, _gained, _spent, _wasted, _moved;
        private int _atCap, _playerDeaths, _enemyDeaths, _phases, _stationaryUses, _totalUses;
        private readonly Dictionary<(EntityId, string), double> _expectedAbilityGain = new();
        private readonly Dictionary<(EntityId, string), double> _actualAbilityGain = new();

        public void Consume(World world, IReadOnlyList<Entity> playerEntities, Dictionary<EntityId, Vector2> lastPos)
        {
            foreach (var ev in world.LastTickEvents)
            {
                switch (ev)
                {
                    case DamageDealt d:
                        if (players.Contains(d.Target))
                        {
                            _taken += d.Amount;
                        }

                        if (players.Contains(d.Source))
                        {
                            _dealt += d.Amount;
                        }

                        break;

                    case HealApplied h when players.Contains(h.Source):
                        _heal += h.Effective;
                        _overheal += h.Overheal;
                        break;

                    case ResourceChanged r when players.Contains(r.Entity):
                        OnResource(r);
                        break;

                    case AbilityExecuted a when players.Contains(a.Source):
                        OnExecuted(world, a);
                        break;

                    case EntityDied died:
                        if (players.Contains(died.Entity))
                        {
                            _playerDeaths++;
                        }
                        else
                        {
                            _enemyDeaths++;
                        }

                        break;

                    default:
                        // Eventos de fase de boss (T-012) são identificados pelo nome do tipo, sem depender dele.
                        if (ev.GetType().Name.Contains("Phase", StringComparison.Ordinal))
                        {
                            _phases++;
                        }

                        break;
                }
            }

            foreach (var p in playerEntities)
            {
                var pos = p.Get<Transform>().Position;
                _moved += Vector2.Distance(pos, lastPos[p.Id]);
                lastPos[p.Id] = pos;
                if (p.IsAlive && p.TryGet<ResourcePool>() is { Max: > 0 } pool && pool.Current >= pool.Max)
                {
                    _atCap++;
                }
            }
        }

        private void OnExecuted(World world, AbilityExecuted a)
        {
            _uses[a.AbilityId] = _uses.GetValueOrDefault(a.AbilityId) + 1;
            _totalUses++;
            if (world.Find(a.Source) is { } e && e.TryGet<Movement>() is { } mv && mv.Direction == Vector2.Zero)
            {
                _stationaryUses++;
            }

            // Ganho esperado de recurso por habilidade (efeito "resource" positivo): permite medir o desperdício exato.
            if (content.Abilities.TryGetValue(a.AbilityId, out var def))
            {
                foreach (var fx in def.Effects.OfType<ResourceEffect>().Where(f => f.Amount > 0))
                {
                    var key = (a.Source, a.AbilityId);
                    _expectedAbilityGain[key] = _expectedAbilityGain.GetValueOrDefault(key) + fx.Amount;
                }
            }
        }

        private void OnResource(ResourceChanged r)
        {
            if (r.Delta > 0)
            {
                _gained += r.Delta;
                if (r.Reason == ResourceChangeReason.Effect)
                {
                    var key = (r.Entity, r.SourceId);
                    _actualAbilityGain[key] = _actualAbilityGain.GetValueOrDefault(key) + r.Delta;
                }
                else if (r.Reason == ResourceChangeReason.Rule)
                {
                    _wasted += RuleTruncation(r);
                }
            }
            else if (r.Delta < 0 && r.Reason != ResourceChangeReason.Regen)
            {
                _spent += -r.Delta;
            }
        }

        /// <summary>Ganho por regra truncado pelo máximo (só regras de quantidade fixa: o valor esperado é conhecido).</summary>
        private double RuleTruncation(ResourceChanged r)
        {
            // Sem acesso à entidade aqui; a classe é buscada pelo id da regra em todas as classes (ids de regra são por classe).
            foreach (var cls in content.Classes.Values)
            {
                var rule = cls.ResourceRules.FirstOrDefault(x => x.Id == r.SourceId);
                if (rule is { Ratio: 0, Amount: > 0 })
                {
                    return Math.Max(0, rule.Amount - r.Delta);
                }
            }

            return 0;
        }

        public RunResult Build(ulong seed, RunOutcome outcome, int ticks, ulong hash)
        {
            var wasted = _wasted;
            foreach (var (key, expected) in _expectedAbilityGain)
            {
                wasted += Math.Max(0, expected - _actualAbilityGain.GetValueOrDefault(key));
            }

            return new RunResult
            {
                Seed = seed,
                Outcome = outcome,
                Ticks = ticks,
                DamageTaken = _taken,
                DamageDealt = _dealt,
                Healing = _heal,
                Overheal = _overheal,
                ResourceGained = _gained,
                ResourceSpent = _spent,
                ResourceWasted = wasted,
                TicksAtResourceCap = _atCap,
                PlayerDeaths = _playerDeaths,
                EnemyDeaths = _enemyDeaths,
                PhaseChanges = _phases,
                AbilityUses = new SortedDictionary<string, int>(_uses, StringComparer.Ordinal),
                DistanceMoved = _moved,
                StationaryUseShare = _totalUses == 0 ? 0 : _stationaryUses / (double)_totalUses,
                StateHash = hash,
            };
        }
    }
}
