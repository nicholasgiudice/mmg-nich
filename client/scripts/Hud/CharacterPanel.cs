using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;
using MmoRpgClient.Presentation;

namespace MmoRpgClient.Hud;

/// <summary>
/// Tela de personagem (T-221, tecla C; refeita na T-082; <c>design/progression-v0.1.md</c> §3–§4): pontos livres em
/// destaque, uma linha larga por atributo (ícone, valor com a divisão classe / pontos / equipamento, +1 e +5 e a prévia
/// do próximo ponto), o atributo sugerido marcado e as estatísticas derivadas agrupadas, com a mudança em verde enquanto
/// um + está sob o mouse. Tudo vem do snapshot (<see cref="CharacterStats"/>, <see cref="CharacterProgress"/>); as contas
/// da prévia são as do Core.
/// </summary>
public partial class CharacterPanel : ProgressionPanel
{
    /// <summary>Pontos que o botão +5 gasta (um <see cref="SpendAttributePointIntent"/> por ponto).</summary>
    private const int ManyPoints = 5;

    private const float RowWidth = 560f;

    /// <summary>Linhas das derivadas: valor de agora e, ao lado, o valor depois do + em foco.</summary>
    private readonly List<(Func<CharacterStats, string> Value, Label Delta)> _derived = new();

    protected override string StateKey(EntitySnapshot self)
    {
        var s = self.TryGet<CharacterStats>();
        var p = self.TryGet<CharacterProgress>();
        var st = self.TryGet<AbilityState>();
        return string.Join(",",
            s?.Level, s?.Str, s?.Dex, s?.Agi, s?.Int, s?.Vit, s?.WeaponDamage, s?.Armor, s?.Resistance, s?.HealthBonus, s?.CritBonus, s?.ManaRegen,
            p?.AttributePoints, p?.SkillPoints, p?.AllocatedTotal, self.TryGet<Health>()?.Max, self.TryGet<ResourcePool>()?.Max,
            st is null ? "" : string.Join(";", st.Known.Select(id => id + st.RankOf(id))));
    }

    protected override void Build(VBoxContainer box, EntitySnapshot self)
    {
        _derived.Clear();
        var info = self.TryGet<UnitInfo>();
        Content.Classes.TryGetValue(info?.DefinitionId ?? "", out var cls);
        var stats = self.TryGet<CharacterStats>();
        var progress = self.TryGet<CharacterProgress>();
        var who = info?.Name is { Length: > 0 } n && n != cls?.Name ? $"{n} — {cls?.Name}" : cls?.Name ?? "Personagem";
        box.AddChild(Title($"{who}, nível {stats?.Level ?? 1}"));
        if (stats is null || cls is null)
        {
            box.AddChild(Small("Sem atributos para mostrar.", 12));
            return;
        }

        var free = progress?.AttributePoints ?? 0;
        if (progress is null)
        {
            box.AddChild(Small("Encontro de teste: personagem sem progressão (os pontos não podem ser gastos).", 12, width: RowWidth));
        }
        else if (free > 0)
        {
            var points = Small(free == 1 ? "1 ponto para distribuir" : $"{free} pontos para distribuir", 17, 1f);
            points.AddThemeColorOverride("font_color", HudTheme.Removable);
            box.AddChild(points);
        }
        else
        {
            box.AddChild(Small("Nenhum ponto para distribuir (5 a cada nível).", 13, 0.7f));
        }

        var suggested = ProgressionText.Suggested(cls, Content);
        foreach (var a in ProgressionText.Attributes)
        {
            box.AddChild(AttributeRow(self, cls, stats, progress, a, a == suggested, free));
        }

        box.AddChild(Small("Estatísticas", 13, 1f));
        var groups = new HBoxContainer();
        groups.AddThemeConstantOverride("separation", 18);
        box.AddChild(groups);
        foreach (var (title, lines) in Derived(self, cls, stats))
        {
            var group = new VBoxContainer();
            group.AddThemeConstantOverride("separation", 2);
            var heading = Small(title, 12, 1f);
            heading.AddThemeColorOverride("font_color", HudTheme.Suggested);
            group.AddChild(heading);
            foreach (var (label, value) in lines)
            {
                group.AddChild(Small(label, 11, 0.7f));
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 6);
                row.AddChild(Small(value(stats), 13, 1f));
                var delta = Small("", 13, 1f);
                delta.AddThemeColorOverride("font_color", HudTheme.Removable);
                row.AddChild(delta);
                group.AddChild(row);
                _derived.Add((value, delta));
            }

            groups.AddChild(group);
        }

        if (progress is not null)
        {
            // Longe dos +: no rodapé, à direita, e com confirmação no segundo clique.
            box.AddChild(new HSeparator());
            var footer = new HBoxContainer();
            footer.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
            footer.AddChild(ResetButton(self, AnySpent(progress, stats, Content)));
            box.AddChild(footer);
        }
    }

    /// <summary>Linha larga do atributo: ícone, nome, valor (classe / pontos / equipamento), +1, +5 e a prévia do próximo ponto.</summary>
    private Control AttributeRow(EntitySnapshot self, ClassDefinition cls, CharacterStats stats, CharacterProgress? progress,
        AttributeKind a, bool isSuggested, int free)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(RowWidth, 0) };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = isSuggested ? new Color(HudTheme.Suggested, 0.10f) : new Color(1, 1, 1, 0.04f),
            BorderColor = isSuggested ? HudTheme.Suggested : new Color(1, 1, 1, 0.08f),
            BorderWidthLeft = isSuggested ? 3 : 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 3,
            ContentMarginBottom = 3,
        });
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        panel.AddChild(row);

        row.AddChild(new TextureRect
        {
            Texture = IconCatalog.Shared.For("attribute." + ProgressionText.ScalingKey(a)),
            CustomMinimumSize = new Vector2(34, 34),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });

        var names = new VBoxContainer { CustomMinimumSize = new Vector2(150, 0) };
        names.AddThemeConstantOverride("separation", 0);
        var name = Small($"{ProgressionText.Name(a)} ({ProgressionText.Short(a)})", 14, 1f);
        if (isSuggested)
        {
            name.Text = "★ " + name.Text;
            name.AddThemeColorOverride("font_color", HudTheme.Suggested);
        }

        names.AddChild(name);
        if (isSuggested)
        {
            var tag = Small("sugerido para a classe", 10, 0.9f);
            tag.AddThemeColorOverride("font_color", HudTheme.Suggested);
            names.AddChild(tag);
        }

        var total = ProgressionText.Get(stats, a);
        var fromClass = cls.Attributes is { } baseSet ? ProgressionText.Get(baseSet, a) : total;
        var allocated = progress?.Allocated(a) ?? 0;
        var fromItems = total - fromClass - allocated;
        if (allocated > 0 || fromItems != 0)
        {
            names.AddChild(Small(ProgressionText.Breakdown(fromClass, allocated, fromItems), 10, 0.6f));
        }

        row.AddChild(names);

        var value = Small(total.ToString(System.Globalization.CultureInfo.InvariantCulture), 20, 1f);
        value.CustomMinimumSize = new Vector2(34, 0);
        value.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(value);

        var buttons = new HBoxContainer { CustomMinimumSize = new Vector2(78, 0) };
        buttons.AddThemeConstantOverride("separation", 4);
        if (free > 0)
        {
            var many = Math.Min(ManyPoints, free);
            buttons.AddChild(SpendButton(self, cls, stats, a, 1, "+1"));
            buttons.AddChild(SpendButton(self, cls, stats, a, many, $"+{many}"));
        }

        row.AddChild(buttons);
        row.AddChild(Small("+1: " + Preview(self, cls, stats, a), 11, 0.75f, 230));
        return panel;
    }

    /// <summary>Botão que gasta <paramref name="amount"/> pontos; com o mouse em cima, as derivadas mostram a mudança.</summary>
    private Button SpendButton(EntitySnapshot self, ClassDefinition cls, CharacterStats stats, AttributeKind a, int amount, string text)
    {
        var button = PlusButton(true, $"Gastar {amount} {(amount == 1 ? "ponto" : "pontos")} em {ProgressionText.Name(a)}", () =>
        {
            for (var i = 0; i < amount; i++)
            {
                Send?.Invoke(new SpendAttributePointIntent(self.Id, a));
            }
        });
        button.Text = text;
        button.CustomMinimumSize = new Vector2(36, 26);
        var after = ProgressionText.With(stats, a, amount);
        button.MouseEntered += () => ShowDeltas(stats, after);
        button.MouseExited += () => ShowDeltas(stats, null);
        return button;
    }

    private void ShowDeltas(CharacterStats before, CharacterStats? after)
    {
        foreach (var (value, delta) in _derived)
        {
            var next = after is null ? "" : value(after);
            delta.Text = next.Length > 0 && next != value(before) ? "→ " + next : "";
        }
    }

    /// <summary>
    /// Estatísticas derivadas agrupadas. Cada linha é uma função das estatísticas, para a mesma conta mostrar o valor de agora e
    /// o de depois do + em foco. Vida e Mana partem do máximo do snapshot (que inclui o equipamento) mais a diferença do Core.
    /// </summary>
    private IEnumerable<(string Title, List<(string Label, Func<CharacterStats, string> Value)> Lines)> Derived(
        EntitySnapshot self, ClassDefinition cls, CharacterStats now)
    {
        var r = Content.Rules.Stats;
        var health = self.TryGet<Health>();
        var pool = self.TryGet<ResourcePool>();
        var vital = new List<(string, Func<CharacterStats, string>)>
        {
            ("Vida máxima", s => ProgressionText.Number((health?.Max ?? StatMath.MaxHealth(cls, now, r)) + StatMath.MaxHealth(cls, s, r) - StatMath.MaxHealth(cls, now, r), "0")),
        };
        if (pool is not null)
        {
            var mana = pool.Type == EntityFactory.ManaResource;
            vital.Add(($"{HudTheme.ResourceName(pool.Type)} máxima", s => ProgressionText.Number(
                pool.Max + (mana ? StatMath.MaxMana(s, r) - StatMath.MaxMana(now, r) : 0), "0")));
            vital.Add(("Recuperação", s => ProgressionText.Number(cls.Resource.RegenPerSecond + StatMath.ManaRegenBonus(s, r), "0.##") + "/s"));
        }

        yield return ("Vida e Mana", vital);

        var basic = Content.AbilityAt(cls.BasicAttack, 1);
        yield return ("Ataque",
        [
            ("Dano da arma", s => ProgressionText.Number(s.WeaponDamage, "0.#")),
            ($"Crítico (×{ProgressionText.Number(r.CritMultiplier)})", s => ProgressionText.Percent(StatMath.CritChance(s, r), "0.##")),
            ("Velocidade de ataque", s => "+" + ProgressionText.Percent(StatMath.AttackSpeedFactor(s, r) - 1, "0.#")),
            ($"{basic?.Name ?? "Ataque básico"} a cada", s => ProgressionText.Number(ProgressionText.BasicAttackSeconds(basic, s, r)) + " s"),
        ]);
        yield return ("Defesa",
        [
            ("Armadura", s => $"{ProgressionText.Number(s.Armor, "0")} (−{ProgressionText.Percent(ProgressionText.Reduction(s.Armor, s.Level, r), "0")} físico)"),
            ("Resistência", s => $"{ProgressionText.Number(s.Resistance, "0")} (−{ProgressionText.Percent(ProgressionText.Reduction(s.Resistance, s.Level, r), "0")} mágico)"),
        ]);
    }

    /// <summary>
    /// Efeito de +1 no atributo: as duas habilidades da barra que mais mudam ("Golpe Pesado 51 → 53") e as derivadas que
    /// mudam (vida, Mana, crítico, velocidade de ataque).
    /// </summary>
    private string Preview(EntitySnapshot self, ClassDefinition cls, CharacterStats before, AttributeKind a)
    {
        var r = Content.Rules.Stats;
        var after = ProgressionText.With(before, a, 1);
        var parts = new List<string>();
        var state = self.TryGet<AbilityState>();
        var bar = state is null ? [cls.BasicAttack] : state.Known.ToList();
        var changes = bar
            .Select(id => Content.AbilityAt(id, state?.RankOf(id) ?? 1))
            .OfType<AbilityDefinition>()
            .Select(def => (def.Name, Before: AbilityText.MainValue(def, before), After: AbilityText.MainValue(def, after)))
            .Where(x => x.Before is not null && x.After > x.Before)
            .OrderByDescending(x => x.After - x.Before)
            .Take(2);
        parts.AddRange(changes.Select(x => $"{x.Name} {ProgressionText.Number(x.Before!.Value, "0")} → {ProgressionText.Number(x.After!.Value, "0")}"));

        var healthDelta = StatMath.MaxHealth(cls, after, r) - StatMath.MaxHealth(cls, before, r);
        if (healthDelta > 0)
        {
            parts.Add($"vida +{ProgressionText.Number(healthDelta, "0")}");
        }

        if (self.TryGet<ResourcePool>() is { Type: EntityFactory.ManaResource })
        {
            var manaDelta = StatMath.MaxMana(after, r) - StatMath.MaxMana(before, r);
            if (manaDelta > 0)
            {
                parts.Add($"Mana +{ProgressionText.Number(manaDelta, "0")} e +{ProgressionText.Number(StatMath.ManaRegenBonus(after, r) - StatMath.ManaRegenBonus(before, r))}/s");
            }
        }

        var critBefore = StatMath.CritChance(before, r);
        var critAfter = StatMath.CritChance(after, r);
        if (critAfter > critBefore)
        {
            parts.Add($"crítico {ProgressionText.Percent(critBefore, "0.##")} → {ProgressionText.Percent(critAfter, "0.##")}");
        }

        var basic = Content.AbilityAt(cls.BasicAttack, 1);
        var swingBefore = ProgressionText.BasicAttackSeconds(basic, before, r);
        var swingAfter = ProgressionText.BasicAttackSeconds(basic, after, r);
        if (swingAfter < swingBefore)
        {
            parts.Add($"ataque a cada {ProgressionText.Number(swingBefore)} → {ProgressionText.Number(swingAfter)} s");
        }
        else if (StatMath.AttackSpeedFactor(after, r) > StatMath.AttackSpeedFactor(before, r))
        {
            parts.Add($"velocidade de ataque +{ProgressionText.Percent(StatMath.AttackSpeedFactor(after, r) - StatMath.AttackSpeedFactor(before, r), "0.#")}");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : "sem efeito nas habilidades da sua barra";
    }
}
