using Game.Core.Definitions;

namespace MmoRpgClient.Match;

/// <summary>
/// Destino de quem morre, para a tela de morte (T-541, de <c>respawn</c> da zona, T-501). O servidor decide onde o
/// personagem renasce; o cliente só lê os mesmos dados para dizer qual é, sem pedir nada.
/// </summary>
public static class DeathDestination
{
    /// <summary>Zona inicial do servidor (<c>--start-zone</c>, padrão): onde renasce quem morre numa zona sem <c>respawn</c>.</summary>
    public const string DefaultZoneId = "zone.city";

    /// <summary>Com o artigo certo, depois de "Retornar": o português pede "à Cidade" e "ao Forte do Passo".</summary>
    private static readonly Dictionary<string, string> Phrases = new(StringComparer.Ordinal)
    {
        ["zone.city"] = "à Cidade",
        ["zone.pass_fort"] = "ao Forte do Passo",
    };

    /// <summary>Id da zona segura onde renasce quem morre em <paramref name="died"/> (sem <c>respawn</c>, a zona inicial).</summary>
    public static string ZoneId(ZoneDefinition? died) => died?.Respawn ?? DefaultZoneId;

    /// <summary>"à Cidade", "ao Forte do Passo"; uma zona que não está na lista sai como "para {nome}".</summary>
    public static string Phrase(string zoneId, IReadOnlyDictionary<string, ZoneDefinition> zones) =>
        Phrases.TryGetValue(zoneId, out var phrase) ? phrase
        : zones.TryGetValue(zoneId, out var zone) ? $"para {zone.Name}"
        : "à Cidade";

    /// <summary>O texto do botão da tela de morte.</summary>
    public static string Button(string phrase) => $"Retornar {phrase}";

    /// <summary>A nota embaixo do resumo.</summary>
    public static string Note(string phrase) => $"Você volta {phrase} com vida e Mana cheias. Nada é perdido.";
}
