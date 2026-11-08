#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;
using LongWayHome.Game.Ui;
using LongWayHome.Game.World;

namespace LongWayHome.Game.Opening;

/// <summary>
/// Maggie's Opening on the farm (#139, BR-I1): glue that plays each step of an <see cref="OpeningFlow"/> over
/// <see cref="GameRoot.Session"/> and reports back to it. Its children are Maggie (a <c>Resident.tscn</c>), the
/// <c>Waypoints</c> of her walk from the farm's south arrival point to beside the door, and the <c>ExitPoint</c> she
/// leaves by. When the flow isn't active (a Game past the Opening, BR-I1) it removes Maggie and does nothing else.
/// Otherwise: the <see cref="CrashScene"/> with the tree paused, the Player hidden and the clock off; <c>open_menu</c>
/// skips to its end. Then a fade in with the Player at the south arrival point and Maggie beside him; each block of
/// her lines through <see cref="DialogueBox.Show"/> (which pauses the tree, BR-G2), its <c>Finished</c> reported as
/// <see cref="OpeningFlow.LinesFinished"/>; her walk at the tuning's walk speed, waiting at the last waypoint until
/// the Player is within <see cref="NearTiles"/>, reported as <see cref="OpeningFlow.WalkArrived"/>. The till, plant
/// and water steps are the Player's own Use; the flow follows them. The <c>DayClock</c> runs only while
/// <see cref="OpeningFlow.ClockRuns"/>. When the flow is done Maggie walks back to the south exit and is removed.
/// While <see cref="IsOpeningActive"/>, <see cref="AreaExit"/> and <see cref="DoorSleep"/> do nothing and the Hud shows
/// <see cref="StepHint"/>. Always processes (the crash plays under a paused tree); her walk stops while the tree is
/// paused. Holds no rules.
/// </summary>
public partial class OpeningSequence : Node3D
{
    /// <summary>The group the sequence joins, so the exit, the door and the Hud can find it.</summary>
    public const string Group = "opening_sequence";

    /// <summary>How close, in tiles, the Player must come before Maggie speaks at the farmhouse.</summary>
    public const float NearTiles = 2f;

    /// <summary>Where Maggie stands beside the Player at the arrival point.</summary>
    private static readonly Vector3 BesideArrival = new(1.2f, 0f, 0f);

    private const string ArrivalId = "farm_south";

    private OpeningFlow? _flow;

    private GameSession? _session;

    private DialogueBox? _box;

    private DayClock? _clock;

    private PlayerController? _player;

    private CanvasLayer? _hud;

    private Node3D? _maggie;

    private CrashScene? _crash;

    private readonly List<Vector3> _path = [];

    private float _walkSpeed;

    /// <summary>Whether a block of Maggie's lines is showing, so a Talk's Finished isn't taken for hers.</summary>
    private bool _awaitingLines;

    /// <summary>Whether Maggie is walking back to the south exit to leave.</summary>
    private bool _leaving;

    /// <summary>Raised when the step changes, so the Hud can refresh its hints.</summary>
    public event Action? Changed;

    /// <summary>Whether the Opening is still playing (BR-I1).</summary>
    public bool IsOpeningActive => _flow?.IsActive == true;

    /// <summary>The farm's sequence while it is playing the Opening, or null.</summary>
    public static OpeningSequence? ActiveIn(Node node) =>
        node.GetTree()?.GetFirstNodeInGroup(Group) is OpeningSequence sequence && sequence.IsOpeningActive
            ? sequence
            : null;

    /// <summary>Whether the Opening is playing in this scene (the south exit and the door wait for it).</summary>
    public static bool IsActiveIn(Node node) => ActiveIn(node) is not null;

    /// <summary>The HUD step hint, in <paramref name="device"/>'s glyphs; null when there is none.</summary>
    public string? StepHint(InputDeviceKind device) => _flow?.Hint(device);

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        AddToGroup(Group);
        SetProcess(false);
        _maggie = GetNodeOrNull<Node3D>("Maggie");

        var root = GetNode<GameRoot>("/root/GameRoot");
        _session = root.Session;
        if (_session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            DropMaggie();
            return;
        }

        _flow = new OpeningFlow(_session);
        if (!_flow.IsActive)
        {
            // BR-I1: a Game past the Opening never shows it, nor Maggie on the farm.
            _flow.Dispose();
            _flow = null;
            DropMaggie();
            return;
        }

        _walkSpeed = (float)root.Data.Tuning.WalkSpeedTilesPerSecond;
        _flow.StepChanged += OnStepChanged;

        // Deferred: the Player, the Hud, the clock and the dialogue box (under Common) aren't ready yet.
        CallDeferred(MethodName.Begin);
    }

    public override void _ExitTree()
    {
        if (_flow is not null)
        {
            _flow.StepChanged -= OnStepChanged;
            _flow.Dispose();
        }

        if (_box is not null && IsInstanceValid(_box))
        {
            _box.Finished -= OnLinesFinished;
        }

        if (_crash is not null && IsInstanceValid(_crash))
        {
            // Leaving mid-crash (quitting) must not leave the tree paused.
            GetTree().Paused = false;
        }
    }

    private void Begin()
    {
        var scene = GetParent();
        _player = GetTree().GetFirstNodeInGroup(PlayerController.Group) as PlayerController;
        _box = GetTree().GetFirstNodeInGroup(DialogueBox.Group) as DialogueBox;
        _clock = scene.GetNodeOrNull<DayClock>("Common/DayClock");
        _hud = scene.GetNodeOrNull<CanvasLayer>("Common/Hud");
        if (_box is not null)
        {
            _box.Finished += OnLinesFinished;
        }

        // The crash: the Player hidden and held (the tree paused), the clock off, the HUD away.
        ApplyClock();
        GetTree().Paused = true;
        if (_player is not null)
        {
            _player.Visible = false;
        }

        if (_hud is not null)
        {
            _hud.Visible = false;
        }

        if (_maggie is not null)
        {
            _maggie.Visible = false;
        }

        _crash = new CrashScene { Name = "CrashScene" };
        _crash.Finished += OnCrashFinished;
        AddChild(_crash);
        Changed?.Invoke();
    }

    public override void _Input(InputEvent @event)
    {
        if (_crash is null || !IsInstanceValid(_crash) || !_crash.IsPlaying)
        {
            return;
        }

        // During the crash open_menu skips to its end, and nothing opens a menu (which would resume the tree).
        if (@event.IsActionPressed("open_menu", allowEcho: false))
        {
            GetViewport().SetInputAsHandled();
            _crash.Skip();
            return;
        }

        if (@event.IsActionPressed("pause") || @event.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnCrashFinished()
    {
        // On black: hand the view back, put the Player at the south arrival point with Maggie beside him, fade in.
        _crash!.ReleaseCamera();
        var arrival = FindArrival();
        if (_player is not null)
        {
            if (arrival is not null)
            {
                _player.PlaceAt(arrival.GlobalPosition, -arrival.GlobalBasis.Z);
            }

            _player.Visible = true;
            var camera = _player.GetNodeOrNull<FollowCamera>("FollowCamera");
            camera?.MakeCurrent();
            camera?.SnapToTarget();
        }

        if (_maggie is not null)
        {
            var at = (arrival?.GlobalPosition ?? _maggie.GlobalPosition) + BesideArrival;
            _maggie.GlobalPosition = at;
            _maggie.Visible = true;
        }

        if (_hud is not null)
        {
            _hud.Visible = true;
        }

        _crash.FadeIn(() =>
        {
            _crash = null;

            // Resume the tree in the same frame the first lines pause it again, so the clock never ticks between.
            GetTree().Paused = false;
            _flow!.CrashFinished();
        });
    }

    private void OnStepChanged()
    {
        ApplyClock();
        Changed?.Invoke();
        var flow = _flow!;
        switch (flow.Current)
        {
            case OpeningStep.Walk:
                StartWalk(Waypoints(), leaving: false);
                break;
            case OpeningStep.Done:
                // Maggie walks back the way she came and leaves by the south exit.
                var back = Waypoints();
                back.Reverse();
                if (FindArrival() is { } arrival)
                {
                    back.Add(arrival.GlobalPosition + BesideArrival);
                }

                if (GetNodeOrNull<Node3D>("ExitPoint") is { } exit)
                {
                    back.Add(exit.GlobalPosition);
                }

                StartWalk(back, leaving: true);
                break;
            default:
                if (flow.Lines.Count > 0)
                {
                    ShowLines();
                }

                break;
        }
    }

    private void ShowLines()
    {
        if (_box is null)
        {
            // No dialogue box in this scene: nothing can show the lines, so they are over at once.
            _flow!.LinesFinished();
            return;
        }

        _awaitingLines = true;
        _box.Show(_flow!.Speaker, _flow.Lines);
    }

    private void OnLinesFinished()
    {
        if (!_awaitingLines)
        {
            // A Talk with Maggie between steps, not one of the Opening's blocks.
            return;
        }

        _awaitingLines = false;
        _flow!.LinesFinished();
    }

    // BR-G2: the clock runs only on the steps that let it (the DialogueBox also pauses the tree while she talks).
    private void ApplyClock()
    {
        if (_clock is not null && _flow is not null)
        {
            _clock.ProcessMode = _flow.ClockRuns ? ProcessModeEnum.Pausable : ProcessModeEnum.Disabled;
        }
    }

    private void StartWalk(List<Vector3> path, bool leaving)
    {
        _path.Clear();
        _path.AddRange(path);
        _leaving = leaving;
        SetProcess(_maggie is not null);
        if (_maggie is null)
        {
            Arrive();
        }
    }

    public override void _Process(double delta)
    {
        // Her walk stops while a menu or the dialogue box has paused the tree.
        if (GetTree().Paused || _maggie is null || !IsInstanceValid(_maggie))
        {
            return;
        }

        if (_path.Count > 0)
        {
            var target = _path[0] with { Y = _maggie.GlobalPosition.Y };
            var offset = target - _maggie.GlobalPosition;
            var step = _walkSpeed * (float)delta;
            if (offset.Length() <= step)
            {
                _maggie.GlobalPosition = target;
                _path.RemoveAt(0);
            }
            else
            {
                _maggie.GlobalPosition += offset.Normalized() * step;
                _maggie.Rotation = _maggie.Rotation with { Y = Mathf.Atan2(offset.X, offset.Z) };
            }

            return;
        }

        // At the farmhouse she waits for the Player; leaving, she is gone.
        if (_leaving || PlayerIsNear())
        {
            Arrive();
        }
    }

    private void Arrive()
    {
        SetProcess(false);
        if (_leaving)
        {
            RemoveMaggie();
            return;
        }

        if (_player is not null && _maggie is not null)
        {
            // Face the Player as she speaks.
            var offset = _player.GlobalPosition - _maggie.GlobalPosition;
            _maggie.Rotation = _maggie.Rotation with { Y = Mathf.Atan2(offset.X, offset.Z) };
        }

        _flow!.WalkArrived();
    }

    private bool PlayerIsNear()
    {
        if (_player is null || _maggie is null)
        {
            return true;
        }

        var offset = _player.GlobalPosition - _maggie.GlobalPosition;
        return new Vector2(offset.X, offset.Z).Length() <= NearTiles;
    }

    private List<Vector3> Waypoints() =>
        GetNodeOrNull<Node3D>("Waypoints") is { } waypoints
            ? waypoints.GetChildren().OfType<Marker3D>().Select(marker => marker.GlobalPosition).ToList()
            : [];

    private AreaArrival? FindArrival() => FindArrival(GetParent());

    private static AreaArrival? FindArrival(Node node)
    {
        if (node is AreaArrival arrival && arrival.ArrivalId == ArrivalId)
        {
            return arrival;
        }

        foreach (var child in node.GetChildren())
        {
            if (FindArrival(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Takes Maggie out of the scene at once, before the Hud looks for Resident points (no Opening).</summary>
    private void DropMaggie()
    {
        if (_maggie is null)
        {
            return;
        }

        RemoveChild(_maggie);
        _maggie.QueueFree();
        _maggie = null;
    }

    /// <summary>Maggie leaves the farm at the south exit (the Opening is over).</summary>
    private void RemoveMaggie()
    {
        if (_maggie is null || !IsInstanceValid(_maggie))
        {
            return;
        }

        // Far out of the Player's reach first (the points measure horizontally), so her point reports him gone and
        // the Hud's Talk hint hides; then freed.
        var maggie = _maggie;
        _maggie = null;
        maggie.Visible = false;
        maggie.GlobalPosition += new Vector3(10000f, 0f, 0f);
        GetTree().CreateTimer(0.25, processAlways: true).Timeout += () =>
        {
            if (IsInstanceValid(maggie))
            {
                maggie.QueueFree();
            }
        };
    }
}
