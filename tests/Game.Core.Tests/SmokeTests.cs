namespace Game.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Assembly_Loads()
    {
        var assembly = typeof(SmokeTests).Assembly;
        Assert.NotNull(assembly);
    }
}
