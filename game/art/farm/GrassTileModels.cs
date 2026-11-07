#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The grass Tile detail meshes (ART_DIRECTION §7.2): each <see cref="GrassTileArt"/> layout in a Season's
/// <see cref="SeasonGrass"/>, merged by <see cref="PartMeshes"/> into one mesh coloured by its UVs under the shared
/// palette material. Built once per (layout, Season) and shared by every Tile showing it, so the plot's 108 Tiles share
/// at most <see cref="GrassTileArt.Variants"/> meshes per Season; the meadow's wild cover has its own set
/// (<see cref="MeadowMeshFor"/>).
/// </summary>
public static class GrassTileModels
{
    private static readonly Dictionary<(int, SeasonGrass), ArrayMesh> Meshes = new();

    private static readonly Dictionary<(int, SeasonGrass), ArrayMesh> MeadowMeshes = new();

    /// <summary>The wild meadow mesh of <paramref name="variant"/> in <paramref name="grass"/> (<see cref="GrassTileArt.MeadowParts"/>).</summary>
    public static ArrayMesh MeadowMeshFor(int variant, SeasonGrass grass)
    {
        if (!MeadowMeshes.TryGetValue((variant, grass), out var mesh))
        {
            mesh = PartMeshes.Merge(GrassTileArt.MeadowParts(variant, grass));
            MeadowMeshes[(variant, grass)] = mesh;
        }

        return mesh;
    }

    /// <summary>The mesh of <paramref name="variant"/> in <paramref name="grass"/>.</summary>
    public static ArrayMesh MeshFor(int variant, SeasonGrass grass)
    {
        if (!Meshes.TryGetValue((variant, grass), out var mesh))
        {
            mesh = PartMeshes.Merge(GrassTileArt.Parts(variant, grass));
            Meshes[(variant, grass)] = mesh;
        }

        return mesh;
    }
}
