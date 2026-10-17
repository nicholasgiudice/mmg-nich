namespace MmoRpgClient.Input;

/// <summary>Nomes das ações do Input Map (definidas em project.godot; remapeáveis no editor).</summary>
public static class InputActions
{
    public const string MoveUp = "move_up";
    public const string MoveDown = "move_down";
    public const string MoveLeft = "move_left";
    public const string MoveRight = "move_right";
    public const string Attack = "attack";

    /// <summary>Ataque básico pelo teclado (Espaço): usa o alvo selecionado (Tab) e mira nele.</summary>
    public const string AttackKey = "attack_key";
    public const string TargetNext = "target_next";
    public const string TargetClear = "target_clear";

    /// <summary>Esc: pausa (quando não há alvo selecionado para limpar).</summary>
    public const string Pause = "pause";

    /// <summary>R: reinicia a partida com a mesma seed.</summary>
    public const string Restart = "restart";

    /// <summary>Primeiro consumível da classe (poção), tecla Q.</summary>
    public const string Consumable = "consumable_1";

    /// <summary>Ações das habilidades da classe, na ordem dos slots (1, 2, 3, 4).</summary>
    public static readonly string[] AbilitySlots = ["ability_1", "ability_2", "ability_3", "ability_4"];

    public static IEnumerable<string> All =>
        new[] { MoveUp, MoveDown, MoveLeft, MoveRight, Attack, AttackKey, TargetNext, TargetClear, Consumable, Pause, Restart }.Concat(AbilitySlots);
}
