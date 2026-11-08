namespace LongWayHome.Application.Games;

/// <summary>The hint text of talking and the dialogue box (#138), with the glyphs of <see cref="InputGlyphs"/>.</summary>
public static class DialogueText
{
    /// <summary>The HUD hint near a present resident: "&lt;interact glyph&gt; Talk", e.g. "[F] Talk".</summary>
    public static string TalkHint(InputDeviceKind device) => throw new NotImplementedException();

    /// <summary>The box's advance hint: "&lt;interact glyph&gt; Next", or "… Close" on the last line.</summary>
    public static string AdvanceHint(InputDeviceKind device, bool isLastLine) => throw new NotImplementedException();

    /// <summary>The box's skip hint: "&lt;open_menu glyph&gt; Skip", e.g. "[Tab] Skip".</summary>
    public static string SkipHint(InputDeviceKind device) => throw new NotImplementedException();
}
