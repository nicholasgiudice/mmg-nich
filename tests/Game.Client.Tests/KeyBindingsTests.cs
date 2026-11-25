using MmoRpgClient.Input;

namespace Game.Client.Tests;

/// <summary>Teclas configuráveis: troca com conflito, teclas fixas, o que é guardado e os nomes mostrados.</summary>
public class KeyBindingsTests
{
    private static KeyBindings Defaults()
    {
        var defaults = new Dictionary<string, KeyBinding?>
        {
            [InputActions.MoveUp] = KeyBinding.Key('W'),
            [InputActions.Consumable] = KeyBinding.Key('Q'),
            [InputActions.PickUp] = KeyBinding.Key('F'),
            [InputActions.TargetNext] = KeyBinding.Key(KeyBinding.Tab),
            [InputActions.TargetPrevious] = KeyBinding.Key(KeyBinding.Tab, shift: true),
        };
        for (var i = 0; i < InputActions.AbilitySlots.Length; i++)
        {
            defaults[InputActions.AbilitySlots[i]] = KeyBinding.Key('0' + (i + 1) % 10);
        }

        return new KeyBindings(defaults);
    }

    [Fact]
    public void Starts_with_the_project_keys()
    {
        var keys = Defaults();

        Assert.Equal(KeyBinding.Key('1'), keys["ability_1"]);
        Assert.Equal(KeyBinding.Key('0'), keys["ability_10"]);
        Assert.Null(keys[InputActions.ZoneMap]); // sem padrão no teste: sem tecla
        Assert.True(keys.IsAllDefault);
        Assert.Empty(keys.Overrides());
    }

    [Fact]
    public void Default_bar_labels_go_from_one_to_nine_and_then_zero()
    {
        var keys = Defaults();

        Assert.Equal(["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"],
            InputActions.AbilitySlots.Select(a => keys[a]!.Value.ShortLabel()));
    }

    [Fact]
    public void A_free_key_just_moves()
    {
        var keys = Defaults();

        var result = keys.Set("ability_6", KeyBinding.Key('1', shift: true));

        Assert.True(result.Ok);
        Assert.Null(result.Swapped);
        Assert.Equal(KeyBinding.Key('1', shift: true), keys["ability_6"]);
        Assert.Equal(KeyBinding.Key('1'), keys["ability_1"]); // Shift+1 e 1 são teclas diferentes
    }

    [Fact]
    public void A_key_in_use_swaps_the_two_actions()
    {
        var keys = Defaults();

        var result = keys.Set("ability_6", KeyBinding.Key('Q'));

        Assert.True(result.Ok);
        Assert.Equal(InputActions.Consumable, result.Swapped);
        Assert.Equal(KeyBinding.Key('Q'), keys["ability_6"]);
        Assert.Equal(KeyBinding.Key('6'), keys[InputActions.Consumable]); // a poção fica com o 6
        Assert.Equal(InputActions.Consumable, keys.OwnerOf(KeyBinding.Key('6')));
    }

    [Theory]
    [InlineData(BindingDevice.Key, KeyBinding.Escape, false, false)]
    [InlineData(BindingDevice.Key, 'F', false, true)] // Ctrl+F
    [InlineData(BindingDevice.Key, KeyBinding.ShiftKey, true, false)] // Shift sozinho
    [InlineData(BindingDevice.Mouse, KeyBinding.MouseLeft, false, false)]
    [InlineData(BindingDevice.Mouse, KeyBinding.MouseRight, false, false)]
    [InlineData(BindingDevice.Mouse, 4L, false, false)] // roda
    public void Fixed_keys_are_refused_and_nothing_changes(BindingDevice device, long code, bool shift, bool ctrl)
    {
        var keys = Defaults();

        var result = keys.Set("ability_1", new KeyBinding(device, code, shift, ctrl));

        Assert.False(result.Ok);
        Assert.NotNull(result.Refusal);
        Assert.Equal(KeyBinding.Key('1'), keys["ability_1"]);
    }

    [Fact]
    public void Side_mouse_buttons_and_alt_combinations_are_accepted()
    {
        var keys = Defaults();

        Assert.True(keys.Set("ability_7", KeyBinding.Mouse(KeyBinding.MouseBack)).Ok);
        Assert.True(keys.Set("ability_8", KeyBinding.Key('E', alt: true)).Ok);
        Assert.True(keys.Set("ability_9", KeyBinding.Key('F', ctrl: true, shift: true)).Ok); // só o Ctrl+F puro é fixo
    }

    [Fact]
    public void Only_changes_are_saved_and_they_load_back()
    {
        var keys = Defaults();
        keys.Set("ability_6", KeyBinding.Key('1', shift: true));
        keys.Set("ability_7", KeyBinding.Mouse(KeyBinding.MouseBack, ctrl: true));
        keys.Clear(InputActions.PickUp);

        var saved = keys.Overrides();
        Assert.Equal(3, saved.Count);
        Assert.Contains(("ability_6", "key:49+shift"), saved);
        Assert.Contains(("ability_7", "mouse:8+ctrl"), saved);
        Assert.Contains((InputActions.PickUp, "none"), saved);

        var again = Defaults();
        again.Load(saved);
        Assert.Equal(KeyBinding.Key('1', shift: true), again["ability_6"]);
        Assert.Equal(KeyBinding.Mouse(KeyBinding.MouseBack, ctrl: true), again["ability_7"]);
        Assert.Null(again[InputActions.PickUp]);
        Assert.Equal(KeyBinding.Key('1'), again["ability_1"]);
    }

    [Fact]
    public void Loading_ignores_garbage_and_never_leaves_a_key_on_two_actions()
    {
        var keys = Defaults();

        keys.Load([
            ("ability_6", "key:abc"), // inválido: fica o padrão
            ("unknown_action", "key:69"),
            ("ability_7", "key:4194305"), // Esc é fixo
            ("ability_8", "key:81"), // Q, que também é o padrão da poção
        ]);

        Assert.Equal(KeyBinding.Key('6'), keys["ability_6"]);
        Assert.Equal(KeyBinding.Key('7'), keys["ability_7"]);
        Assert.Equal(KeyBinding.Key('Q'), keys["ability_8"]); // a barra vem antes da poção na lista: fica com o Q
        Assert.Null(keys[InputActions.Consumable]); // e a poção fica sem tecla, em vez de duas ações no Q
    }

    [Fact]
    public void Reset_brings_back_every_project_key()
    {
        var keys = Defaults();
        keys.Set("ability_1", KeyBinding.Key('E'));
        keys.Clear(InputActions.Consumable);

        keys.ResetAll();

        Assert.True(keys.IsAllDefault);
        Assert.Equal(KeyBinding.Key('1'), keys["ability_1"]);
    }

    [Theory]
    [InlineData("key:49", "1", "1")]
    [InlineData("key:49+shift", "Shift+1", "S+1")]
    [InlineData("key:69+ctrl", "Ctrl+E", "C+E")]
    [InlineData("key:32", "Espaço", "Esp")]
    [InlineData("key:4194306+shift", "Shift+Tab", "S+Tab")]
    [InlineData("key:4194333", "F2", "F2")]
    [InlineData("mouse:8", "Mouse 4", "M4")]
    [InlineData("mouse:3+alt", "Alt+Botão do meio", "A+M3")]
    public void Names_for_the_keys_screen_and_the_bar(string saved, string label, string shortLabel)
    {
        var binding = KeyBinding.Parse(saved)!.Value;

        Assert.Equal(label, binding.Label());
        Assert.Equal(shortLabel, binding.ShortLabel());
        Assert.Equal(saved, binding.Serialize());
    }

    [Fact]
    public void Keys_outside_the_table_use_the_keyboard_name()
    {
        var semicolon = KeyBinding.Key(59); // ";" na posição americana; num ABNT2 é o Ç

        Assert.Equal("Ç", semicolon.Label(_ => "Ç"));
        Assert.Equal(";", semicolon.Label());
        Assert.Equal("Pag", KeyBinding.Key(4194323).ShortLabel(_ => "PageUp"));
    }

    [Theory]
    [InlineData(InputActions.MoveUp, false)]
    [InlineData(InputActions.MoveRight, false)]
    [InlineData(InputActions.Attack, false)]
    [InlineData("ability_1", true)]
    [InlineData(InputActions.TargetNext, true)]
    public void Only_movement_ignores_held_modifiers(string action, bool exact)
    {
        // Andar com Shift apertado (para um Shift+1) não pode parar o personagem; o resto exige a combinação exata.
        Assert.Equal(exact, InputActions.ExactMatch(action));
    }
}
