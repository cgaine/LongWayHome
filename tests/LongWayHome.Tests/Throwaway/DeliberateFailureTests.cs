namespace LongWayHome.Tests.Throwaway;

// Throwaway: proves CI / test fails on a failing test (#15 AC3). Never merged.
public class DeliberateFailureTests
{
    [Xunit.Fact]
    public void Deliberately_fails() => Xunit.Assert.Equal(1, 2);
}
