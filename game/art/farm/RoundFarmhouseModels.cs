#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The round farmhouse's meshes (ART_DIRECTION §7.1, the concept art's round farmhouse), built from
/// <see cref="RoundFarmhouseArt"/> by <see cref="PartMeshes"/> as one mesh per state under the shared material, so the
/// drum and the roof cone (cylinders 1.5 m or more across) read round. Each mesh is built once and shared. Shown only in
/// the art sandbox for now; the farm still uses <see cref="FarmhouseView"/>.
/// </summary>
public static class RoundFarmhouseModels
{
    private static readonly Dictionary<string, ArrayMesh> Meshes = new();

    /// <summary>The round farmhouse, run-down (<paramref name="restored"/> false) or restored.</summary>
    public static ArrayMesh MeshFor(bool restored) =>
        Cached(restored ? "restored" : "rundown", restored ? RoundFarmhouseArt.Restored : RoundFarmhouseArt.RunDown);

    /// <summary>The restored house's lit window panes, to show only at night.</summary>
    public static ArrayMesh NightLights() => Cached("night", RoundFarmhouseArt.RestoredNightLights);

    private static ArrayMesh Cached(string key, IEnumerable<CropPart> parts)
    {
        if (!Meshes.TryGetValue(key, out var mesh))
        {
            mesh = PartMeshes.Merge(parts);
            Meshes[key] = mesh;
        }

        return mesh;
    }
}
