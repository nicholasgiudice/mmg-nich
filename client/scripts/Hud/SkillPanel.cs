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
/// básico, as habilidades da classe (assinatura em destaque) e a poção, com os pontos do NH embaixo de cada ícone. O plano
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
    private static readonly Color ItemBorder = new(0.55f, 0.95f, 0.55f, 0.6f);

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
        box.AddChild(Title("Habilidades"));
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

        foreach (var id in state.Consumables.Count > 0 ? state.Consumables : cls.Consumables)
        {
            if (Content.AbilityAt(id, 1) is { } item)
            {
                var left = item.Charges is { } max ? $"{Math.Max(0, max - state.ChargesUsed.GetValueOrDefault(id))}/{max} cargas" : "consumível";
                grid.AddChild(Cell(item, IconCatalog.Shared.For(id), ItemBorder, left, Tip(item.Name + " — consumível (Q)", item, stats, null),
                    draggable: true, plus: null, req: null));
            }
        }

        box.AddChild(Small("Arraste um ícone para a barra (teclas 1 a 6). Habilidade não aprendida não vai para a barra.", 11, 0.6f,
            Columns * CellWidth));

        if (progress is not null && stats is not null)
        {
            box.AddChild(new HSeparator());
            var footer = new HBoxContainer();
            footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            footer.AddChild(ResetButton(self, AnySpent(progress, stats, Content)));
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

        var pips = new string('●', rank) + new string('○', Math.Max(0, max - rank));
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
            pips, tip, draggable: rank > 0, plus, req, dim: rank == 0);
    }

    /// <summary>Uma célula da grade: ícone (com o + no canto), nome, linha de baixo (pontos do NH ou tipo) e o nível pedido.</summary>
    private static PanelContainer Cell(AbilityDefinition def, Texture2D? icon, Color border, string below, string tip, bool draggable,
        Button? plus, Label? req, bool dim = false)
    {
        var cell = new PanelContainer { CustomMinimumSize = new Vector2(CellWidth, 0), TooltipText = tip };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(1, 1, 1, 0.04f),
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });
        var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 1);
        cell.AddChild(column);

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
            plus.Position = new Vector2(IconSize - 12, -6);
            plus.AddThemeColorOverride("font_color", HudTheme.Removable);
            holder.AddChild(plus);
        }

        column.AddChild(holder);
        var name = Small(def.Name, 11, 1f, CellWidth - 8);
        name.HorizontalAlignment = HorizontalAlignment.Center;
        column.AddChild(name);
        var line = Small(below, 11, 0.85f);
        line.HorizontalAlignment = HorizontalAlignment.Center;
        line.AddThemeColorOverride("font_color", HudTheme.Suggested);
        column.AddChild(line);
        if (req is not null)
        {
            req.HorizontalAlignment = HorizontalAlignment.Center;
            column.AddChild(req);
        }

        return cell;
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
        else if (current is not null && !current.Id.StartsWith("item.", StringComparison.Ordinal) && current.Charges is null)
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
