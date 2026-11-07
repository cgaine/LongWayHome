#nullable enable

using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// Lights the ripe crops' night glows on the farm (#94): sets the <c>art_night</c> shader global to
/// <see cref="NightGlow.At"/> of the clock on start, on <see cref="GameSession.ClockChanged"/> and on
/// <see cref="GameSession.NightCompleted"/> (back at 06:00, glows off), and back to 0 on leaving the farm so the title
/// screen and the art sandbox start without it. Reads the clock and holds no rules.
/// </summary>
public partial class NightGlowView : Node
{
    private const string GameRootPath = "/root/GameRoot";

    private const string ArtNightGlobal = "art_night";

    private GameSession? _session;

    private float? _applied;

    public override void _Ready()
    {
        // GameRoot.Session is null when the data failed to load and GameRoot is already quitting.
        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            SetArtNight(0f);
            return;
        }

        _session.ClockChanged += Apply;
        _session.NightCompleted += Apply;
        Apply();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.ClockChanged -= Apply;
            _session.NightCompleted -= Apply;
        }

        RenderingServer.GlobalShaderParameterSet(ArtNightGlobal, 0f);
        _applied = null;
    }

    private void Apply() => SetArtNight(NightGlow.At(_session!.Game.ClockMinutes));

    /// <summary>Sets <c>art_night</c> only when it changes: the clock moves every minute, the glow only at dusk.</summary>
    private void SetArtNight(float value)
    {
        if (_applied == value)
        {
            return;
        }

        RenderingServer.GlobalShaderParameterSet(ArtNightGlobal, value);
        _applied = value;
    }
}
