#nullable enable

using Godot;
using LongWayHome.Game.Core;
using LongWayHome.Game.Player;

namespace LongWayHome.Game.World;

/// <summary>
/// Where the Player walks in from a neighbouring area (#136): a <see cref="Marker3D"/> one and a half tiles inside the
/// edge its exit is on, outside that exit's trigger, whose forward (−Z) faces into the area. When the scene starts with
/// <see cref="GameRoot.PendingArrivalId"/> equal to <see cref="ArrivalId"/>, it clears the pending id and, once every
/// node is ready, moves the Player to the marker facing its way (<see cref="PlayerController.PlaceAt"/>). With no
/// matching id the Player stays where the scene placed him. Holds no rules.
/// </summary>
public partial class AreaArrival : Marker3D
{
    /// <summary>The id the neighbouring area's <see cref="AreaExit.ArrivalId"/> names.</summary>
    [Export]
    public string ArrivalId { get; set; } = "";

    public override void _Ready()
    {
        var root = GetNode<GameRoot>("/root/GameRoot");
        if (ArrivalId.Length == 0 || root.PendingArrivalId != ArrivalId)
        {
            return;
        }

        root.PendingArrivalId = null;

        // Deferred: the Player may not be ready (or in its group) yet, and must keep the scene's spot as his start.
        CallDeferred(MethodName.PlacePlayer);
    }

    private void PlacePlayer()
    {
        if (GetTree().GetFirstNodeInGroup(PlayerController.Group) is PlayerController player)
        {
            player.PlaceAt(GlobalPosition, -GlobalBasis.Z);
        }
    }
}
