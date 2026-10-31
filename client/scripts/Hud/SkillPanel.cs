using Game.Core.Combat;
using Game.Core.Combat.Progression;
using Game.Core.Combat.Stats;
using Game.Core.Intents;
using Game.Core.Simulation;
using Godot;
using MmoRpgClient.Match;

namespace MmoRpgClient.Hud;

/// <summary>
/// Tela de habilidades (T-221, tecla K; <c>design/progression-v0.1.md</c> §7): o painel de NH da T-064 ligado aos pontos de
/// verdade. O plano vem do estado do servidor (<see cref="SkillPlan.FromState"/>, com <see cref="SkillPlan.Enforce"/>): o
/// botão + só fica ativo quando há ponto e nível, e manda <see cref="RaiseSkillRankIntent"/>. Mostra o NH atual e a
/// prévia do próximo com as estatísticas do personagem.
/// </summary>
public partial class SkillPanel : ProgressionPanel
{
    private const float TextWidth = 430f;

    protected override string StateKey(EntitySnapshot self)
    {
        var s = self.TryGet<CharacterStats>();
        var p = self.TryGet<CharacterProgress>();
        var st = self.TryGet<AbilityState>();
        return string.Join(",", s?.Level, s?.Str, s?.Dex, s?.Int, s?.Vit, s?.WeaponDamage, p?.SkillPoints,
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
            box.AddChild(Small("Encontro de teste: personagem sem progressão. Os NH vêm do menu e não recebem pontos aqui.", 12, width: TextWidth));
        }
        else
        {
            var free = Small($"Pontos de habilidade livres: {progress.SkillPoints}  ·  usados {plan.PointsUsed} de {plan.PointBudget}",
                13, progress.SkillPoints > 0 ? 1f : 0.75f);
            if (progress.SkillPoints > 0)
            {
                free.AddThemeColorOverride("font_color", HudTheme.Removable);
            }

            box.AddChild(free);
        }

        foreach (var ability in plan.Abilities)
        {
            box.AddChild(new HSeparator());
            var rank = plan.RankOf(ability.Id);
            var line = new HBoxContainer();
            line.AddThemeConstantOverride("separation", 8);
            var name = new Label
            {
                Text = ability.Name + (ability.Signature ? "  (assinatura)" : ""),
                CustomMinimumSize = new Vector2(190, 0),
            };
            if (rank == 0)
            {
                name.Modulate = new Color(1, 1, 1, 0.6f);
            }

            line.AddChild(name);
            line.AddChild(new Label { Text = $"NH {SkillPlan.Roman(rank)} / {SkillPlan.Roman(SkillPlan.MaxRank(ability))}", CustomMinimumSize = new Vector2(80, 0) });

            var blocker = progress is null ? ProgressionRejectReason.NoProgression : plan.RaiseBlocker(ability.Id);
            var next = rank + 1;
            var tip = blocker is { } b ? ProgressionText.Reason(b) : rank == 0 ? "Aprender (põe na barra)" : $"Subir para o NH {SkillPlan.Roman(next)}";
            var id = ability.Id;
            line.AddChild(PlusButton(blocker is null, tip, () => Send?.Invoke(new RaiseSkillRankIntent(self.Id, id))));
            if (next <= SkillPlan.MaxRank(ability))
            {
                var required = plan.RequiredLevel(ability, next);
                var req = Small($"NH {SkillPlan.Roman(next)} exige nível {required}", 11, level >= required ? 0.6f : 1f);
                if (level < required)
                {
                    req.AddThemeColorOverride("font_color", HudTheme.Warning);
                }

                line.AddChild(req);
            }

            box.AddChild(line);
            var current = rank > 0 && Content.AbilityAt(ability.Id, rank) is { } def
                ? $"Atual (NH {SkillPlan.Roman(rank)}): " + Flat(AbilityText.Describe(def, Content, stats))
                : "Não aprendida: fora da barra.";
            box.AddChild(Small(current, 11, rank > 0 ? 0.85f : 0.6f, TextWidth));
            if (next <= SkillPlan.MaxRank(ability) && Content.AbilityAt(ability.Id, next) is { } nextDef)
            {
                box.AddChild(Small($"Próximo (NH {SkillPlan.Roman(next)}): " + Flat(AbilityText.Describe(nextDef, Content, stats)), 11, 0.6f, TextWidth));
            }
            else if (rank > 0)
            {
                box.AddChild(Small("NH máximo.", 11, 0.6f));
            }
        }

        if (progress is not null && stats is not null)
        {
            box.AddChild(new HSeparator());
            box.AddChild(ResetButton(self, AnySpent(progress, stats, Content)));
        }
    }

    private static string Flat(string text) => text.Replace("\n", " · ", StringComparison.Ordinal);
}
