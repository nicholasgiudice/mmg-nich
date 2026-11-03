using System.Text;
using System.Text.RegularExpressions;
using static Game.Site.Fmt;

namespace Game.Site;

/// <summary>
/// Markdown mínimo para a página de créditos (<c>CREDITS.md</c>): títulos, parágrafos, listas, tabelas, código em linha,
/// negrito, links e endereços soltos. Títulos rebaixam um nível (o <c>h1</c> é da página).
/// </summary>
public static partial class Markdown
{
    public static string ToHtml(string markdown)
    {
        var html = new StringBuilder();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var paragraph = new List<string>();
        string? list = null;

        void FlushParagraph()
        {
            if (paragraph.Count > 0)
            {
                html.Append("<p>").Append(Inline(string.Join(" ", paragraph))).Append("</p>\n");
                paragraph.Clear();
            }
        }

        void CloseList()
        {
            if (list is not null)
            {
                html.Append($"</{list}>\n");
                list = null;
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (line.Length == 0)
            {
                FlushParagraph();
                CloseList();
                continue;
            }

            if (HeadingRegex().Match(line) is { Success: true } h)
            {
                FlushParagraph();
                CloseList();
                var level = Math.Min(6, h.Groups[1].Value.Length + 1);
                html.Append($"<h{level}>{Inline(h.Groups[2].Value)}</h{level}>\n");
                continue;
            }

            if (line.StartsWith('|'))
            {
                FlushParagraph();
                CloseList();
                var rows = new List<string>();
                while (i < lines.Length && lines[i].TrimStart().StartsWith('|'))
                {
                    rows.Add(lines[i].Trim());
                    i++;
                }

                i--;
                html.Append(Table(rows));
                continue;
            }

            var ordered = OrderedRegex().Match(line);
            var bullet = line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal);
            if (ordered.Success || bullet)
            {
                FlushParagraph();
                var kind = ordered.Success ? "ol" : "ul";
                if (list != kind)
                {
                    CloseList();
                    html.Append($"<{kind}>\n");
                    list = kind;
                }

                var text = ordered.Success ? ordered.Groups[1].Value : line[2..];
                html.Append("<li>").Append(Inline(text)).Append("</li>\n");
                continue;
            }

            if (list is not null && lines[i].StartsWith("  ", StringComparison.Ordinal))
            {
                // Continuação de item de lista: anexa ao último <li>.
                var end = html.ToString().LastIndexOf("</li>", StringComparison.Ordinal);
                html.Insert(end, " " + Inline(line.Trim()));
                continue;
            }

            CloseList();
            paragraph.Add(line.Trim());
        }

        FlushParagraph();
        CloseList();
        return html.ToString();
    }

    private static string Table(List<string> rows)
    {
        static string[] Cells(string row) => row.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();

        var sb = new StringBuilder("<div class=\"table-wrap\"><table>\n");
        var header = Cells(rows[0]);
        sb.Append("<thead><tr>").Append(string.Concat(header.Select(c => $"<th>{Inline(c)}</th>"))).Append("</tr></thead>\n<tbody>\n");
        foreach (var row in rows.Skip(1).Where(r => !SeparatorRegex().IsMatch(r)))
        {
            sb.Append("<tr>").Append(string.Concat(Cells(row).Select(c => $"<td>{Inline(c)}</td>"))).Append("</tr>\n");
        }

        return sb.Append("</tbody></table></div>\n").ToString();
    }

    /// <summary>Formatação em linha. O texto é escapado antes; código em linha fica protegido das outras regras.</summary>
    public static string Inline(string text)
    {
        var codes = new List<string>();
        var s = CodeRegex().Replace(text, m =>
        {
            codes.Add($"<code>{Esc(m.Groups[1].Value)}</code>");
            return $"\u0000{codes.Count - 1}\u0000";
        });
        s = Esc(s);
        s = LinkRegex().Replace(s, m => $"<a href=\"{m.Groups[2].Value}\">{m.Groups[1].Value}</a>");
        s = BareUrlRegex().Replace(s, m => $"<a href=\"{m.Value}\">{m.Value}</a>");
        s = BoldRegex().Replace(s, "<strong>$1</strong>");
        return PlaceholderRegex().Replace(s, m => codes[int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)]);
    }

    [GeneratedRegex(@"^(#{1,5})\s+(.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\d+\.\s+(.*)$")]
    private static partial Regex OrderedRegex();

    [GeneratedRegex(@"^\|?\s*:?-{3,}")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\((https?://[^)\s]+)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"(?<![""=>/])\bhttps?://[^\s<)""]+[^\s<)"".,;]")]
    private static partial Regex BareUrlRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex("\u0000(\\d+)\u0000")]
    private static partial Regex PlaceholderRegex();
}
