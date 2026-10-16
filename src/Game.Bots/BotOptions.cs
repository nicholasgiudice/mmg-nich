using System.Globalization;

namespace Game.Bots;

/// <summary>
/// Opções livres chave=valor das estratégias. Uma chave pode ser escopada por classe
/// (<c>class.warrior:taunt</c>), que tem prioridade sobre a chave global (<c>taunt</c>).
/// Chaves usadas: taunt, explosion, heal (seletores), skip, order (listas), healBelow, minAoe, kiteDistance, retreatDistance.
/// Um seletor é uma lista separada por vírgula de ids de habilidade ou <c>tag:xxx</c>.
/// </summary>
public sealed class BotOptions
{
    public static readonly BotOptions Empty = new(new Dictionary<string, string>());

    public BotOptions(IReadOnlyDictionary<string, string> values) => Values = values;

    public IReadOnlyDictionary<string, string> Values { get; }

    public string? Get(string classId, string key) =>
        Values.TryGetValue($"{classId}:{key}", out var v) || Values.TryGetValue(key, out v) ? v : null;

    public double GetDouble(string classId, string key, double fallback) =>
        Get(classId, key) is { } s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : fallback;

    public IReadOnlyList<string> GetList(string classId, string key) =>
        Get(classId, key) is { } s
            ? s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
}
