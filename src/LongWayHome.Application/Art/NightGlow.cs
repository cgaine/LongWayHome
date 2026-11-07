namespace LongWayHome.Application.Art;

/// <summary>
/// How strongly the crop glows show at a clock time (#94): the <c>art_night</c> shader global, 0 to 1. The glows wake
/// as dusk ends (ART_DIRECTION §3): 0 up to 19:30, rising linearly to 1 at 20:30, and 1 until the day ends at 02:00.
/// Pure C#, no Godot; <c>NightGlowView</c> only applies it.
/// </summary>
public static class NightGlow
{
    /// <summary>19:30, when the glows start to fade in.</summary>
    public const int FadeInStartMinutes = 1170;

    /// <summary>20:30, when the glows are full.</summary>
    public const int FadeInEndMinutes = 1230;

    /// <summary>
    /// The glow strength at <paramref name="clockMinutes"/> (minutes after midnight, 360 to 1560). Values outside the
    /// day clamp to its nearest end: 0 before 06:00, 1 after 02:00.
    /// </summary>
    public static float At(int clockMinutes)
    {
        if (clockMinutes <= FadeInStartMinutes)
        {
            return 0f;
        }

        if (clockMinutes >= FadeInEndMinutes)
        {
            return 1f;
        }

        return (float)(clockMinutes - FadeInStartMinutes) / (FadeInEndMinutes - FadeInStartMinutes);
    }
}
