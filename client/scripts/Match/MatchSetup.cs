using Game.Bots;
using Game.Core.Combat;
using Game.Core.Definitions;
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

    public static MatchWorld Create(ContentCatalog content, MatchSettings settings)
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

        var setup = EncounterSetup.Create(settings.Seed, content, encounter);
        var controlled = setup.Players[controlledIndex];
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
