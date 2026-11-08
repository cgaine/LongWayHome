#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Game.Art;

/// <summary>
/// The meshes of the trees and rocks the axe and pickaxe gather from (ART_DIRECTION §5.1 and §7.6): the Twinbough from
/// <see cref="TreeArt"/> at each stage and season, and each <see cref="RockKind"/> from <see cref="RockArt"/>, each one
/// merged mesh under the shared material by <see cref="PartMeshes"/>. Each mesh is built once and shared. Shown in the
/// art sandbox, and as scenery by the forest path, town and lakeshore views (#136).
/// </summary>
public static class NatureModels
{
    private static readonly Dictionary<(TreeStage, Season), ArrayMesh> Trees = new();
    private static readonly Dictionary<RockKind, ArrayMesh> Rocks = new();

    /// <summary>The Twinbough at <paramref name="stage"/> in <paramref name="season"/>.</summary>
    public static ArrayMesh Twinbough(TreeStage stage, Season season)
    {
        if (!Trees.TryGetValue((stage, season), out var mesh))
        {
            mesh = PartMeshes.Merge(TreeArt.Twinbough(stage, season));
            Trees[(stage, season)] = mesh;
        }

        return mesh;
    }

    /// <summary>The rock of <paramref name="kind"/>.</summary>
    public static ArrayMesh Rock(RockKind kind)
    {
        if (!Rocks.TryGetValue(kind, out var mesh))
        {
            mesh = PartMeshes.Merge(RockArt.For(kind));
            Rocks[kind] = mesh;
        }

        return mesh;
    }
}
