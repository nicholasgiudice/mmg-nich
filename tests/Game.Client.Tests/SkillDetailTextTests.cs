using MmoRpgClient.Hud;

namespace Game.Client.Tests;

/// <summary>Detalhe das habilidades: o texto de cada NH em trechos, com a marca do que mudou.</summary>
public class SkillDetailTextTests
{
    [Fact]
    public void SplitsLinesEffectsAndTimingKeepingTheSeparators()
    {
        var parts = SkillDetailText.Split("Custo 30 mana, recarga 6 s\nDano 51; interrompe");

        Assert.Equal(["Custo 30 mana", "recarga 6 s", "Dano 51", "interrompe"], parts.Select(p => p.Text).ToArray());
        Assert.Equal([", ", "\n", "; ", ""], parts.Select(p => p.Separator).ToArray());
        Assert.Equal("Custo 30 mana, recarga 6 s\nDano 51; interrompe", string.Concat(parts.Select(p => p.Text + p.Separator)));
    }

    [Fact]
    public void FirstRankHasNothingMarked()
    {
        Assert.All(SkillDetailText.Compare(null, "Custo 30 mana\nDano 51; interrompe"), p => Assert.False(p.Changed));
    }

    [Fact]
    public void MarksOnlyWhatChangedFromThePreviousRank()
    {
        var previous = "Custo 30 mana, recarga 6 s\nDano 51; interrompe";
        var current = "Custo 30 mana, recarga 6 s\nDano 60; interrompe; Atordoado 0,5 s";

        var changed = SkillDetailText.Compare(previous, current).Where(p => p.Changed).Select(p => p.Text).ToArray();

        Assert.Equal(["Dano 60", "Atordoado 0,5 s"], changed);
    }

    [Fact]
    public void KeepsOnlyTheLinesWithAChangeAndTheWholeLine()
    {
        var previous = "Corpo a corpo (1,9, arco de 90°)\nCusto 30 mana, recarga 6 s\nDano 51; interrompe";
        var current = "Corpo a corpo (1,9, arco de 90°)\nCusto 30 mana, recarga 6 s\nDano 60; interrompe";

        var kept = SkillDetailText.ChangedLines(SkillDetailText.Compare(previous, current));

        Assert.Equal("Dano 60; interrompe", string.Concat(kept.Select(p => p.Text + p.Separator)));
    }

    [Fact]
    public void NoChangeKeepsNothing()
    {
        Assert.Empty(SkillDetailText.ChangedLines(SkillDetailText.Compare("Dano 51", "Dano 51")));
    }

    [Fact]
    public void IgnoresTheCapitalLetterAtTheStartOfALine()
    {
        // "dano 51" vira o primeiro trecho da linha (com maiúscula) quando o custo some: não é mudança.
        var parts = SkillDetailText.Compare("Interrompe; dano 51", "Dano 51; interrompe");

        Assert.All(parts, p => Assert.False(p.Changed));
    }
}
