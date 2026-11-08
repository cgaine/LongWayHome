#nullable enable

using System.Collections.Generic;
using LongWayHome.Domain.Games;

namespace LongWayHome.Game.World;

/// <summary>
/// The scene each open area lives in (#136, technical-blueprint.md, *Areas and scenes*): one scene per area, only the
/// current one loaded. Areas that aren't built yet (across the river, the mine) have no scene. Holds no rules: which
/// areas are open is <see cref="Domain.Games.Game.IsAreaOpen"/> (BR-A1).
/// </summary>
public static class AreaScenes
{
    /// <summary>The farm's scene, where every Day starts and every Game is started or loaded.</summary>
    public const string FarmScene = "res://scenes/Farm.tscn";

    private static readonly Dictionary<AreaId, string> Paths = new()
    {
        [AreaId.Farm] = "res://scenes/Farm.tscn",
        [AreaId.ForestPath] = "res://scenes/ForestPath.tscn",
        [AreaId.Town] = "res://scenes/Town.tscn",
        [AreaId.Lakeshore] = "res://scenes/Lakeshore.tscn",
    };

    /// <summary>The scene path of <paramref name="area"/>, or null when it has no scene yet.</summary>
    public static string? PathOf(AreaId area) => Paths.TryGetValue(area, out var path) ? path : null;
}
