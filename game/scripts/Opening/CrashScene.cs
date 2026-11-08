#nullable enable

using System;
using Godot;

namespace LongWayHome.Game.Opening;

/// <summary>
/// The Opening's crash scene (#139), about <see cref="Seconds"/> long and built in code: a camera on the farm looking
/// south over the treeline, a pale blue-silver (<see cref="StreakColor"/>) streak crossing the sky toward the
/// mountains, a flash lighting the treeline where it comes down, then a fade to black. <see cref="Skip"/> jumps to the
/// black at once. <see cref="Finished"/> fires once, on black; <see cref="FadeIn"/> then fades the black away and
/// frees the scene. Always processes, so it plays while <see cref="OpeningSequence"/> holds the tree paused. Purely
/// cosmetic; holds no rules.
/// </summary>
public partial class CrashScene : Node3D
{
    /// <summary>How long the crash scene plays, in seconds.</summary>
    public const double Seconds = 6.0;

    /// <summary>The streak's pale blue-silver.</summary>
    public static readonly Color StreakColor = new("#C9D6E8");

    private const double StreakSeconds = 3.0;

    private const double FlashUpSeconds = 0.15;

    private const double FlashDownSeconds = 0.9;

    private const double FadeOutSeconds = Seconds - StreakSeconds - FlashUpSeconds - FlashDownSeconds - HoldSeconds;

    private const double HoldSeconds = 0.35;

    private const double FadeInSeconds = 0.8;

    /// <summary>The camera: on the farm's south half, looking south and a little up over the treeline.</summary>
    private static readonly Vector3 Eye = new(0f, 2.2f, 4f);

    private static readonly Vector3 EyeTarget = new(0f, 6f, 40f);

    /// <summary>The streak comes in high from the south-east and comes down beyond the south treeline.</summary>
    private static readonly Vector3 StreakFrom = new(-38f, 34f, 70f);

    private static readonly Vector3 StreakTo = new(10f, 3f, 46f);

    /// <summary>Where the flash lights the treeline.</summary>
    private static readonly Vector3 FlashAt = new(10f, 5f, 34f);

    /// <summary>Raised once, when the screen is black (played through or skipped).</summary>
    public event Action? Finished;

    private Camera3D _camera = null!;

    private MeshInstance3D _streak = null!;

    private OmniLight3D _flash = null!;

    private ColorRect _white = null!;

    private ColorRect _black = null!;

    private Tween? _tween;

    private bool _finished;

    /// <summary>Whether the scene is still playing toward black.</summary>
    public bool IsPlaying => !_finished;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;

        _camera = new Camera3D { Name = "CrashCamera", Fov = 60f };
        AddChild(_camera);
        _camera.LookAtFromPosition(Eye, EyeTarget, Vector3.Up);
        _camera.MakeCurrent();

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = StreakColor,
            EmissionEnabled = true,
            Emission = StreakColor,
        };
        _streak = new MeshInstance3D
        {
            Name = "Streak",
            Mesh = new BoxMesh { Size = new Vector3(0.35f, 0.35f, 7f) },
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        AddChild(_streak);
        _streak.LookAtFromPosition(StreakFrom, StreakTo, Vector3.Up);

        _flash = new OmniLight3D
        {
            Name = "Flash",
            LightColor = StreakColor,
            LightEnergy = 0f,
            OmniRange = 40f,
            Position = FlashAt,
        };
        AddChild(_flash);

        var overlay = new CanvasLayer { Name = "Overlay", Layer = 50 };
        AddChild(overlay);
        _white = FullScreen(new Color(StreakColor, 0f));
        _black = FullScreen(new Color(0f, 0f, 0f, 0f));
        overlay.AddChild(_white);
        overlay.AddChild(_black);

        _tween = CreateTween();
        _tween.TweenProperty(_streak, "position", StreakTo, StreakSeconds);
        _tween.TweenCallback(Callable.From(() => _streak.Visible = false));
        _tween.TweenProperty(_flash, "light_energy", 16.0, FlashUpSeconds);
        _tween.Parallel().TweenProperty(_white, "color:a", 0.7, FlashUpSeconds);
        _tween.TweenProperty(_flash, "light_energy", 0.0, FlashDownSeconds);
        _tween.Parallel().TweenProperty(_white, "color:a", 0.0, FlashDownSeconds);
        _tween.TweenInterval(HoldSeconds);
        _tween.TweenProperty(_black, "color:a", 1.0, FadeOutSeconds);
        _tween.TweenCallback(Callable.From(Finish));
    }

    /// <summary>Jumps to the black screen and finishes (<c>open_menu</c> during the crash).</summary>
    public void Skip()
    {
        if (_finished)
        {
            return;
        }

        _tween?.Kill();
        _streak.Visible = false;
        _flash.LightEnergy = 0f;
        _white.Color = _white.Color with { A = 0f };
        _black.Color = _black.Color with { A = 1f };
        Finish();
    }

    /// <summary>Fades the black away (the farm's own camera must be current by then), then frees the scene.</summary>
    public void FadeIn(Action done)
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(_black, "color:a", 0.0, FadeInSeconds);
        _tween.TweenCallback(Callable.From(() =>
        {
            done();
            QueueFree();
        }));
    }

    /// <summary>Hands the view back from the crash camera.</summary>
    public void ReleaseCamera() => _camera.ClearCurrent(enableNext: false);

    private void Finish()
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        Finished?.Invoke();
    }

    private static ColorRect FullScreen(Color color)
    {
        var rect = new ColorRect { Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }
}
