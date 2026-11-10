using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Match;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Tela de habilidades (T-221, tecla K; em grade de ícones na T-081; <c>design/progression-v0.1.md</c> §7): ataque
/// básico e as habilidades da classe (assinatura em destaque), com os pontos do NH embaixo de cada ícone (T-323: a poção
/// saiu daqui; é item do inventário). O plano
/// vem do estado do servidor (<see cref="SkillPlan.FromState"/>, com <see cref="SkillPlan.Enforce"/>): o + no ícone só
/// aparece quando há ponto e nível, e manda <see cref="RaiseSkillRankIntent"/>. A dica mostra o NH atual e o próximo com
/// as estatísticas do personagem. Arrastar um ícone para a barra põe a habilidade numa tecla de 1 a 6.
/// </summary>
public partial class SkillPanel : ProgressionPanel
{
    private const int Columns = 4;
    private const float CellWidth = 112f;
    private const float IconSize = 44f;

    private static readonly Color BasicBorder = new(0.55f, 0.75f, 1f, 0.8f);

    /// <summary>Janela de pedra e ouro (T-097).</summary>
    protected override bool StoneWindow => true;

    protected override string StateKey(EntitySnapshot self)
    {
        var s = self.TryGet<CharacterStats>();
        var p = self.TryGet<CharacterProgress>();
        var st = self.TryGet<AbilityState>();
        return string.Join(",", s?.Level, s?.Str, s?.Dex, s?.Agi, s?.Int, s?.Vit, s?.WeaponDamage, p?.SkillPoints,
            st is null ? "" : string.Join(";", st.Known.Select(id => id + st.RankOf(id))));
    }

    protected override void Build(VBoxContainer box, EntitySnapshot self)
    {
        var info = self.TryGet<UnitInfo>();
        var stats = self.TryGet<CharacterStats>();
        var progress = self.TryGet<CharacterProgress>();
        var state = self.TryGet<AbilityState>();
        box.AddChild(WindowTitle("Habilidades"));
        if (!Content.Classes.TryGetValue(info?.DefinitionId ?? "", out var cls) || state is null)
        {
            box.AddChild(Small("Sem habilidades para mostrar.", 12));
            return;
        }

        var level = stats?.Level ?? 1;
        var plan = SkillPlan.FromState(cls, Content, state, level);
        if (progress is null)
        {
            box.AddChild(Small("Encontro de teste: personagem sem progressão. Os NH vêm do menu e não recebem pontos aqui.", 12,
                width: Columns * CellWidth));
        }
        else
        {
            var free = Small(progress.SkillPoints == 1 ? "1 ponto de habilidade livre" : $"{progress.SkillPoints} pontos de habilidade livres",
                16, progress.SkillPoints > 0 ? 1f : 0.7f);
            if (progress.SkillPoints > 0)
            {
                free.AddThemeColorOverride("font_color", HudTheme.Removable);
            }

            box.AddChild(free);
            box.AddChild(Small($"Usados {plan.PointsUsed} de {plan.PointBudget} (nível {level}).", 11, 0.6f));
        }

        var grid = new GridContainer { Columns = Columns };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        box.AddChild(grid);

        if (Content.AbilityAt(cls.BasicAttack, 1) is { } basic)
        {
            grid.AddChild(Cell(basic, IconCatalog.Shared.For(basic.Id), BasicBorder, "ataque básico",
                Tip(basic.Name + " — ataque básico", basic, stats, null), draggable: true, plus: null, req: null));
        }

        foreach (var ability in plan.Abilities)
        {
            grid.AddChild(AbilityCell(self, ability, plan, progress, stats, level));
        }

        // T-323: as poções são itens do inventário (I), de onde vão para a barra; não aparecem mais aqui.
        box.AddChild(Small("Arraste um ícone para a barra (teclas 1 a 6). Habilidade não aprendida não vai para a barra. "
            + "Poções: arraste do inventário (I).", 11, 0.6f,
            Columns * CellWidth));

        if (progress is not null && stats is not null)
        {
            box.AddChild(HudTheme.Divider(Columns * CellWidth) ?? new HSeparator());
            var footer = new HBoxContainer();
            footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            var reset = ResetButton(self, AnySpent(progress, stats, Content));
            HudTheme.StoneButton(reset);
            footer.AddChild(reset);
            box.AddChild(footer);
        }
    }

    private Control AbilityCell(EntitySnapshot self, AbilityDefinition ability, SkillPlan plan, CharacterProgress? progress,
        CharacterStats? stats, int level)
    {
        var rank = plan.RankOf(ability.Id);
        var max = SkillPlan.MaxRank(ability);
        var next = rank + 1;
        var blocker = progress is null ? ProgressionRejectReason.NoProgression : plan.RaiseBlocker(ability.Id);
        var current = rank > 0 ? Content.AbilityAt(ability.Id, rank) : null;
        var nextDef = next <= max ? Content.AbilityAt(ability.Id, next) : null;

        var title = $"{ability.Name}{(ability.Signature ? " — assinatura" : "")}\n"
            + (rank > 0 ? $"NH {SkillPlan.Roman(rank)} de {SkillPlan.Roman(max)}" : $"Não aprendida (NH até {SkillPlan.Roman(max)})");
        var tip = Tip(title, current, stats, nextDef is null ? null : (next, nextDef));
        if (rank == 0 && nextDef is not null)
        {
            tip = Tip(title, null, stats, (next, nextDef));
        }

        if (blocker is { } b && progress is not null && nextDef is not null)
        {
            tip += $"\n\n{ProgressionText.Reason(b)}";
        }

        var pips = (Rank: rank, Max: max);
        Button? plus = null;
        if (blocker is null)
        {
            var id = ability.Id;
            plus = PlusButton(true, rank == 0 ? "Aprender" : $"Subir para o NH {SkillPlan.Roman(next)}",
                () => Send?.Invoke(new RaiseSkillRankIntent(self.Id, id)));
        }

        Label? req = null;
        if (next <= max)
        {
            var required = plan.RequiredLevel(ability, next);
            req = Small($"NH {SkillPlan.Roman(next)}: nível {required}", 10, level >= required ? 0.55f : 1f);
            if (level < required)
            {
                req.AddThemeColorOverride("font_color", HudTheme.Warning);
            }
        }

        return Cell(current ?? ability, IconCatalog.Shared.For(ability.Id), ability.Signature ? HudTheme.Suggested : new Color(1, 1, 1, 0.12f),
            null, tip, draggable: rank > 0, plus, req, dim: rank == 0, pips);
    }

    /// <summary>
    /// Uma célula da grade: ícone (com o + no canto), nome, linha de baixo (os pontos do NH em <paramref name="pips"/>, ou um
    /// texto como "ataque básico") e o nível pedido. Com a peça de pedra (T-097), a célula é de pedra e a cor de destaque
    /// (básico ou assinatura) vira um contorno fino por dentro dela.
    /// </summary>
    private static PanelContainer Cell(AbilityDefinition def, Texture2D? icon, Color border, string? below, string tip, bool draggable,
        Button? plus, Label? req, bool dim = false, (int Rank, int Max)? pips = null)
    {
        var cell = new PanelContainer { CustomMinimumSize = new Vector2(CellWidth, 0), TooltipText = tip };
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        if (HudTheme.SkillCell() is { } stone)
        {
            cell.AddThemeStyleboxOverride("panel", stone);
            var inner = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            inner.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = Colors.Transparent,
                BorderColor = border.A > 0.5f ? border : Colors.Transparent,
                BorderWidthLeft = 1,
                BorderWidthTop = 1,
                BorderWidthRight = 1,
                BorderWidthBottom = 1,
                ContentMarginTop = 3,
                ContentMarginBottom = 3,
            });
            inner.AddChild(column);
            cell.AddChild(inner);
        }
        else
        {
            cell.AddThemeStyleboxOverride("panel", FlatCell(border));
            cell.AddChild(column);
        }

        column.AddThemeConstantOverride("separation", 1);

        var holder = new Control
        {
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        holder.AddChild(new SkillIcon
        {
            Icon = icon,
            AbilityId = draggable ? def.Id : null,
            Fallback = def.Name,
            Size = new Vector2(IconSize, IconSize),
            TooltipText = tip,
            Modulate = new Color(1, 1, 1, dim ? 0.4f : 1f),
        });
        if (plus is not null)
        {
            plus.CustomMinimumSize = new Vector2(20, 20);
            plus.Size = new Vector2(20, 20);
            plus.AddThemeColorOverride("font_color", HudTheme.Removable);
            HudTheme.StonePlusButton(plus);
            plus.Position = new Vector2(IconSize - plus.Size.X / 2 - 2, -plus.Size.Y / 2 + 2);
            holder.AddChild(plus);
        }

        column.AddChild(holder);
        var name = Small(def.Name, 11, 1f, CellWidth - 8);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(name);
        column.AddChild(pips is { } p ? Pips(p.Rank, p.Max) : BelowLine(below ?? ""));
        if (req is not null)
        {
            req.HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(req);
        }

        return cell;
    }

    private static StyleBoxFlat FlatCell(Color border) => new()
    {
        BgColor = new Color(1, 1, 1, 0.04f),
        BorderColor = border,
        BorderWidthLeft = 1,
        BorderWidthTop = 1,
        BorderWidthRight = 1,
        BorderWidthBottom = 1,
        ContentMarginTop = 4,
        ContentMarginBottom = 4,
    };

    private static Label BelowLine(string text)
    {
        var line = Small(text, 11, 0.85f);
        line.HorizontalAlignment = HorizontalAlignment.Center;
        line.AddThemeColorOverride("font_color", HudTheme.Suggested);
        return line;
    }

    /// <summary>Pontos do NH: gemas de ouro (cheia e vazia, T-097) ou, sem as peças, ● e ○.</summary>
    private static Control Pips(int rank, int max)
    {
        var on = HudTheme.UiTexture("skill_rank_on.png");
        var off = HudTheme.UiTexture("skill_rank_off.png");
        if (on is null || off is null)
        {
            return BelowLine(new string('●', rank) + new string('○', Math.Max(0, max - rank)));
        }

        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 2);
        for (var i = 0; i < max; i++)
        {
            row.AddChild(new TextureRect
            {
                Texture = i < rank ? on : off,
                StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            });
        }

        return row;
    }

    /// <summary>Dica: título, alcance, custo e efeitos do NH atual e a prévia do próximo, com as estatísticas do personagem.</summary>
    private string Tip(string title, AbilityDefinition? current, CharacterStats? stats, (int Rank, AbilityDefinition Def)? next)
    {
        var text = title;
        if (current is not null)
        {
            text += Section(current, stats);
        }

        if (next is { } n)
        {
            text += $"\n\nPróximo (NH {SkillPlan.Roman(n.Rank)}):" + Section(n.Def, stats);
        }
        else if (current is not null && !current.Id.StartsWith("item.", StringComparison.Ordinal))
        {
            text += "\n\nNH máximo.";
        }

        return text;
    }

    private string Section(AbilityDefinition def, CharacterStats? stats)
    {
        var reach = AbilityText.Reach(def);
        return (reach.Length > 0 ? "\n" + char.ToUpperInvariant(reach[0]) + reach[1..] : "") + "\n" + AbilityText.Describe(def, Content, stats);
    }
}
