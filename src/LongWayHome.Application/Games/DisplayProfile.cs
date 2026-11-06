namespace LongWayHome.Application.Games;

/// <summary>Display choices that depend on the device: the Steam Deck frame cap and the FPS counter switch.</summary>
public static class DisplayProfile
{
    /// <summary>True when the <c>SteamDeck</c> environment variable is "1".</summary>
    public static bool IsSteamDeck(string? steamDeckVariable) => throw new NotImplementedException();

    /// <summary>The frame cap: 30 on a Steam Deck, 0 (uncapped) elsewhere.</summary>
    public static int MaxFps(bool isSteamDeck) => throw new NotImplementedException();

    /// <summary>True when the user command-line arguments include <c>--show-fps</c>.</summary>
    public static bool ShowFps(IReadOnlyList<string> userArgs) => throw new NotImplementedException();
}
