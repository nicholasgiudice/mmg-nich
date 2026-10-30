using Game.Core.CombatLog;
using Game.Core.Definitions;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>Cores e nomes exibidos no HUD (somente apresentação).</summary>
public static class HudTheme
{
    public static readonly Color Health = new(0.25f, 0.75f, 0.30f);
    public static readonly Color HealthLow = new(0.85f, 0.25f, 0.20f);
    public static readonly Color Shield = new(0.85f, 0.85f, 0.95f, 0.75f);
    public static readonly Color Cast = new(0.95f, 0.75f, 0.25f);
    public static readonly Color CastEnemy = new(0.90f, 0.35f, 0.25f);
    public static readonly Color Panel = new(0.06f, 0.06f, 0.08f, 0.82f);
    public static readonly Color BarBack = new(0.12f, 0.12f, 0.14f, 0.95f);
    public static readonly Color Removable = new(0.55f, 0.95f, 0.55f);
    public static readonly Color Xp = new(0.70f, 0.45f, 0.95f);
    public static readonly Color Suggested = new(1f, 0.85f, 0.45f);
    public static readonly Color Warning = new(1f, 0.55f, 0.4f);

    public static Color Resource(string type) => Key(type) switch
    {
        "vigor" => new Color(0.90f, 0.45f, 0.20f),
        "focus" => new Color(0.85f, 0.85f, 0.30f),
        "mana" => new Color(0.30f, 0.50f, 0.95f),
        "faith" => new Color(0.95f, 0.85f, 0.55f),
        _ => new Color(0.6f, 0.6f, 0.6f),
    };

    public static string ResourceName(string type) => Key(type) switch
    {
        "vigor" => "Vigor",
        "focus" => "Foco",
        "mana" => "Mana",
        "faith" => "Fé",
        var other => other,
    };

    public static Color School(DamageSchool school) => school switch
    {
        DamageSchool.Physical => new Color(1.00f, 0.95f, 0.90f),
        DamageSchool.Arcane => new Color(0.80f, 0.55f, 1.00f),
        DamageSchool.Fire => new Color(1.00f, 0.55f, 0.20f),
        DamageSchool.Frost => new Color(0.55f, 0.85f, 1.00f),
        DamageSchool.Holy => new Color(1.00f, 0.90f, 0.45f),
        DamageSchool.Nature => new Color(0.55f, 0.90f, 0.35f),
        _ => Colors.White,
    };

    public static Color Heal => new(0.40f, 1.00f, 0.45f);

    public static Color Category(LogCategory c) => c switch
    {
        LogCategory.Dano => new Color(1.00f, 0.60f, 0.55f),
        LogCategory.Cura => new Color(0.55f, 1.00f, 0.60f),
        LogCategory.Status => new Color(0.75f, 0.75f, 1.00f),
        LogCategory.Recurso => new Color(0.95f, 0.85f, 0.50f),
        LogCategory.Fase => new Color(1.00f, 0.50f, 1.00f),
        LogCategory.Morte => new Color(1.00f, 0.35f, 0.35f),
        LogCategory.Progressao => Xp,
        _ => new Color(0.85f, 0.85f, 0.85f),
    };

    private static string Key(string type) => type.StartsWith("resource.", StringComparison.Ordinal) ? type["resource.".Length..] : type;
}
