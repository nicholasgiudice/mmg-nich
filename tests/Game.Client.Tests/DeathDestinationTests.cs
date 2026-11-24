using Game.Core.Definitions;
using MmoRpgClient.Match;

namespace Game.Client.Tests;

/// <summary>T-541: o destino mostrado na tela de morte vem do <c>respawn</c> da zona (T-501).</summary>
public class DeathDestinationTests
{
    private static ZoneDefinition Zone(string id, string name, ZoneKind kind, string? respawn = null) =>
        new() { Id = id, Name = name, Arena = "map.x", Kind = kind, Respawn = respawn };

    private static readonly IReadOnlyDictionary<string, ZoneDefinition> Zones = new[]
    {
        Zone("zone.city", "Cidade-Fortaleza", ZoneKind.Safe),
        Zone("zone.pass_fort", "Forte do Passo", ZoneKind.Safe),
        Zone("zone.lower_forest", "Floresta Baixa", ZoneKind.Field),
        Zone("zone.serra", "Serra", ZoneKind.Field, "zone.pass_fort"),
        Zone("zone.other_safe", "Posto Avançado", ZoneKind.Safe),
    }.ToDictionary(z => z.Id);

    [Fact]
    public void A_zone_without_respawn_returns_to_the_city()
    {
        var id = DeathDestination.ZoneId(Zones["zone.lower_forest"]);
        Assert.Equal("zone.city", id);
        Assert.Equal("Retornar à Cidade", DeathDestination.Button(DeathDestination.Phrase(id, Zones)));
    }

    [Fact]
    public void A_zone_with_respawn_returns_to_the_fort()
    {
        var id = DeathDestination.ZoneId(Zones["zone.serra"]);
        Assert.Equal("zone.pass_fort", id);
        var phrase = DeathDestination.Phrase(id, Zones);
        Assert.Equal("Retornar ao Forte do Passo", DeathDestination.Button(phrase));
        Assert.Contains("ao Forte do Passo", DeathDestination.Note(phrase));
    }

    [Fact]
    public void A_safe_zone_not_in_the_list_uses_its_name_and_an_unknown_one_falls_back_to_the_city()
    {
        Assert.Equal("para Posto Avançado", DeathDestination.Phrase("zone.other_safe", Zones));
        Assert.Equal("à Cidade", DeathDestination.Phrase("zone.nowhere", Zones));
        Assert.Equal("zone.city", DeathDestination.ZoneId(null));
    }
}
