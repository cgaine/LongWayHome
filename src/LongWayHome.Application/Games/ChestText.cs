using LongWayHome.Domain.Games;

namespace LongWayHome.Application.Games;

/// <summary>The Chest screen's words (#164): its hint and the reason a stack didn't move.</summary>
public static class ChestText
{
    /// <summary>"&lt;ui_accept glyph&gt; Move stack": accept on a filled slot moves its stack to the other side.</summary>
    public static string Hint(InputDeviceKind device) => $"{InputGlyphs.For("ui_accept", device)} Move stack";

    /// <summary>
    /// Why a stack didn't move, shown under the grids, or null when it moved: a tool stays in the Backpack (BR-K6), no
    /// room on the other side (BR-K2), or nothing to move.
    /// </summary>
    public static string? Reason(ActionResult result) => result.Rejection switch
    {
        null => null,
        Rejection.NotOffered => "Tools stay in the Backpack",
        Rejection.BackpackFull => "No room for it",
        _ => "Nothing to move",
    };
}
