namespace MmoRpgClient.Hud;

/// <summary>Um trecho do texto de um NH e o separador que vem depois dele ("", ", ", "; " ou quebra de linha).</summary>
public sealed record DetailSegment(string Text, string Separator, bool Changed);

/// <summary>
/// Detalhe de uma habilidade na tela de habilidades: o texto de cada NH dividido em trechos (custo, recarga, cada
/// efeito), com a marca do que mudou em relação ao NH anterior. Só texto, sem Godot: o painel pinta os trechos marcados.
/// </summary>
public static class SkillDetailText
{
    private static readonly string[] Separators = ["\n", "; ", ", "];

    /// <summary>
    /// Divide <paramref name="current"/> em trechos e marca os que não aparecem em <paramref name="previous"/> (o texto do
    /// NH anterior; null = primeiro NH, nada marcado). A comparação ignora maiúsculas, porque o primeiro trecho de cada
    /// linha começa com maiúscula.
    /// </summary>
    public static IReadOnlyList<DetailSegment> Compare(string? previous, string current)
    {
        var before = previous is null
            ? null
            : new HashSet<string>(Split(previous).Select(s => s.Text), StringComparer.OrdinalIgnoreCase);
        return Split(current)
            .Select(s => s with { Changed = before is not null && s.Text.Length > 0 && !before.Contains(s.Text) })
            .ToList();
    }

    /// <summary>
    /// Só as linhas com alguma mudança, inteiras (o resto da linha dá o contexto). Sem nenhuma mudança, a lista fica
    /// vazia. A última linha guardada perde a quebra de linha final.
    /// </summary>
    public static IReadOnlyList<DetailSegment> ChangedLines(IReadOnlyList<DetailSegment> segments)
    {
        var kept = new List<DetailSegment>();
        var line = new List<DetailSegment>();
        foreach (var segment in segments)
        {
            line.Add(segment);
            if (segment.Separator is "\n" or "")
            {
                if (line.Any(s => s.Changed))
                {
                    kept.AddRange(line);
                }

                line.Clear();
            }
        }

        if (kept.Count > 0 && kept[^1].Separator == "\n")
        {
            kept[^1] = kept[^1] with { Separator = "" };
        }

        return kept;
    }

    /// <summary>Trechos de um texto, cada um com o separador que o segue (o último fica com "").</summary>
    public static IReadOnlyList<DetailSegment> Split(string text)
    {
        var segments = new List<DetailSegment>();
        var start = 0;
        var i = 0;
        while (i < text.Length)
        {
            var separator = Separators.FirstOrDefault(s => string.CompareOrdinal(text, i, s, 0, s.Length) == 0);
            if (separator is null)
            {
                i++;
                continue;
            }

            segments.Add(new DetailSegment(text[start..i], separator, false));
            i += separator.Length;
            start = i;
        }

        segments.Add(new DetailSegment(text[start..], "", false));
        return segments;
    }
}
