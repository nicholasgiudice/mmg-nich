using Game.Core.Combat;

namespace MmoRpgClient.Presentation;

/// <summary>Textos exibidos ao jogador quando a autoridade recusa uma habilidade.</summary>
public static class RejectionText
{
    private static readonly Dictionary<string, string> ResourceNames = new(StringComparer.Ordinal)
    {
        ["vigor"] = "Vigor",
        ["focus"] = "Foco",
        ["mana"] = "Mana",
        ["faith"] = "Fé",
    };

    /// <param name="resourceType">Tipo do recurso do jogador (ex.: "vigor"), para "Sem Vigor".</param>
    public static string For(AbilityRejectReason reason, string? resourceType) => reason switch
    {
        AbilityRejectReason.Dead => "Você está morto",
        AbilityRejectReason.AlreadyCasting => "Já conjurando",
        AbilityRejectReason.CannotAct => "Não pode agir",
        AbilityRejectReason.Stunned => "Atordoado",
        AbilityRejectReason.Silenced => "Silenciado",
        AbilityRejectReason.UnknownAbility => "Habilidade desconhecida",
        AbilityRejectReason.OnCooldown => "Em recarga",
        AbilityRejectReason.NotEnoughResource => $"Sem {ResourceName(resourceType)}",
        AbilityRejectReason.InvalidTarget => "Alvo inválido",
        AbilityRejectReason.OutOfRange => "Fora de alcance",
        AbilityRejectReason.NoLineOfSight => "Sem linha de visão",
        AbilityRejectReason.NoCharges => "Sem cargas",
        _ => reason.ToString(),
    };

    private static string ResourceName(string? type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return "recurso";
        }

        var key = type.StartsWith("resource.", StringComparison.Ordinal) ? type["resource.".Length..] : type;
        return ResourceNames.TryGetValue(key, out var name) ? name : key;
    }
}
