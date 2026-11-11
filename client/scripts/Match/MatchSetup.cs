using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
using Game.Core.Simulation.Components;
using MmoRpgClient.Authority;
using GridMap = Game.Core.Spatial.GridMap;

namespace MmoRpgClient.Match;

/// <summary>Partida montada: autoridade local (com bots nos outros jogadores), mapa e encontro.</summary>
public sealed record MatchWorld(LocalAuthority Authority, GridMap Map, EncounterDefinition Encounter, string PlayerClass, int BotCount);

/// <summary>
/// Monta uma partida do protótipo a partir das escolhas do menu. Encontro solo: a classe escolhida substitui
/// a do encontro. Encontro de grupo: o jogador controla a classe escolhida e os demais são bots de <c>Game.Bots</c>.
/// </summary>
public static class MatchSetup
{
    /// <summary>
    /// Estratégia dos bots aliados. A tarefa sugeria <c>rotation</c>, mas no simulador um grupo em
    /// <c>rotation</c> nunca vence a Matriarca; com <c>kite+dodge</c> (o grupo "que se move" da T-016)
    /// 3 bots + jogador parado vencem ~80%.
    /// </summary>
    public const string BotStrategy = "kite+dodge";

    private static readonly BotOptions BotSettings = new(new Dictionary<string, string>
    {
        ["class.mage:explosion"] = "mage.elemental_blast",
    });

    /// <param name="enemyLevel">
    /// Depuração (T-345, <c>--enemy-level=n</c>): nível de todos os inimigos e objetivos do encontro, para ver as fases de um
    /// chefe com o grupo do nível 1. Null = os níveis do encontro.
    /// </param>
    /// <param name="enemyHealth">
    /// Depuração (T-345, <c>--enemy-health=f</c>): os inimigos começam com esta fração da vida (0..1), para ver as fases
    /// finais de um chefe. Null = vida cheia.
    /// </param>
    public static MatchWorld Create(ContentCatalog content, MatchSettings settings, int? enemyLevel = null, float? enemyHealth = null)
    {
        if (!content.Encounters.TryGetValue(settings.EncounterId, out var encounter))
        {
            throw new InvalidOperationException($"Encontro desconhecido: {settings.EncounterId}");
        }

        var playerClass = settings.ClassId is { } chosen && content.Classes.ContainsKey(chosen)
            ? chosen
            : encounter.Players[0].Class;

        var controlledIndex = encounter.Players.ToList().FindIndex(p => p.Class == playerClass);
        if (controlledIndex < 0)
        {
            // Classe fora do encontro: ocupa a primeira vaga (solo = troca de classe).
            controlledIndex = 0;
            var players = encounter.Players.ToList();
            players[0] = players[0] with { Class = playerClass };
            encounter = encounter with { Players = players };
        }

        // T-064: NH escolhidos no menu (ou nos argumentos) valem só para o jogador; os bots ficam no NH1.
        var withRanks = encounter.Players.ToList();
        withRanks[controlledIndex] = withRanks[controlledIndex] with
        {
            Ranks = SkillPlan.For(content.Classes[playerClass], content).ToSpawnRanks(),
        };
        encounter = encounter with { Players = withRanks };

        if (enemyLevel is { } level)
        {
            encounter = encounter with
            {
                Enemies = encounter.Enemies.Select(e => e with { Level = level }).ToList(),
                Objectives = encounter.Objectives.Select(o => o with { Level = level }).ToList(),
            };
        }

        var setup = EncounterSetup.Create(settings.Seed, content, encounter);
        var controlled = setup.Players[controlledIndex];
        if (enemyHealth is { } fraction)
        {
            foreach (var enemy in setup.World.Entities.Where(e => e.TryGet<FactionTag>()?.Faction == Faction.Monster && e.Has<Health>()).ToList())
            {
                var health = enemy.Get<Health>();
                health.Current = Math.Max(1, health.Max * Math.Clamp(fraction, 0f, 1f));
            }
        }

        var director = new BotDirector(setup.World, new TelegraphHazardSource()) { Seed = settings.Seed };
        foreach (var bot in setup.Players.Where(p => p.Id != controlled.Id))
        {
            director.Add(bot, BotStrategies.Create(BotStrategy), BotSettings);
        }

        var map = setup.World.Map ?? EntityFactory.BuildMap(content.Arenas[encounter.Arena]);
        var authority = new LocalAuthority(setup.World, controlled.Id, director.Tick);
        return new MatchWorld(authority, map, encounter, playerClass, director.Bots.Count);
    }

    /// <summary>Classe sugerida de um encontro (a do primeiro jogador).</summary>
    public static string SuggestedClass(EncounterDefinition encounter) => encounter.Players[0].Class;
}
