#nullable enable

using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Games;
using LongWayHome.Game.Core;
using LongWayHome.Game.Opening;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// An exit on an area's edge (#136): an <see cref="Area3D"/> trigger, 3 tiles wide and 1 tile deep, leading to
/// <see cref="TargetArea"/> (an <see cref="AreaId"/> code, e.g. <c>forest_path</c>). When the Player's body enters it,
/// it asks <see cref="GameSession.EnterArea"/>; on <c>Ok</c> it records <see cref="ArrivalId"/> as the
/// <see cref="GameRoot.PendingArrivalId"/> and changes to the target's scene from <see cref="AreaScenes"/>, deferred
/// because the physics callback must not take the scene out of the tree. On a rejection it does nothing. While a menu
/// is open the tree is paused, so the trigger can't fire. Holds no rules: whether the area is open is the session's
/// (BR-A1). While the farm's <see cref="OpeningSequence"/> is playing the Opening (#139, BR-I1) it does nothing, so the
/// Player can't leave the farm before Maggie has finished.
/// </summary>
public partial class AreaExit : Area3D
{
    private GameRoot? _root;

    /// <summary>Set once the scene change is under way, so a second overlap in the same frame can't start another.</summary>
    private bool _leaving;

    /// <summary>The area this exit leads to, as its canonical code (canonical-values.md).</summary>
    [Export]
    public string TargetArea { get; set; } = "";

    /// <summary>The <see cref="AreaArrival"/> in the target area the Player walks in at.</summary>
    [Export]
    public string ArrivalId { get; set; } = "";

    public override void _Ready()
    {
        _root = GetNode<GameRoot>("/root/GameRoot");
        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree() => BodyEntered -= OnBodyEntered;

    private void OnBodyEntered(Node3D body)
    {
        // A pending night message means the Day has just ended here and NightReturn is already taking him home.
        if (_leaving || !body.IsInGroup(PlayerController.Group) || _root?.Session is not { } session
            || _root.PendingNight is not null || OpeningSequence.IsActiveIn(this))
        {
            return;
        }

        if (!AreaCodes.TryParse(TargetArea, out var target) || AreaScenes.PathOf(target) is not { } path)
        {
            GD.PushError($"{Name}: no area scene for TargetArea \"{TargetArea}\"");
            return;
        }

        var from = session.CurrentArea;
        if (!session.EnterArea(target).IsOk)
        {
            return;
        }

        _leaving = true;
        _root.PendingArrivalId = ArrivalId;
        CallDeferred(MethodName.Leave, path, (int)from);
    }

    private void Leave(string path, int from)
    {
        var error = GetTree().ChangeSceneToFile(path);
        if (error != Error.Ok)
        {
            // The area could not be opened: stay here, back in this area, so the Player can still play.
            GD.PushError($"Could not open {path}: {error}");
            _root!.Session.EnterArea((AreaId)from);
            _root.PendingArrivalId = null;
            _leaving = false;
        }
    }
}
