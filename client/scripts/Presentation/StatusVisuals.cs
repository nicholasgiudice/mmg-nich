using Game.Core.Combat;
using Game.Core.Combat.Status;
using Godot;

namespace MmoRpgClient.Presentation;

/// <summary>Controle forte em vigor, para indicação no corpo da unidade.</summary>
[Flags]
public enum ControlMarks
{
    None = 0,
    Stunned = 1,
    Rooted = 2,
    Frozen = 4,
    Silenced = 8,
}

/// <summary>Um ícone pequeno de status acima da unidade (sigla + cor no protótipo).</summary>
public readonly record struct StatusChip(string Abbreviation, Color Color);

/// <summary>Converte os status ativos (snapshot) em ícones e marcas de controle, a partir das definições.</summary>
public static class StatusVisuals
{
    public const int MaxChips = 5;

    private static readonly Color Buff = new(0.35f, 0.80f, 0.45f);
    private static readonly Color Debuff = new(0.90f, 0.35f, 0.30f);
    private static readonly Color Control = new(1.00f, 0.80f, 0.20f);
    private static readonly Color Mark = new(1.00f, 0.55f, 0.15f);
    private static readonly Color Poison = new(0.55f, 0.90f, 0.25f);

    public static (IReadOnlyList<StatusChip> Chips, ControlMarks Marks) From(StatusEffects? effects, ContentCatalog content, double now)
    {
        if (effects is null || effects.Active.Count == 0)
        {
            return (Array.Empty<StatusChip>(), ControlMarks.None);
        }

        var chips = new List<StatusChip>();
        var marks = ControlMarks.None;
        foreach (var st in effects.Active)
        {
            if (st.ExpiresAtTick <= now)
            {
                continue;
            }

            content.Statuses.TryGetValue(st.StatusId, out var def);
            var tags = def?.Tags ?? [];
            if (def is not null)
            {
                marks |= def.Flags.Stunned ? ControlMarks.Stunned : ControlMarks.None;
                marks |= def.Flags.Silenced ? ControlMarks.Silenced : ControlMarks.None;
                marks |= tags.Contains("freeze") ? ControlMarks.Frozen
                    : def.Flags.Rooted ? ControlMarks.Rooted
                    : ControlMarks.None;
            }

            if (chips.Count >= MaxChips)
            {
                continue;
            }

            var color = tags.Contains("control") ? Control
                : tags.Contains("poison") ? Poison
                : tags.Contains("mark") ? Mark
                : tags.Contains("buff") ? Buff
                : Debuff;
            chips.Add(new StatusChip(Abbreviate(def?.Name ?? st.StatusId, st.Stacks), color));
        }

        return (chips, marks);
    }

    private static string Abbreviate(string name, int stacks)
    {
        var letters = name.Length >= 2 ? name[..2] : name;
        return stacks > 1 ? $"{letters}{stacks}" : letters;
    }
}
