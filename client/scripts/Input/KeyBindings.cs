using System.Globalization;

namespace MmoRpgClient.Input;

/// <summary>De onde vem um atalho: tecla do teclado ou botão do mouse.</summary>
public enum BindingDevice
{
    Key,
    Mouse,
}

/// <summary>
/// Um atalho configurável: a tecla física (código do <c>Key</c> do Godot, posição no teclado) ou o botão do mouse, com
/// Shift, Ctrl e Alt. Sem Godot, para os testes; quem converte para o Input Map é o <c>KeyBindingSettings</c>.
/// </summary>
public readonly record struct KeyBinding(BindingDevice Device, long Code, bool Shift = false, bool Ctrl = false, bool Alt = false)
{
    // Códigos do Key do Godot usados aqui (os mesmos valores do enum, sem depender dele).
    public const long Space = 32;
    public const long Escape = 4194305;
    public const long Tab = 4194306;
    public const long Backspace = 4194308;
    public const long Enter = 4194309;
    public const long Left = 4194319;
    public const long Up = 4194320;
    public const long Right = 4194321;
    public const long Down = 4194322;
    public const long ShiftKey = 4194325;
    public const long CtrlKey = 4194326;
    public const long MetaKey = 4194327;
    public const long AltKey = 4194328;
    public const long F1 = 4194332;

    // Botões do mouse (MouseButton do Godot).
    public const long MouseLeft = 1;
    public const long MouseRight = 2;
    public const long MouseMiddle = 3;
    public const long MouseBack = 8;
    public const long MouseForward = 9;

    public static KeyBinding Key(long code, bool shift = false, bool ctrl = false, bool alt = false) =>
        new(BindingDevice.Key, code, shift, ctrl, alt);

    public static KeyBinding Mouse(long button, bool shift = false, bool ctrl = false, bool alt = false) =>
        new(BindingDevice.Mouse, button, shift, ctrl, alt);

    /// <summary>Shift, Ctrl, Alt ou Meta sozinhos: só servem de modificador, nunca de atalho.</summary>
    public bool IsModifierKey => Device == BindingDevice.Key && Code is ShiftKey or CtrlKey or MetaKey or AltKey;

    /// <summary>Texto guardado no <c>settings.cfg</c>: <c>key:49</c>, <c>key:49+shift</c>, <c>mouse:8+ctrl</c>.</summary>
    public string Serialize()
    {
        var text = (Device == BindingDevice.Key ? "key:" : "mouse:") + Code.ToString(CultureInfo.InvariantCulture);
        return text + (Shift ? "+shift" : "") + (Ctrl ? "+ctrl" : "") + (Alt ? "+alt" : "");
    }

    /// <summary>Lê o texto de <see cref="Serialize"/>; null se não for um atalho válido.</summary>
    public static KeyBinding? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Trim().Split('+');
        var head = parts[0].Split(':');
        if (head.Length != 2 || !long.TryParse(head[1], NumberStyles.None, CultureInfo.InvariantCulture, out var code) || code <= 0)
        {
            return null;
        }

        BindingDevice device;
        switch (head[0])
        {
            case "key":
                device = BindingDevice.Key;
                break;
            case "mouse":
                device = BindingDevice.Mouse;
                break;
            default:
                return null;
        }

        bool shift = false, ctrl = false, alt = false;
        foreach (var modifier in parts.Skip(1))
        {
            switch (modifier)
            {
                case "shift":
                    shift = true;
                    break;
                case "ctrl":
                    ctrl = true;
                    break;
                case "alt":
                    alt = true;
                    break;
                default:
                    return null;
            }
        }

        return new KeyBinding(device, code, shift, ctrl, alt);
    }

    /// <summary>
    /// Nome para a tela de teclas e os textos: <c>Shift+1</c>, <c>Ctrl+E</c>, <c>Espaço</c>, <c>Mouse 4</c>.
    /// <paramref name="keyName"/> dá o nome das teclas que não estão na tabela daqui (símbolos no layout do teclado,
    /// teclas especiais); sem ele, <c>Tecla N</c>.
    /// </summary>
    public string Label(Func<long, string?>? keyName = null) => Modifiers("Shift+", "Ctrl+", "Alt+") + BaseName(keyName, shortName: false);

    /// <summary>Nome curto para o canto do espaço da barra (29 px): <c>1</c>, <c>S+1</c>, <c>C+E</c>, <c>M4</c>, <c>Esp</c>.</summary>
    public string ShortLabel(Func<long, string?>? keyName = null) => Modifiers("S+", "C+", "A+") + BaseName(keyName, shortName: true);

    private string Modifiers(string shift, string ctrl, string alt) => (Ctrl ? ctrl : "") + (Alt ? alt : "") + (Shift ? shift : "");

    private string BaseName(Func<long, string?>? keyName, bool shortName)
    {
        if (Device == BindingDevice.Mouse)
        {
            return Code switch
            {
                MouseLeft => shortName ? "M1" : "Clique esquerdo",
                MouseRight => shortName ? "M2" : "Clique direito",
                MouseMiddle => shortName ? "M3" : "Botão do meio",
                MouseBack => shortName ? "M4" : "Mouse 4",
                MouseForward => shortName ? "M5" : "Mouse 5",
                _ => shortName ? $"M{Code}" : $"Mouse {Code}",
            };
        }

        switch (Code)
        {
            case >= 'A' and <= 'Z':
            case >= '0' and <= '9':
                return ((char)Code).ToString();
            case Space:
                return shortName ? "Esp" : "Espaço";
            case Tab:
                return "Tab";
            case Enter:
                return shortName ? "Ent" : "Enter";
            case Backspace:
                return shortName ? "Bks" : "Backspace";
            case Up:
                return shortName ? "Cim" : "Seta para cima";
            case Down:
                return shortName ? "Bai" : "Seta para baixo";
            case Left:
                return shortName ? "Esq" : "Seta para a esquerda";
            case Right:
                return shortName ? "Dir" : "Seta para a direita";
            case >= F1 and < F1 + 12:
                return "F" + (Code - F1 + 1).ToString(CultureInfo.InvariantCulture);
        }

        var name = keyName?.Invoke(Code);
        if (string.IsNullOrEmpty(name))
        {
            name = Code is > 32 and < 127 ? ((char)Code).ToString() : "Tecla " + Code.ToString(CultureInfo.InvariantCulture);
        }

        return shortName && name.Length > 3 ? name[..3] : name;
    }
}

/// <summary>Uma ação que o jogador pode trocar de tecla, com o nome mostrado na tela de teclas.</summary>
public sealed record BindableAction(string Id, string Label, string Group);

/// <summary>Resultado de trocar a tecla de uma ação.</summary>
/// <param name="Ok">A tecla foi aceita.</param>
/// <param name="Swapped">Ação que tinha a tecla e ficou com a antiga da ação trocada; null = nenhuma.</param>
/// <param name="Refusal">Por que a tecla não pode ser usada (reservada); null se aceita.</param>
public readonly record struct BindResult(bool Ok, string? Swapped, string? Refusal);

/// <summary>
/// Teclas do jogador (escolhidas na tela "Teclas" do menu do Esc): uma tecla principal por ação, sem Godot, para os
/// testes. Trocar para uma tecla que já é de outra ação troca as duas de lugar, para nenhuma tecla fazer duas coisas.
/// Esc, os cliques esquerdo e direito e o Ctrl+F (debug) são fixos. Só o que difere do padrão é guardado.
/// </summary>
public sealed class KeyBindings
{
    public const string MovementGroup = "Movimento";
    public const string HotbarGroup = "Barra de atalhos";
    public const string CombatGroup = "Combate";
    public const string ScreensGroup = "Telas";

    /// <summary>As ações configuráveis, na ordem da tela de teclas.</summary>
    public static readonly IReadOnlyList<BindableAction> Actions =
    [
        new(InputActions.MoveUp, "Mover para cima", MovementGroup),
        new(InputActions.MoveDown, "Mover para baixo", MovementGroup),
        new(InputActions.MoveLeft, "Mover para a esquerda", MovementGroup),
        new(InputActions.MoveRight, "Mover para a direita", MovementGroup),
        .. InputActions.AbilitySlots.Select((id, i) => new BindableAction(id, $"Atalho {i + 1}", HotbarGroup)),
        new(InputActions.Consumable, "Poção de vida", HotbarGroup),
        new(InputActions.AttackKey, "Ataque básico", CombatGroup),
        new(InputActions.TargetNext, "Próximo alvo", CombatGroup),
        new(InputActions.TargetPrevious, "Alvo anterior", CombatGroup),
        new(InputActions.PickUp, "Pegar item", CombatGroup),
        new(InputActions.CharacterPanel, "Personagem", ScreensGroup),
        new(InputActions.SkillsPanel, "Habilidades", ScreensGroup),
        new(InputActions.InventoryPanel, "Inventário", ScreensGroup),
        new(InputActions.BattleList, "Lista de batalha", ScreensGroup),
        new(InputActions.ZoneMap, "Mapa da zona", ScreensGroup),
    ];

    private readonly Dictionary<string, KeyBinding?> _defaults = new(StringComparer.Ordinal);
    private readonly Dictionary<string, KeyBinding?> _current = new(StringComparer.Ordinal);

    /// <summary>Alguma tecla mudou (troca, restauração ou carga).</summary>
    public event Action? Changed;

    /// <param name="defaults">Tecla principal de cada ação no projeto (o Input Map do <c>project.godot</c>).</param>
    public KeyBindings(IReadOnlyDictionary<string, KeyBinding?> defaults)
    {
        foreach (var action in Actions)
        {
            _defaults[action.Id] = defaults.GetValueOrDefault(action.Id);
            _current[action.Id] = _defaults[action.Id];
        }
    }

    public static bool IsBindable(string action) => Actions.Any(a => a.Id == action);

    /// <summary>Tecla atual da ação; null = sem tecla (ou ação que não é configurável).</summary>
    public KeyBinding? this[string action] => _current.GetValueOrDefault(action);

    public KeyBinding? DefaultOf(string action) => _defaults.GetValueOrDefault(action);

    /// <summary>Todas as teclas iguais ao padrão.</summary>
    public bool IsAllDefault => _current.All(kv => kv.Value == _defaults[kv.Key]);

    /// <summary>Por que a tecla não pode ser atalho; null = pode.</summary>
    public static string? Reserved(KeyBinding binding) => binding switch
    {
        { IsModifierKey: true } => "Shift, Ctrl e Alt sozinhos não servem: use junto com outra tecla.",
        { Device: BindingDevice.Key, Code: KeyBinding.Escape } => "Esc é fixo: limpa o alvo e abre o menu.",
        { Device: BindingDevice.Key, Code: 'F', Ctrl: true, Shift: false, Alt: false } => "Ctrl+F é fixo (informações de depuração).",
        { Device: BindingDevice.Mouse, Code: KeyBinding.MouseLeft } => "O clique esquerdo é fixo: ataca e seleciona.",
        { Device: BindingDevice.Mouse, Code: KeyBinding.MouseRight } => "O clique direito é fixo: abre os menus dos jogadores e da barra.",
        { Device: BindingDevice.Mouse, Code: >= 4 and <= 7 } => "A roda do mouse não serve de atalho.",
        _ => null,
    };

    /// <summary>Ação que usa esta tecla; null = nenhuma.</summary>
    public string? OwnerOf(KeyBinding binding) => _current.FirstOrDefault(kv => kv.Value == binding).Key;

    /// <summary>
    /// Põe a tecla na ação. Se outra ação já a usava, ela fica com a tecla antiga desta (as duas trocam de lugar).
    /// Tecla reservada é recusada e nada muda.
    /// </summary>
    public BindResult Set(string action, KeyBinding binding)
    {
        if (!_current.TryGetValue(action, out var old))
        {
            return new BindResult(false, null, "Ação desconhecida.");
        }

        if (Reserved(binding) is { } refusal)
        {
            return new BindResult(false, null, refusal);
        }

        var owner = OwnerOf(binding);
        if (owner == action)
        {
            return new BindResult(true, null, null);
        }

        _current[action] = binding;
        if (owner is not null)
        {
            _current[owner] = old;
        }

        Changed?.Invoke();
        return new BindResult(true, owner, null);
    }

    /// <summary>Deixa a ação sem tecla.</summary>
    public void Clear(string action)
    {
        if (_current.TryGetValue(action, out var old) && old is not null)
        {
            _current[action] = null;
            Changed?.Invoke();
        }
    }

    /// <summary>Volta todas as ações às teclas do projeto.</summary>
    public void ResetAll()
    {
        foreach (var action in _defaults.Keys)
        {
            _current[action] = _defaults[action];
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// O que guardar: só as ações com tecla diferente do padrão (<c>none</c> = sem tecla). Assim, mudar o padrão do
    /// projeto chega a quem nunca mexeu naquela ação.
    /// </summary>
    public IReadOnlyList<(string Action, string Value)> Overrides() =>
        Actions.Where(a => _current[a.Id] != _defaults[a.Id])
            .Select(a => (a.Id, _current[a.Id]?.Serialize() ?? "none"))
            .ToList();

    /// <summary>
    /// Aplica o que foi guardado por <see cref="Overrides"/>. Ação desconhecida, texto inválido e tecla reservada são
    /// ignorados. Se duas ações acabarem com a mesma tecla (arquivo editado à mão, padrão novo), fica com a primeira da
    /// lista e a outra fica sem tecla.
    /// </summary>
    public void Load(IEnumerable<(string Action, string Value)> saved)
    {
        foreach (var (action, value) in saved)
        {
            if (!_current.ContainsKey(action))
            {
                continue;
            }

            if (value == "none")
            {
                _current[action] = null;
            }
            else if (KeyBinding.Parse(value) is { } binding && Reserved(binding) is null)
            {
                _current[action] = binding;
            }
        }

        var seen = new HashSet<KeyBinding>();
        foreach (var action in Actions)
        {
            if (_current[action.Id] is { } binding && !seen.Add(binding))
            {
                _current[action.Id] = null;
            }
        }

        Changed?.Invoke();
    }
}
