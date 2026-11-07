using LongWayHome.Domain.Players;

namespace LongWayHome.Application.Games;

/// <summary>The held hotbar position, 0–5 within the current row.</summary>
public static class HotbarPosition
{
    /// <summary>Steps <paramref name="position"/> by <paramref name="delta"/>, wrapping within 0–5.</summary>
    public static int Step(int position, int delta) =>
        (((position + delta) % Backpack.RowLength) + Backpack.RowLength) % Backpack.RowLength;
}
