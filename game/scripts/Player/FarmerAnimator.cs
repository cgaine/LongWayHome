#nullable enable

using System;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Art;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Player;

/// <summary>
/// Plays the farmer's clips (#103) on his <see cref="AnimationPlayer"/> (<c>../Model/Farmer/Animations</c>): a child of
/// the Player. Each physics frame with no one-shot playing it plays the locomotion loop that
/// <see cref="FarmerAnimationState.Locomotion"/> picks from the Player's horizontal velocity and the <c>run</c> action,
/// with a 0.15 s blend (the walk at <see cref="FarmerAnimationState.WalkPlaybackScale"/>). The hooks
/// (<c>ToolUse</c>, <c>PondRefill</c>, <c>BinShip</c>, <c>DoorSleep</c>) call <see cref="PlayOneShot"/> only after
/// the session's action succeeded, so a clip never delays or decides anything. Moving cuts a tool, refill or ship clip
/// short; <see cref="FarmerClip.Sleep"/> and <see cref="FarmerClip.PassOut"/> can't be cut short, and while either
/// plays <see cref="LocksMovement"/> stops the Player and his Use.
/// <para>
/// In the last seconds before 02:00 it plays <see cref="FarmerClip.PassOut"/>, timed to end as the clock reaches
/// 02:00 (<see cref="FarmerAnimationState.PassOutStarts"/>), and holds its last pose; the clock keeps running and the
/// session still ends the Day at 02:00 by passing out (BR-G3). On <see cref="GameSession.NightCompleted"/> it stops
/// any one-shot without calling its done callback (so a sleep's callback can never reach the next Day) and plays
/// <see cref="FarmerClip.Idle"/>. Pausable like <see cref="DayClock"/>: a paused tree pauses the clip and the clock
/// together (BR-G2). It never moves the clock and holds no rules.
/// </para>
/// </summary>
public partial class FarmerAnimator : Node
{
    /// <summary>The blend into each locomotion loop, in seconds.</summary>
    private const float LocomotionBlend = 0.15f;

    /// <summary>The blend into a one-shot, in seconds.</summary>
    private const float OneShotBlend = 0.08f;

    private AnimationPlayer? _player;

    private CharacterBody3D? _body;

    private GameSession? _session;

    private GameTuning? _tuning;

    private float _walkSpeed;

    private FarmerClip? _oneShot;

    private Action? _done;

    private int _lastClockMinutes = -1;

    /// <summary>Real seconds of play since the clock last showed a new minute.</summary>
    private float _secondsIntoMinute;

    /// <summary>True while a one-shot plays (or the pass out holds its last pose).</summary>
    public bool Busy => _oneShot is not null;

    /// <summary>The one-shot playing, if any.</summary>
    public FarmerClip? OneShot => _oneShot;

    /// <summary>True while <see cref="FarmerClip.Sleep"/> or <see cref="FarmerClip.PassOut"/> plays (or holds): the
    /// Player doesn't move and Use is ignored.</summary>
    public bool LocksMovement => _oneShot is FarmerClip.Sleep or FarmerClip.PassOut;

    public override void _Ready()
    {
        // BR-G2: the clips pause with the tree, like the clock.
        ProcessMode = ProcessModeEnum.Pausable;

        _body = GetParent() as CharacterBody3D;
        _player = GetNodeOrNull<AnimationPlayer>("../Model/Farmer/Animations");
        if (_player is null)
        {
            GD.PushError("FarmerAnimator: no AnimationPlayer at ../Model/Farmer/Animations.");
            SetPhysicsProcess(false);
            SetProcess(false);
            return;
        }

        _player.AnimationFinished += OnAnimationFinished;
        Play(FarmerClip.Idle, 0f);

        var root = GetNode<GameRoot>("/root/GameRoot");
        if (root.Data is null || root.Session is null)
        {
            // GameRoot has already reported the data errors and is quitting.
            SetPhysicsProcess(false);
            SetProcess(false);
            return;
        }

        _tuning = root.Data.Tuning;
        _walkSpeed = (float)_tuning.WalkSpeedTilesPerSecond;
        _session = root.Session;
        _session.NightCompleted += OnNightCompleted;
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= OnNightCompleted;
        }

        if (_player is not null)
        {
            _player.AnimationFinished -= OnAnimationFinished;
        }
    }

    /// <summary>
    /// Plays <paramref name="clip"/> once with a 0.08 s blend in, then calls <paramref name="done"/> when it finishes
    /// and returns to locomotion. Playing the same clip again restarts it; another clip replaces the one playing (and
    /// drops its callback). Ignored while <see cref="LocksMovement"/>: nothing replaces the sleep or the pass out.
    /// </summary>
    public void PlayOneShot(FarmerClip clip, Action? done = null)
    {
        if (_player is null || LocksMovement)
        {
            return;
        }

        _oneShot = clip;
        _done = done;
        Play(clip, OneShotBlend, restart: true);
    }

    public override void _Process(double delta)
    {
        // Tracks the part-minute the clock doesn't show, so the pass out can be timed to end at 02:00.
        var clock = _session!.Game.ClockMinutes;
        if (clock != _lastClockMinutes)
        {
            _lastClockMinutes = clock;
            _secondsIntoMinute = 0f;
        }
        else
        {
            _secondsIntoMinute += (float)delta;
        }

        if (_oneShot is FarmerClip.PassOut or FarmerClip.Sleep)
        {
            return;
        }

        // BR-G3: the clip only plays out the last seconds; the session ends the Day at 02:00 on its own.
        if (FarmerAnimationState.PassOutDue(clock, _tuning!) ||
            FarmerAnimationState.PassOutStarts(clock, _secondsIntoMinute, _tuning!))
        {
            _oneShot = FarmerClip.PassOut;
            _done = null;
                Play(FarmerClip.PassOut, OneShotBlend, restart: true);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_oneShot is not null)
        {
            if (LocksMovement || !MoveInput())
            {
                return;
            }

            // Moving cuts a tool, refill or ship clip short; its action has already happened.
            _oneShot = null;
            _done = null;
        }

        var velocity = _body?.Velocity ?? Vector3.Zero;
        var speed = new Vector2(velocity.X, velocity.Z).Length();
        var locomotion = FarmerAnimationState.Locomotion(speed, _walkSpeed, Input.IsActionPressed("run"));
        var scale = locomotion == FarmerClip.Walk ? FarmerAnimationState.WalkPlaybackScale(speed, _walkSpeed) : 1f;
        Play(locomotion, LocomotionBlend, speedScale: scale);
    }

    private static bool MoveInput() => Input.GetVector("move_left", "move_right", "move_up", "move_down") != Vector2.Zero;

    private void Play(FarmerClip clip, float blend, bool restart = false, float speedScale = 1f)
    {
        var name = FarmerAnimations.ClipName(clip);
        _player!.SpeedScale = speedScale;
        if (_player.CurrentAnimation == name && _player.IsPlaying())
        {
            if (restart)
            {
                _player.Seek(0, update: true);
            }

            return;
        }

        _player.Play(name, blend);
    }

    private void OnAnimationFinished(StringName name)
    {
        if (_oneShot is not { } clip || name != FarmerAnimations.ClipName(clip))
        {
            return;
        }

        if (clip == FarmerClip.PassOut)
        {
            // He lies where he fell until the Night: the clip holds its last pose and keeps the lock.
            return;
        }

        var done = _done;
        _oneShot = null;
        _done = null;
        done?.Invoke();
    }

    private void OnNightCompleted()
    {
        // Stop any one-shot without its callback, so nothing from the old Day runs in the new one.
        _oneShot = null;
        _done = null;
        _secondsIntoMinute = 0f;
        _lastClockMinutes = _session?.Game.ClockMinutes ?? -1;
        if (_player is not null)
        {
            _player.SpeedScale = 1f;
            _player.Play(FarmerAnimations.ClipName(FarmerClip.Idle));
            _player.Seek(0, update: true);
        }
    }
}
