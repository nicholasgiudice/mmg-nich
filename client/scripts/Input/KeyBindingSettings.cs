using Godot;

namespace MmoRpgClient.Input;

/// <summary>
/// Teclas do jogador no Godot: lê o padrão do Input Map do projeto, aplica as trocas guardadas na seção <c>[keys]</c> de
/// <c>user://settings.cfg</c> (o mesmo arquivo dos efeitos e da lista de batalha) e grava cada troca. Só a primeira tecla
/// de cada ação é configurável; as outras do projeto (as setas do movimento) continuam, a não ser que alguma ação use a
/// mesma tecla. As regras ficam em <see cref="KeyBindings"/> (testadas sem Godot).
/// </summary>
public static class KeyBindingSettings
{
    private const string File = "user://settings.cfg";
    private const string Section = "keys";

    private static KeyBindings? s_current;

    /// <summary>Teclas extras do projeto (da segunda em diante) por ação, guardadas antes de mexer no Input Map.</summary>
    private static readonly Dictionary<string, List<InputEvent>> s_extras = new(StringComparer.Ordinal);

    /// <summary>As teclas atuais (carrega e aplica na primeira chamada).</summary>
    public static KeyBindings Current
    {
        get
        {
            if (s_current is null)
            {
                Load();
            }

            return s_current!;
        }
    }

    /// <summary>Garante que as teclas guardadas estão no Input Map (chamar ao abrir a partida).</summary>
    public static void EnsureApplied() => _ = Current;

    /// <summary>Grava as trocas e aplica no Input Map.</summary>
    public static void SaveAndApply()
    {
        var config = new ConfigFile();
        config.Load(File); // sem arquivo ainda: começa vazio
        if (config.HasSection(Section))
        {
            config.EraseSection(Section);
        }

        foreach (var (action, value) in Current.Overrides())
        {
            config.SetValue(Section, action, value);
        }

        config.Save(File);
        Apply(Current);
    }

    /// <summary>Nome da tecla da ação para textos (<c>Shift+1</c>); "sem tecla" quando não há.</summary>
    public static string Label(string action) => Current[action]?.Label(PhysicalName) ?? "sem tecla";

    /// <summary>Nome curto para o canto do espaço da barra (<c>S+1</c>); vazio quando não há tecla.</summary>
    public static string ShortLabel(string action) => Current[action]?.ShortLabel(PhysicalName) ?? "";

    /// <summary>A ação foi apertada neste frame, com a correspondência certa (<see cref="InputActions.ExactMatch"/>).</summary>
    public static bool JustPressed(string action) => Godot.Input.IsActionJustPressed(action, InputActions.ExactMatch(action));

    /// <summary>A tecla ou o botão apertado neste evento como atalho; null se não for um aperto de tecla ou botão.</summary>
    public static KeyBinding? FromEvent(InputEvent e) => e switch
    {
        InputEventKey { Pressed: true, Echo: false } key => KeyBinding.Key(
            (long)(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode),
            key.ShiftPressed, key.CtrlPressed, key.AltPressed),
        InputEventMouseButton { Pressed: true } mouse => KeyBinding.Mouse(
            (long)mouse.ButtonIndex, mouse.ShiftPressed, mouse.CtrlPressed, mouse.AltPressed),
        _ => null,
    };

    /// <summary>Nome da tecla física no layout do teclado do jogador (no ABNT2, a tecla do ";" americano é o Ç).</summary>
    private static string? PhysicalName(long code)
    {
        var label = DisplayServer.KeyboardGetLabelFromPhysical((Key)code);
        var name = OS.GetKeycodeString(label != Key.None ? label : (Key)code);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    private static void Load()
    {
        var defaults = new Dictionary<string, KeyBinding?>(StringComparer.Ordinal);
        foreach (var action in KeyBindings.Actions)
        {
            if (!InputMap.HasAction(action.Id))
            {
                continue;
            }

            var events = InputMap.ActionGetEvents(action.Id);
            defaults[action.Id] = events.Count > 0 ? ToBinding(events[0]) : null;
            s_extras[action.Id] = events.Skip(1).ToList();
        }

        var keys = new KeyBindings(defaults);
        var config = new ConfigFile();
        if (config.Load(File) == Error.Ok && config.HasSection(Section))
        {
            keys.Load(config.GetSectionKeys(Section).Select(k => (k, (string)config.GetValue(Section, k, ""))).ToList());
        }

        s_current = keys;
        Apply(keys);
    }

    private static void Apply(KeyBindings keys)
    {
        var used = KeyBindings.Actions.Select(a => keys[a.Id]).OfType<KeyBinding>().ToHashSet();
        foreach (var action in KeyBindings.Actions)
        {
            if (!InputMap.HasAction(action.Id))
            {
                continue;
            }

            InputMap.ActionEraseEvents(action.Id);
            if (keys[action.Id] is { } binding)
            {
                InputMap.ActionAddEvent(action.Id, ToEvent(binding));
            }

            // As teclas extras do projeto (setas do movimento) ficam, menos as que viraram atalho de outra ação.
            foreach (var extra in s_extras.GetValueOrDefault(action.Id) ?? [])
            {
                if (ToBinding(extra) is not { } e || !used.Contains(e))
                {
                    InputMap.ActionAddEvent(action.Id, extra);
                }
            }
        }
    }

    private static KeyBinding? ToBinding(InputEvent e) => e switch
    {
        InputEventKey key => KeyBinding.Key(
            (long)(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode),
            key.ShiftPressed, key.CtrlPressed, key.AltPressed),
        InputEventMouseButton mouse => KeyBinding.Mouse((long)mouse.ButtonIndex, mouse.ShiftPressed, mouse.CtrlPressed, mouse.AltPressed),
        _ => null,
    };

    private static InputEvent ToEvent(KeyBinding b)
    {
        InputEventWithModifiers e = b.Device == BindingDevice.Key
            ? new InputEventKey { PhysicalKeycode = (Key)b.Code }
            : new InputEventMouseButton { ButtonIndex = (MouseButton)b.Code };
        e.ShiftPressed = b.Shift;
        e.CtrlPressed = b.Ctrl;
        e.AltPressed = b.Alt;
        return e;
    }
}
