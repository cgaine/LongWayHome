namespace LongWayHome.Application.Games;

/// <summary>The HUD gold text: "Gold 100".</summary>
public static class GoldText
{
    /// <summary>"Gold " and the gold held, as is (BR-P5).</summary>
    public static string For(int gold) => $"Gold {gold}";
}
