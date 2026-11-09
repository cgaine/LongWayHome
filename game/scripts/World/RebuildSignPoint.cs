#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Game.World;

/// <summary>
/// A Rebuild sign's Building (#163): a child of the sign's <see cref="InteractionPoint"/> (<c>PointId = "sign"</c>),
/// naming the Building it rebuilds. The <c>RebuildMenu</c> opens for it and the HUD shows its "Rebuild" hint while
/// <see cref="GameSession.RebuildSign"/> has a sign for it (BR-B4). Holds no rules and handles no input itself.
/// </summary>
public partial class RebuildSignPoint : Node
{
    /// <summary>The <see cref="BuildingId"/> code this sign rebuilds, e.g. <c>general_store</c> (<see cref="BuildingCodes"/>).
    /// An unknown code is reported once and the sign never opens.</summary>
    [Export] public string Building { get; set; } = "general_store";

    /// <summary>The Building this sign rebuilds, or null when <see cref="Building"/> is not a known code.</summary>
    public BuildingId? BuildingId { get; private set; }

    public override void _Ready()
    {
        if (BuildingCodes.TryParse(Building, out var id))
        {
            BuildingId = id;
        }
        else
        {
            GD.PushError($"{GetPath()}: Building \"{Building}\" is not a Building code.");
        }
    }

    /// <summary>The Rebuild sign child of <paramref name="point"/>, or null when it has none.</summary>
    public static RebuildSignPoint? Of(InteractionPoint point) =>
        point.GetChildren().OfType<RebuildSignPoint>().FirstOrDefault();

    /// <summary>Whether <paramref name="session"/> has a sign up for this Building: its next stage exists (BR-B4).</summary>
    public bool IsUp(GameSession? session) => session is not null && BuildingId is { } id && session.RebuildSign(id) is not null;
}
