using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Definitions;
using Game.Core.Intents;
using Game.Core.Simulation;
using Game.Core.Simulation.Components;
using Godot;

namespace MmoRpgClient.Hud;

/// <summary>
/// Tela de personagem (T-221, tecla C; <c>design/progression-v0.1.md</c> §3–§4): atributos com o botão + e a prévia do
/// efeito do próximo ponto, atributo sugerido em destaque e as estatísticas derivadas. Tudo vem do snapshot
/// (<see cref="CharacterStats"/>, <see cref="CharacterProgress"/>); as contas da prévia são as do Core.
/// </summary>
public partial class CharacterPanel : ProgressionPanel
{
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

        if (progress is null)
        {
            box.AddChild(Small("Encontro de teste: personagem sem progressão (os pontos não podem ser gastos).", 12, width: 420));
        }
        else
        {
            var points = Small(progress.AttributePoints > 0
                ? $"Pontos de atributo livres: {progress.AttributePoints}"
                : "Sem pontos de atributo livres (5 a cada nível).", 13, progress.AttributePoints > 0 ? 1f : 0.7f);
            if (progress.AttributePoints > 0)
            {
                points.AddThemeColorOverride("font_color", HudTheme.Removable);
            }

            box.AddChild(points);
        }

        var suggested = ProgressionText.Suggested(cls, Content);
        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 4);
        box.AddChild(grid);
        foreach (var a in ProgressionText.Attributes)
        {
            var isSuggested = a == suggested;
            var name = new Label { Text = (isSuggested ? "★ " : "") + $"{ProgressionText.Name(a)} ({ProgressionText.Short(a)})", CustomMinimumSize = new Vector2(132, 0) };
            if (isSuggested)
            {
                name.AddThemeColorOverride("font_color", HudTheme.Suggested);
                name.TooltipText = "Atributo sugerido para a classe (o que mais escala as habilidades dela).";
                name.MouseFilter = MouseFilterEnum.Pass;
            }

            grid.AddChild(name);
            var allocated = progress?.Allocated(a) ?? 0;
            grid.AddChild(new Label
            {
                Text = ProgressionText.Get(stats, a) + (allocated > 0 ? $"  (+{allocated})" : ""),
                CustomMinimumSize = new Vector2(58, 0),
            });
            var preview = Preview(self, cls, stats, a);
            var canSpend = progress is { AttributePoints: > 0 };
            var attribute = a;
            grid.AddChild(PlusButton(canSpend, canSpend ? $"Gastar 1 ponto em {ProgressionText.Name(a)}\n{preview}" : "Sem pontos livres",
                () => Send?.Invoke(new SpendAttributePointIntent(self.Id, attribute))));
            grid.AddChild(Small("+1: " + preview, 11, 0.7f, 220));
        }

        box.AddChild(new HSeparator());
        box.AddChild(Small("Estatísticas", 13, 1f));
        var derived = new GridContainer { Columns = 2 };
        derived.AddThemeConstantOverride("h_separation", 16);
        box.AddChild(derived);
        foreach (var (label, value) in Derived(self, cls, stats))
        {
            derived.AddChild(Small(label, 12, 0.8f));
            derived.AddChild(Small(value, 12, 1f));
        }

        if (progress is not null)
        {
            box.AddChild(new HSeparator());
            box.AddChild(ResetButton(self, AnySpent(progress, stats, Content)));
        }
    }

    /// <summary>Linhas "nome / valor" das estatísticas derivadas.</summary>
    private IEnumerable<(string, string)> Derived(EntitySnapshot self, ClassDefinition cls, CharacterStats s)
    {
        var r = Content.Rules.Stats;
        var health = self.TryGet<Health>();
        var pool = self.TryGet<ResourcePool>();
        yield return ("Vida máxima", ProgressionText.Number(health?.Max ?? StatMath.MaxHealth(cls, s, r), "0"));
        if (pool is not null)
        {
            var regen = cls.Resource.RegenPerSecond + StatMath.ManaRegenBonus(s, r);
            yield return ($"{HudTheme.ResourceName(pool.Type)} máxima", $"{ProgressionText.Number(pool.Max, "0")}  ({ProgressionText.Number(regen, "0.##")}/s)");
        }

        yield return ("Crítico", $"{ProgressionText.Percent(StatMath.CritChance(s, r), "0.##")}  (×{ProgressionText.Number(r.CritMultiplier)})");
        var basic = Content.AbilityAt(cls.BasicAttack, 1);
        yield return ("Velocidade de ataque", $"+{ProgressionText.Percent(StatMath.AttackSpeedFactor(s, r) - 1)}  "
            + $"({basic?.Name ?? "ataque básico"} a cada {ProgressionText.Number(ProgressionText.BasicAttackSeconds(basic, s, r))} s)");
        yield return ("Dano da arma", ProgressionText.Number(s.WeaponDamage, "0.#"));
        yield return ("Armadura", $"{ProgressionText.Number(s.Armor, "0")}  (−{ProgressionText.Percent(ProgressionText.Reduction(s.Armor, s.Level, r), "0")} dano físico)");
        yield return ("Resistência", $"{ProgressionText.Number(s.Resistance, "0")}  (−{ProgressionText.Percent(ProgressionText.Reduction(s.Resistance, s.Level, r), "0")} dano mágico)");
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
