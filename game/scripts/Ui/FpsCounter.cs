#nullable enable

using System;
using Godot;
using LongWayHome.Application.Games;

namespace LongWayHome.Game.Ui;

/// <summary>
/// The FPS counter, top right under the date: "30 fps · min 28" (<see cref="FpsText"/>), the current frames per
/// second and the lowest over the last 10 seconds (<see cref="FpsWindow"/>). Created in code by <see cref="Hud"/>
/// only when the game is started with <c>--show-fps</c>, so no node exists without it. Samples every half second of
/// real time and keeps updating while the game is paused. Holds no rules.
/// </summary>
public partial class FpsCounter : Label
{
    /// <summary>The node name, so a probe or test can find it under the HUD.</summary>
    public const string NodeName = "FpsCounter";

    private const double SampleIntervalSeconds = 0.5;

    private const int CounterFontSize = 22;

    private const int OutlineSize = 4;

    private readonly FpsWindow _window = new();

    private double _sinceSample;

    public FpsCounter()
    {
        Name = NodeName;
        ProcessMode = ProcessModeEnum.Always;
        MouseFilter = MouseFilterEnum.Ignore;
        HorizontalAlignment = HorizontalAlignment.Right;

        // Top right, directly under the HUD's DateLabel (offsets -216, 84, -16, 116).
        AnchorLeft = 1;
        AnchorRight = 1;
        OffsetLeft = -216;
        OffsetTop = 118;
        OffsetRight = -16;
        OffsetBottom = 150;
        GrowHorizontal = GrowDirection.Begin;

        AddThemeFontSizeOverride("font_size", CounterFontSize);
        AddThemeConstantOverride("outline_size", OutlineSize);
        AddThemeColorOverride("font_outline_color", Colors.Black);
    }

    public override void _Process(double delta)
    {
        // ProcessMode Always: delta keeps running while the tree is paused, so this is real time.
        _sinceSample += delta;
        if (_sinceSample < SampleIntervalSeconds)
        {
            return;
        }

        _sinceSample = 0;
        Sample();
    }

    private void Sample()
    {
        var fps = (int)Math.Round(Engine.GetFramesPerSecond());
        if (fps <= 0)
        {
            // The engine measures once a second; before its first measurement 0 would pin the minimum for 10 s.
            return;
        }

        _window.Add(Time.GetTicksMsec() / 1000.0, fps);
        Text = FpsText.For(fps, _window.Lowest);
    }
}
