#nullable enable

using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The shared palette material (#76), <c>res://art/materials/palette.tres</c>, and the way code-built shapes take a
/// colour from it: <see cref="Apply"/> puts the material on a node and sets the node's <c>swatch</c> instance
/// parameter to the UV centre of an <see cref="ArtPalette"/> role. Every node shares the one material, so there is
/// no per-model material state; imported models instead keep UVs mapped onto <c>palette.png</c> and leave
/// <c>swatch</c> unset.
/// </summary>
public static class ArtMaterials
{
    private const string MaterialPath = "res://art/materials/palette.tres";

    private static ShaderMaterial? _shared;

    /// <summary>The shared material, loaded on first use.</summary>
    public static ShaderMaterial Shared => _shared ??= GD.Load<ShaderMaterial>(MaterialPath);

    /// <summary>Gives <paramref name="node"/> the shared material and the colour of <paramref name="role"/>.</summary>
    /// <exception cref="System.ArgumentException">No palette swatch has that role.</exception>
    public static void Apply(GeometryInstance3D node, string role)
    {
        var (u, v) = ArtPalette.UvCentre(role);
        node.MaterialOverride = Shared;
        node.SetInstanceShaderParameter("swatch", new Vector2(u, v));
    }
}
