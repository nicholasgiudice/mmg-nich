using Game.Core.Definitions;
using Game.Core.Simulation;
using MmoRpgClient.Hud;
using Vec2 = System.Numerics.Vector2;

namespace Game.Client.Tests;

/// <summary>T-382: a lista mostra só as primeiras linhas, o Tab passa por todos, e a miniatura fica em escala inteira.</summary>
public class BattleListCompactTests
{
    private static BattleCandidate At(int id, float x) =>
        new(new EntityId(id), new Vec2(x, 0), $"m{id}", 1, MonsterRank.Normal, 1f, "monster.field_rat");

    private static List<BattleCandidate> Eight() => Enumerable.Range(1, 8).Select(i => At(i, i * 2)).ToList();

    [Fact]
    public void ShowsOnlyTheFirstFiveRowsWithTheSelectedTargetFirst()
    {
        var list = new BattleList();
        list.Update(Eight(), Vec2.Zero, selected: new EntityId(7), tick: 0, delta: 0);

        Assert.Equal(8, list.Entries.Count);
        Assert.Equal(BattleList.ShownRows, list.Shown.Count);
        Assert.Equal([7, 1, 2, 3, 4], list.Shown.Select(e => e.Id.Value).ToArray());
        Assert.Equal("monster.field_rat", list.Shown[0].DefinitionId);
    }

    [Fact]
    public void FewMonstersAreAllShown()
    {
        var list = new BattleList();
        list.Update([At(1, 2), At(2, 4)], Vec2.Zero, null, 0, 0);

        Assert.Equal(2, list.Shown.Count);
    }

    [Fact]
    public void TabReachesMonstersBeyondTheShownRows()
    {
        var list = new BattleList();
        list.Update(Eight(), Vec2.Zero, null, 0, 0);

        var visited = new List<int>();
        EntityId? current = null;
        for (var i = 0; i < 8; i++)
        {
            current = list.Next(current);
            visited.Add(current!.Value.Value);
            list.Update(Eight(), Vec2.Zero, current, i + 1, 0.01);
        }

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], visited);
    }

    [Fact]
    public void SmallContentIsEnlargedByTheLargestIntegerThatFits()
    {
        var cut = BattleThumbnail.Fit(10, 20, 12, 9, box: 28);

        Assert.Equal(new ThumbnailCut(10, 20, 12, 9, 24, 18), cut);
    }

    [Fact]
    public void LargeContentIsReducedByAnInteger()
    {
        // Rato de Campo virado para baixo: 23×47 → metade; lobo: 23×58 → um terço.
        Assert.Equal(new ThumbnailCut(0, 0, 23, 47, 11, 23), BattleThumbnail.Fit(0, 0, 23, 47, 28));
        Assert.Equal(new ThumbnailCut(0, 0, 23, 58, 7, 19), BattleThumbnail.Fit(0, 0, 23, 58, 28));
    }

    [Fact]
    public void EmptyContentDrawsNothing()
    {
        Assert.Equal(0, BattleThumbnail.Fit(0, 0, 0, 0, 28).DrawWidth);
    }
}
