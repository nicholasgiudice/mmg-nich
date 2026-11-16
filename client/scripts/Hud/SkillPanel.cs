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
/// as estatísticas do personagem. Arrastar um ícone para a barra põe a habilidade numa tecla de 1 a 6. O "?" no canto de
/// cada célula abre, ao lado da grade, o detalhe da habilidade: todos os NH, com o nível pedido e, em dourado, o que muda
/// em relação ao NH anterior.
/// </summary>
public partial class SkillPanel : ProgressionPanel
{
    private const int Columns = 4;
    private const float CellWidth = 112f;
    private const float IconSize = 44f;

    private const float DetailWidth = 320f;

    private static readonly Color BasicBorder = new(0.55f, 0.75f, 1f, 0.8f);

    /// <summary>Habilidade com o detalhe aberto ao lado da grade; null = fechado.</summary>
    private string? _detailId;

    /// <summary>Janela de pedra e ouro (T-097).</summary>
    protected override bool StoneWindow => true;

    protected override string StateKey(EntitySnapshot self)
    {
        var s = self.TryGet<CharacterStats>();
        var p = self.TryGet<CharacterProgress>();
        var st = self.TryGet<AbilityState>();
        return string.Join(",", s?.Level, s?.Str, s?.Dex, s?.Agi, s?.Int, s?.Vit, s?.WeaponDamage, p?.SkillPoints,
            st is null ? "" : string.Join(";", st.Known.Select(id => id + st.RankOf(id))), _detailId);
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

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        box.AddChild(row);
        var grid = new GridContainer { Columns = Columns, SizeFlagsVertical = SizeFlags.ShrinkBegin };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        row.AddChild(grid);

        if (Content.AbilityAt(cls.BasicAttack, 1) is { } basic)
        {
            grid.AddChild(Cell(basic, IconCatalog.Shared.For(basic.Id), BasicBorder, "ataque básico",
                Tip(basic.Name + " — ataque básico", basic, stats, null), draggable: true, plus: null, req: null,
                detail: DetailButton(basic.Id)));
            if (_detailId == basic.Id)
            {
                row.AddChild(Detail(basic, "ataque básico", 1, 1, stats, _ => null, level));
            }
        }

        foreach (var ability in plan.Abilities)
        {
            grid.AddChild(AbilityCell(self, ability, plan, progress, stats, level));
            if (_detailId == ability.Id)
            {
                var max = SkillPlan.MaxRank(ability);
                row.AddChild(Detail(ability, ability.Signature ? "assinatura" : null, plan.RankOf(ability.Id), max, stats,
                    r => plan.RequiredLevel(ability, r), level));
            }
        }

        // T-323: as poções são itens do inventário (I), de onde vão para a barra; não aparecem mais aqui.
        box.AddChild(Small("Arraste um ícone para a barra (teclas 1 a 6). Habilidade não aprendida não vai para a barra. "
            + "Poções: arraste do inventário (I).", 11, 0.6f,
            Columns * CellWidth));

        if (progress is not null && stats is not null)
        {
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
            null, tip, draggable: rank > 0, plus, req, dim: rank == 0, pips, DetailButton(ability.Id));
    }

    /// <summary>
    /// Uma célula da grade: ícone (com o + no canto), nome, linha de baixo (os pontos do NH em <paramref name="pips"/>, ou um
    /// texto como "ataque básico") e o nível pedido. Com a peça de pedra (T-097), a célula é de pedra e a cor de destaque
    /// (básico ou assinatura) vira um contorno fino por dentro dela.
    /// </summary>
    private static PanelContainer Cell(AbilityDefinition def, Texture2D? icon, Color border, string? below, string tip, bool draggable,
        Button? plus, Label? req, bool dim = false, (int Rank, int Max)? pips = null, Button? detail = null)
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
            cell.AddChild(PlusCorner(plus));
        }

        if (detail is not null)
        {
            cell.AddChild(DetailCorner(detail));
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

    /// <summary>
    /// Camada por cima da célula com o + no canto de cima à direita, por dentro da moldura e longe do ícone, em posição
    /// inteira (meio pixel borra a arte).
    /// </summary>
    private static Control PlusCorner(Button plus)
    {
        plus.CustomMinimumSize = new Vector2(20, 20);
        plus.AddThemeColorOverride("font_color", HudTheme.Removable);
        HudTheme.StonePlusButton(plus);
        var size = plus.CustomMinimumSize;
        plus.AnchorLeft = 1;
        plus.AnchorRight = 1;
        plus.OffsetLeft = -size.X;
        plus.OffsetRight = 0;
        plus.OffsetTop = 0;
        plus.OffsetBottom = size.Y;

        var layer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        layer.AddChild(plus);
        return layer;
    }

    /// <summary>O "?" da célula: abre o detalhe da habilidade ao lado da grade, ou fecha se ele já está aberto.</summary>
    private Button DetailButton(string abilityId)
    {
        var open = _detailId == abilityId;
        var button = new Button
        {
            Text = "?",
            ToggleMode = true,
            ButtonPressed = open,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(20, 20),
            TooltipText = open ? "Fechar o detalhe" : "Ver o detalhe: o que a habilidade faz em cada NH",
        };
        // Moeda escura com borda de ouro e "?" dourado: diferente do + (arte de pedra), que fica no outro canto.
        StyleBoxFlat Coin(Color back) => new()
        {
            BgColor = back,
            BorderColor = HudTheme.Suggested,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
        };
        button.AddThemeStyleboxOverride("normal", Coin(new Color(0.08f, 0.08f, 0.1f, 0.9f)));
        button.AddThemeStyleboxOverride("hover", Coin(new Color(0.22f, 0.2f, 0.14f, 0.95f)));
        button.AddThemeStyleboxOverride("pressed", Coin(new Color(0.45f, 0.36f, 0.12f, 0.95f)));
        button.AddThemeStyleboxOverride("hover_pressed", Coin(new Color(0.5f, 0.4f, 0.14f, 0.95f)));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        button.AddThemeFontSizeOverride("font_size", 13);
        button.AddThemeColorOverride("font_color", HudTheme.Suggested);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_hover_pressed_color", Colors.White);
        button.Pressed += () =>
        {
            _detailId = _detailId == abilityId ? null : abilityId;
            Refresh();
        };
        return button;
    }

    /// <summary>Camada com o "?" no canto de cima à esquerda da célula (o + fica no da direita).</summary>
    private static Control DetailCorner(Button detail)
    {
        var size = detail.CustomMinimumSize;
        detail.OffsetLeft = 0;
        detail.OffsetTop = 0;
        detail.OffsetRight = size.X;
        detail.OffsetBottom = size.Y;
        var layer = new Control { MouseFilter = MouseFilterEnum.Ignore };
        layer.AddChild(detail);
        return layer;
    }

    /// <summary>
    /// Detalhe de uma habilidade: nome, e para cada NH o nível pedido e o que ela faz (alcance, custo, tempos e efeitos,
    /// com as estatísticas atuais do personagem). Em dourado, o que mudou em relação ao NH anterior; o NH atual tem marca.
    /// </summary>
    private Control Detail(AbilityDefinition ability, string? kind, int rank, int max, CharacterStats? stats,
        Func<int, int?> requiredLevel, int level)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(DetailWidth, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
        if (HudTheme.SkillCell() is { } stone)
        {
            panel.AddThemeStyleboxOverride("panel", stone);
        }
        else
        {
            panel.AddThemeStyleboxOverride("panel", FlatCell(HudTheme.Suggested));
        }

        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 4);
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 6);
        margin.AddThemeConstantOverride("margin_right", 6);
        margin.AddThemeConstantOverride("margin_top", 4);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        margin.AddChild(column);
        panel.AddChild(margin);

        var head = new HBoxContainer();
        var name = Small(ability.Name + (kind is null ? "" : $" — {kind}"), 15, 1f, DetailWidth - 70);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.AddThemeColorOverride("font_color", HudTheme.Suggested);
        head.AddChild(name);
        var close = new Button { Text = "Fechar", FocusMode = FocusModeEnum.None };
        close.AddThemeFontSizeOverride("font_size", 11);
        HudTheme.StoneButton(close);
        close.Pressed += () =>
        {
            _detailId = null;
            Refresh();
        };
        head.AddChild(close);
        column.AddChild(head);

        string? previous = null;
        for (var r = 1; r <= max; r++)
        {
            if (Content.AbilityAt(ability.Id, r) is not { } def)
            {
                continue;
            }

            var required = requiredLevel(r);
            var header = $"NH {SkillPlan.Roman(r)}" + (required is { } lv ? $" · nível {lv}" : "") + (r == rank ? " · atual" : "");
            var label = Small(header, 12, 1f, DetailWidth - 16);
            label.AddThemeColorOverride("font_color", r == rank ? HudTheme.Suggested
                : required is { } need && level < need ? HudTheme.Warning : HudTheme.StoneText);
            column.AddChild(label);

            var reach = AbilityText.Reach(def);
            var text = (reach.Length > 0 ? char.ToUpperInvariant(reach[0]) + reach[1..] + "\n" : "") + AbilityText.Describe(def, Content, stats);
            // NH I inteiro; nos seguintes, só as linhas que mudaram (inteiras, com o trecho novo em dourado).
            var segments = SkillDetailText.Compare(previous, text);
            var shown = previous is null ? segments : SkillDetailText.ChangedLines(segments);
            column.AddChild(shown.Count > 0 ? RankText(shown) : Small("Sem mudança nos números.", 12, 0.6f));
            previous = text;
        }

        var note = Small(max > 1 ? "Do NH II em diante, só as linhas que mudam; em dourado, o que é novo. Valores com os seus atributos de agora."
            : "Valores com os seus atributos de agora.", 10, 0.6f, DetailWidth - 16);
        column.AddChild(note);
        return panel;
    }

    /// <summary>Texto de um NH em BBCode: os trechos que mudaram em dourado.</summary>
    private static RichTextLabel RankText(IReadOnlyList<DetailSegment> segments)
    {
        var gold = HudTheme.Suggested.ToHtml(false);
        var bbcode = string.Concat(segments.Select(s =>
        {
            var text = s.Text.Replace("[", "[lb]");
            return (s.Changed ? $"[color=#{gold}]{text}[/color]" : text) + s.Separator;
        }));
        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(DetailWidth - 16, 0),
            MouseFilter = MouseFilterEnum.Ignore,
            Text = bbcode,
        };
        label.AddThemeFontSizeOverride("normal_font_size", 12);
        label.AddThemeColorOverride("default_color", new Color(1, 1, 1, 0.85f));
        return label;
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
