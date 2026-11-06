using Game.Core.Definitions;
using Game.Core.Simulation;
using MmoRpgClient.Hud;
using Vec2 = System.Numerics.Vector2;

namespace Game.Client.Tests;

/// <summary>T-312: lista de batalha (ordem com o alvo primeiro, histerese, Tab/Shift+Tab e a marca de quem mira o jogador).</summary>
public class BattleListTests
{
    private static readonly Vec2 Player = Vec2.Zero;

    private static BattleCandidate At(int id, float x, MonsterRank rank = MonsterRank.Normal) =>
        new(new EntityId(id), new Vec2(x, 0), $"m{id}", 1, rank, 1f);

    private static int[] Ids(BattleList list) => list.Entries.Select(e => e.Id.Value).ToArray();

    [Fact]
    public void OrdersByDistanceWithTheSelectedTargetFirst()
    {
        var list = new BattleList();
        list.Update([At(1, 5), At(2, 2), At(3, 8)], Player, selected: new EntityId(3), tick: 0, delta: 0);

        Assert.Equal([3, 2, 1], Ids(list));
        Assert.True(list.Entries[0].Selected);
        Assert.Equal([2, 1, 3], list.TabOrder.Select(i => i.Value).ToArray());
    }

    [Fact]
    public void SmallDistanceChangesDoNotReorder()
    {
        var list = new BattleList();
        list.Update([At(1, 3), At(2, 4)], Player, null, 0, 0);

        // O 2 passa a ficar 0,5 tile mais perto: abaixo do limiar, a ordem não muda.
        list.Update([At(1, 4), At(2, 3.5f)], Player, null, 10, 1);
        Assert.Equal([1, 2], Ids(list));

        // Mais de 1 tile de diferença: troca.
        list.Update([At(1, 6), At(2, 3)], Player, null, 20, 1);
        Assert.Equal([2, 1], Ids(list));
    }

    [Fact]
    public void ReordersAtMostFourTimesPerSecond()
    {
        var list = new BattleList();
        list.Update([At(1, 2), At(2, 5)], Player, null, 0, 0);

        list.Update([At(1, 9), At(2, 2)], Player, null, 1, 0.05);
        Assert.Equal([1, 2], Ids(list));

        list.Update([At(1, 9), At(2, 2)], Player, null, 6, 0.25);
        Assert.Equal([2, 1], Ids(list));
    }

    [Fact]
    public void NewMonstersAreInsertedByDistanceAndGoneOnesLeave()
    {
        var list = new BattleList();
        list.Update([At(1, 2), At(2, 8)], Player, null, 0, 0);
        list.Update([At(2, 8), At(3, 5)], Player, null, 1, 0.01);

        Assert.Equal([3, 2], Ids(list));
    }

    [Fact]
    public void NextAndPreviousWalkTheDistanceOrderAndWrap()
    {
        var list = new BattleList();
        list.Update([At(1, 2), At(2, 4), At(3, 6)], Player, null, 0, 0);

        Assert.Equal(1, list.Next(null)!.Value.Value);
        Assert.Equal(2, list.Next(new EntityId(1))!.Value.Value);
        Assert.Equal(1, list.Next(new EntityId(3))!.Value.Value);
        Assert.Equal(3, list.Previous(null)!.Value.Value);
        Assert.Equal(3, list.Previous(new EntityId(1))!.Value.Value);

        // O alvo atual vai para o topo da exibição, mas o Tab continua andando pela distância (não alterna entre dois).
        list.Update([At(1, 2), At(2, 4), At(3, 6)], Player, new EntityId(2), 1, 0.01);
        Assert.Equal(3, list.Next(new EntityId(2))!.Value.Value);
    }

    [Fact]
    public void EmptyListHasNoNextTarget()
    {
        var list = new BattleList();
        list.Update([], Player, null, 0, 0);

        Assert.Empty(list.Entries);
        Assert.Null(list.Next(null));
        Assert.Null(list.Previous(null));
    }

    [Fact]
    public void AimMarkLastsThreeSecondsOrWhileCastingAtThePlayer()
    {
        var list = new BattleList();
        list.NoteAimAtPlayer(new EntityId(1), tick: 100);
        list.Update([At(1, 2), At(2, 3)], Player, null, 110, 0);
        Assert.True(list.Entries[0].TargetsPlayer);
        Assert.False(list.Entries[1].TargetsPlayer);

        list.Update([At(1, 2), At(2, 3)], Player, null, 100 + BattleList.AimMemoryTicks + 1, 0.1,
            castingAtPlayer: new HashSet<EntityId> { new(2) });
        Assert.False(list.Entries[0].TargetsPlayer);
        Assert.True(list.Entries[1].TargetsPlayer);
    }
}
