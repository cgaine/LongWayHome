namespace LongWayHome.Application.Games;

/// <summary>Display choices that depend on the device: the Steam Deck frame cap and the FPS counter switch.</summary>
public static class DisplayProfile
{
    /// <summary>The frame cap on a Steam Deck: 30 fps, to save battery.</summary>
    public const int SteamDeckMaxFps = 30;

    /// <summary>The user command-line argument that shows the FPS counter.</summary>
    public const string ShowFpsArg = "--show-fps";

    /// <summary>True when the <c>SteamDeck</c> environment variable is "1".</summary>
    public static bool IsSteamDeck(string? steamDeckVariable) => steamDeckVariable == "1";

    /// <summary>The frame cap: 30 on a Steam Deck, 0 (uncapped) elsewhere.</summary>
    public static int MaxFps(bool isSteamDeck) => isSteamDeck ? SteamDeckMaxFps : 0;

    /// <summary>True when the user command-line arguments include <c>--show-fps</c>.</summary>
    public static bool ShowFps(IReadOnlyList<string> userArgs) => userArgs.Contains(ShowFpsArg);
}
