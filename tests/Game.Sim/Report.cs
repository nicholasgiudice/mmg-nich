using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Sim;

public static class Report
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ToJson(string dataDirectory, IReadOnlyList<BatchSummary> summaries) =>
        JsonSerializer.Serialize(new { data = dataDirectory, results = summaries }, JsonOptions);

    public static string ToTable(IReadOnlyList<BatchSummary> summaries)
    {
        var sb = new StringBuilder();
        foreach (var s in summaries)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"== {s.Encounter} | {s.Strategy} | {s.Runs} runs (seed {s.BaseSeed}, {s.WallSeconds:0.0}s) ==");
            Row(sb, "win rate", $"{s.WinRate:P1}  ({s.Wins}W / {s.Losses}L / {s.Timeouts}T)");
            Row(sb, "time to win (s)", $"mean {s.TimeToWinMean:0.0}  p50 {s.TimeToWinP50:0.0}  p90 {s.TimeToWinP90:0.0}");
            Row(sb, "damage taken / dealt", $"{s.DamageTaken:0.0} / {s.DamageDealt:0.0}");
            Row(sb, "healing (overheal)", $"{s.Healing:0.0} ({s.Overheal:0.0})");
            Row(sb, "resource gain/spent/waste", $"{s.ResourceGained:0.0} / {s.ResourceSpent:0.0} / {s.ResourceWasted:0.0}  (at cap {s.SecondsAtResourceCap:0.0}s)");
            Row(sb, "deaths player/enemy", $"{s.PlayerDeaths:0.00} / {s.EnemyDeaths:0.00}");
            Row(sb, "boss phase changes", $"{s.PhaseChanges:0.00}");
            Row(sb, "moved (tiles) / still uses", $"{s.DistanceMoved:0.0} / {s.StationaryUseShare:P0}");
            Row(sb, "distinct abilities used", $"{s.DistinctAbilities:0.0}");
            if (s.AbilityUses.Count > 0)
            {
                var total = s.AbilityUses.Values.Sum();
                var width = s.AbilityUses.Keys.Max(k => k.Length);
                sb.AppendLine("  ability uses per run:");
                foreach (var (id, count) in s.AbilityUses.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    {id.PadRight(width)}  {count,7:0.00}  {(total == 0 ? 0 : count / total):P0}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void Row(StringBuilder sb, string label, string value) =>
        sb.AppendLine(CultureInfo.InvariantCulture, $"  {label.PadRight(28)} {value}");
}
