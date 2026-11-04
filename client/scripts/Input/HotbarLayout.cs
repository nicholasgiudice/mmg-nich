namespace MmoRpgClient.Input;

/// <summary>
/// Barra de atalhos 1–6 configurável pelo jogador (T-081): o id da habilidade (ou do ataque básico, ou da poção) em cada
/// tecla. Só do cliente: o uso continua indo à autoridade pelo id (<c>UseAbilityIntent</c>). Sem Godot, para os testes.
/// Um id fica em no máximo um espaço: pôr num espaço tira dos outros.
/// </summary>
public sealed class HotbarLayout
{
    public const int Size = 6;

    private const char Separator = '|';

    private readonly string?[] _slots = new string?[Size];

    /// <summary>Algum espaço mudou pela mão do jogador (arrastar, trocar, esvaziar). Não dispara em <see cref="Load"/>.</summary>
    public event Action? Changed;

    /// <summary>Configuração carregada (de <see cref="Load"/>); antes disso a barra está vazia e não vale.</summary>
    public bool Loaded { get; private set; }

    public string? this[int slot] => slot is >= 0 and < Size ? _slots[slot] : null;

    public IReadOnlyList<string?> Slots => _slots;

    /// <summary>Padrão sem configuração salva: as habilidades na ordem dos dados em 1–5 e o primeiro consumível em 6.</summary>
    public static string?[] Defaults(IReadOnlyList<string> abilities, IReadOnlyList<string> consumables)
    {
        var slots = new string?[Size];
        for (var i = 0; i < Size - 1 && i < abilities.Count; i++)
        {
            slots[i] = abilities[i];
        }

        if (consumables.Count > 0)
        {
            slots[Size - 1] = consumables[0];
        }

        return slots;
    }

    /// <summary>
    /// Troca a barra inteira: <paramref name="saved"/> (de <see cref="Parse"/>) ou, se null, <paramref name="defaults"/>. Id que
    /// <paramref name="exists"/> recusa (sumiu dos dados) vira espaço vazio, e id repetido fica só no primeiro espaço.
    /// </summary>
    public void Load(IReadOnlyList<string?>? saved, IReadOnlyList<string?> defaults, Func<string, bool> exists)
    {
        var source = saved ?? defaults;
        Array.Clear(_slots);
        for (var i = 0; i < Size && i < source.Count; i++)
        {
            if (source[i] is { Length: > 0 } id && exists(id) && Array.IndexOf(_slots, id) < 0)
            {
                _slots[i] = id;
            }
        }

        Loaded = true;
    }

    /// <summary>Põe <paramref name="id"/> no espaço (null esvazia); se ele já estava em outro espaço, sai de lá.</summary>
    public void Set(int slot, string? id)
    {
        if (slot is < 0 or >= Size)
        {
            return;
        }

        if (id is not null)
        {
            var current = Array.IndexOf(_slots, id);
            if (current == slot)
            {
                return;
            }

            if (current >= 0)
            {
                _slots[current] = null;
            }
        }

        _slots[slot] = id;
        Changed?.Invoke();
    }

    /// <summary>Troca o conteúdo de dois espaços.</summary>
    public void Swap(int a, int b)
    {
        if (a == b || a is < 0 or >= Size || b is < 0 or >= Size)
        {
            return;
        }

        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        Changed?.Invoke();
    }

    public void Clear(int slot) => Set(slot, null);

    /// <summary>Espaço onde o id está, ou -1.</summary>
    public int IndexOf(string id) => Array.IndexOf(_slots, id);

    /// <summary>Texto para guardar: os ids separados por <c>|</c>, vazio para espaço vazio.</summary>
    public string Serialize() => string.Join(Separator, _slots.Select(s => s ?? ""));

    /// <summary>Lê o texto de <see cref="Serialize"/>; null se não há nada guardado.</summary>
    public static string?[]? Parse(string? text) =>
        string.IsNullOrEmpty(text) ? null : text.Split(Separator).Select(s => s.Length > 0 ? s : null).ToArray();
}
