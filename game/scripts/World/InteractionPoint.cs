#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// A place the Player will interact with (the door, the shipping bin, the shop, the pond, the river, the lake). Tracks whether the
/// Player is within <see cref="Range"/> tiles, measured horizontally, and signals when that changes. It performs
/// no action itself; later features listen to it. Holds no rules.
/// </summary>
public partial class InteractionPoint : Node3D
{
    [Signal] public delegate void PlayerEnteredEventHandler();

    [Signal] public delegate void PlayerExitedEventHandler();

    /// <summary>Which point this is: <c>door</c>, <c>bin</c>, <c>shop</c>, <c>pond</c>, <c>river</c> or <c>lake</c>.</summary>
    [Export] public string PointId { get; set; } = "";

    /// <summary>How close, in tiles, the Player must be. Height is ignored.</summary>
    [Export] public float Range { get; set; } = 1f;

    public bool IsPlayerInRange { get; private set; }

    private Node3D? _player;

    /// <summary>The first point under <paramref name="node"/> (depth first) with the given id, or null.</summary>
    public static InteractionPoint? Find(Node node, string pointId)
    {
        if (node is InteractionPoint point && point.PointId == pointId)
        {
            return point;
        }

        foreach (var child in node.GetChildren())
        {
            if (Find(child, pointId) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>Every point under <paramref name="node"/> (depth first, <paramref name="node"/> included) whose id is
    /// one of <paramref name="pointIds"/>.</summary>
    public static List<InteractionPoint> FindAll(Node node, params string[] pointIds)
    {
        var found = new List<InteractionPoint>();
        Collect(node, pointIds, found);
        return found;
    }

    private static void Collect(Node node, string[] pointIds, List<InteractionPoint> found)
    {
        if (node is InteractionPoint point && Array.IndexOf(pointIds, point.PointId) >= 0)
        {
            found.Add(point);
        }

        foreach (var child in node.GetChildren())
        {
            Collect(child, pointIds, found);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || !IsInstanceValid(_player) || !_player.IsInsideTree())
        {
            _player = GetTree().GetFirstNodeInGroup(PlayerController.Group) as Node3D;
        }

        var inRange = false;
        if (_player is not null)
        {
            var offset = _player.GlobalPosition - GlobalPosition;
            inRange = new Vector2(offset.X, offset.Z).Length() <= Range;
        }

        if (inRange == IsPlayerInRange)
        {
            return;
        }

        IsPlayerInRange = inRange;
        EmitSignal(inRange ? SignalName.PlayerEntered : SignalName.PlayerExited);
    }
}
