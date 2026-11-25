using Game.Core.Combat;
using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>Texto das habilidades no tooltip e na tela de habilidades: descrição e efeitos em frases.</summary>
public class AbilityTextTests
{
    private static readonly Lazy<ContentCatalog> Content = new(SkillPlanTests.LoadContent);

    private static string Describe(string id, int rank = 1) =>
        AbilityText.Describe(Content.Value.AbilityAt(id, rank)!, Content.Value);

    [Fact]
    public void Summary_comes_from_the_data_description()
    {
        var def = Content.Value.Abilities["warrior.heavy_strike"];
        Assert.StartsWith("Golpe forte à frente que interrompe", AbilityText.Summary(def), StringComparison.Ordinal);
    }

    [Fact]
    public void Wrapped_summary_fits_the_tooltip_width()
    {
        foreach (var def in Content.Value.Abilities.Values.Where(a => a.Description is not null))
        {
            var wrapped = AbilityText.WrappedSummary(def);
            Assert.All(wrapped.Split('\n'), line => Assert.True(line.Length <= AbilityText.TooltipWidth, $"{def.Id}: '{line}'"));
            Assert.Equal(AbilityText.Summary(def), wrapped.Replace('\n', ' '));
        }
    }

    [Fact]
    public void Effects_are_full_sentences_one_per_line()
    {
        var text = Describe("warrior.heavy_strike", 3);
        var lines = text.Split('\n');
        Assert.Equal("Custo 30 de Mana, recarga 6 s, conjuração 0,4 s (parado)", lines[0]);
        Assert.Contains("Causa 21 + 1,4 × ARMA + 3,5 × FOR de dano físico", lines);
        Assert.Contains("Interrompe a conjuração do alvo", lines);
        Assert.Contains("Aplica Atordoado por 0,5 s (não age)", lines);
        Assert.Contains("+20 de ameaça", lines);
    }

    [Fact]
    public void Statuses_explain_what_they_do()
    {
        Assert.Contains("Você ganha Guarda Firme por 4 s (recebe 60% menos dano de frente, anda 50% mais devagar)", Describe("warrior.firm_guard"), StringComparison.Ordinal);
        Assert.Contains("causa 20% mais dano", Describe("berserker.fury"), StringComparison.Ordinal);
        Assert.Contains("recebe 20% mais dano", Describe("berserker.fury"), StringComparison.Ordinal);
    }

    [Fact]
    public void Taunt_and_areas_are_spelled_out()
    {
        Assert.Contains("Obriga os monstros a atacar você por 3 s", Describe("warrior.taunt"), StringComparison.Ordinal);
        var trap = Describe("archer.containment_trap");
        Assert.Contains("no primeiro inimigo que pisar", trap, StringComparison.Ordinal);
        Assert.Contains("não consegue andar", trap, StringComparison.Ordinal);
    }

    [Fact]
    public void Passives_say_they_are_always_on()
    {
        var text = Describe("berserker.blood_thirst");
        Assert.StartsWith("Passiva: sempre ativa", text, StringComparison.Ordinal);
        Assert.Contains("do dano causado como vida", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_stats_the_scaling_is_shown()
    {
        Assert.Contains("Cura 60 + 2 × VIT de vida", Describe("item.minor_healing_potion"), StringComparison.Ordinal);
    }

    [Fact]
    public void Every_player_ability_text_is_readable()
    {
        // Nenhuma linha vazia nem id cru ("status.x") vazando no texto.
        foreach (var def in Content.Value.Abilities.Values.Where(a => a.Description is not null))
        {
            for (var rank = 1; rank <= def.HighestRank; rank++)
            {
                var text = Describe(def.Id, rank);
                Assert.DoesNotContain("status.", text, StringComparison.Ordinal);
                Assert.DoesNotContain("area.", text, StringComparison.Ordinal);
                Assert.All(text.Split('\n'), line => Assert.False(string.IsNullOrWhiteSpace(line), def.Id));
            }
        }
    }
}
