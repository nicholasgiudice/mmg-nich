using Game.Core.Combat;
using Game.Core.Combat.Status;
using Game.Core.Definitions;
using Game.Core.Simulation;
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

    /// <summary>Medo (T-351, <c>flags.feared</c>): foge sem controle.</summary>
    Feared = 16,

    /// <summary>Na névoa (T-340: status com a tag <c>mist</c> que deixa a unidade fora de alvo): semitransparente, sem barra.</summary>
    Veiled = 32,
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

    /// <summary>T-540: estado do terreno (Água Funda): azul-água, para não parecer um golpe de monstro.</summary>
    public static readonly Color Terrain = new(0.42f, 0.77f, 0.91f);

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
                marks |= def.Flags.Feared ? ControlMarks.Feared : ControlMarks.None;
                marks |= def.Flags.Untargetable && tags.Contains("mist") ? ControlMarks.Veiled : ControlMarks.None;
                marks |= tags.Contains("freeze") ? ControlMarks.Frozen
                    : def.Flags.Rooted ? ControlMarks.Rooted
                    : ControlMarks.None;
            }

            if (chips.Count >= MaxChips)
            {
                continue;
            }

            var color = tags.Contains("terrain") ? Terrain
                : tags.Contains("control") ? Control
                : tags.Contains("poison") ? Poison
                : tags.Contains("mark") ? Mark
                : tags.Contains("buff") ? Buff
                : Debuff;
            chips.Add(new StatusChip(Abbreviate(def?.Name ?? st.StatusId, st.Stacks), color));
        }

        return (chips, marks);
    }

    /// <summary>
    /// Sinais dos status ativos no corpo (T-412), pela lista de <see cref="EffectCatalog.Aura"/>, sem repetir o mesmo
    /// sinal. Para <see cref="AuraKind.Link"/>, vem junto quem protege (a outra ponta da linha).
    /// </summary>
    public static IReadOnlyList<(StatusAura Aura, EntityId Protector)> Auras(StatusEffects? effects, EffectCatalog catalog, double now,
        ContentCatalog? content = null)
    {
        if (effects is null || effects.Active.Count == 0)
        {
            return Array.Empty<(StatusAura, EntityId)>();
        }

        var auras = new List<(StatusAura, EntityId)>();
        foreach (var st in effects.Active)
        {
            if (st.ExpiresAtTick <= now || catalog.Aura(st.StatusId) is not { } found)
            {
                continue;
            }

            if (found.Kind == AuraKind.Stacks)
            {
                // T-434 (Veneno do Caçador): um sinal só, com o maior número de acúmulos entre as instâncias.
                var i = auras.FindIndex(a => a.Item1.Kind == AuraKind.Stacks && a.Item1.Color == found.Color);
                var stacks = Math.Max(st.Stacks, i >= 0 ? auras[i].Item1.Stacks : 0);
                if (i >= 0)
                {
                    auras[i] = (found with { Stacks = stacks }, st.Protector);
                }
                else
                {
                    auras.Add((found with { Stacks = stacks }, st.Protector));
                }

                continue;
            }

            // T-444: quantos orbes desenhar e as pilhas da Marca Arcana.
            var aura = found.Kind switch
            {
                AuraKind.Orbs => found with { Count = OrbCount(st.StatusId, content) },
                AuraKind.Mark => found with { Count = Math.Max(1, st.Stacks) },
                _ => found,
            };
            if (!auras.Any(a => a.Item1 == aura && (aura.Kind != AuraKind.Link || a.Item2 == st.Protector)))
            {
                auras.Add((aura, st.Protector));
            }
        }

        return auras;
    }

    /// <summary>
    /// Quantos orbes desenhar (T-444): o <c>count</c> do <c>current_target</c> no efeito periódico do status (2 a 4 nos Orbes
    /// Arcanos, pelo NH). Sem a definição, 3.
    /// </summary>
    public static int OrbCount(string statusId, ContentCatalog? content)
    {
        if (content is null || !content.Statuses.TryGetValue(statusId, out var def))
        {
            return 3;
        }

        var count = def.Periodic.SelectMany(p => p.Effects).OfType<CurrentTargetEffect>().Select(c => c.Count).DefaultIfEmpty(0).Max();
        return count > 0 ? count : 3;
    }

    private static string Abbreviate(string name, int stacks)
    {
        var letters = name.Length >= 2 ? name[..2] : name;
        return stacks > 1 ? $"{letters}{stacks}" : letters;
    }
}
