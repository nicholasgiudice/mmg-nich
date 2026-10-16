namespace MmoRpgClient.Input;

/// <summary>Nomes das ações do Input Map (definidas em project.godot; remapeáveis no editor).</summary>
public static class InputActions
{
    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Attack = "attack";
    public const string TargetNext = "target_next";
    public const string TargetClear = "target_clear";

    /// <summary>Primeiro consumível da classe (poção), tecla Q.</summary>
    public const string Consumable = "consumable_1";

    /// <summary>Ações das habilidades da classe, na ordem dos slots (1, 2, 3, 4).</summary>
    public static readonly string[] AbilitySlots = ["ability_1", "ability_2", "ability_3", "ability_4"];

    public static IEnumerable<string> All =>
        new[] { MoveUp, MoveDown, MoveLeft, MoveRight, Attack, TargetNext, TargetClear, Consumable }.Concat(AbilitySlots);
}
