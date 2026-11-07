#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// One primitive placed in a merged mesh, with the palette role its UVs point at, if any, and its night glow (0 none to
/// 1 full) and whether that glow twinkles (#94).
/// </summary>
public readonly record struct MeshPart(
    Mesh Mesh, Transform3D At, string? Role = null, float Glow = 0f, bool Twinkle = false);

/// <summary>Building code-made low-poly meshes for the shared palette material (#77, #78).</summary>
public static class ArtMeshes
{
    /// <summary>A skinned vertex's weights: all on its first bone.</summary>
    private static readonly float[] FullWeight = [1f, 0f, 0f, 0f];

    /// <summary>
    /// Merges the parts into one mesh, so a multi-part shape is one draw. Normals go through the inverse transpose of
    /// each part's basis and are re-normalised, so squashed or rotated shapes keep true normals for stepped shading.
    /// A part with a role has its UVs set to that swatch's centre, for meshes that leave the <c>swatch</c> instance
    /// parameter unset; give every part a role or none.
    /// When at least one part glows, every vertex gets UV2 = (Glow, Twinkle ? 1 : 0), which the shared shader turns
    /// into emission at night (#94); otherwise the mesh has no UV2 array and renders exactly as before.
    /// </summary>
    public static ArrayMesh Merge(IEnumerable<MeshPart> parts) =>
        Merge(parts, bones: null);

    /// <summary>
    /// Merges the parts into one skinned mesh (#102): the same UVs, UV2 and normals as <see cref="Merge(IEnumerable{MeshPart})"/>, and every
    /// vertex of a part weighted fully to that part's bone (4 influences, the first the bone at weight 1, the rest
    /// bone 0 at weight 0), for a <see cref="MeshInstance3D"/> under a <see cref="Skeleton3D"/> with a
    /// <see cref="Skin"/> binding each bone. Each part's transform is in the skeleton's rest (model) space.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">A part's bone is negative.</exception>
    public static ArrayMesh MergeSkinned(IEnumerable<(MeshPart Part, int Bone)> parts)
    {
        var list = parts.ToList();
        foreach (var (_, bone) in list)
        {
            if (bone < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(parts), bone, "A skinned part needs a bone index.");
            }
        }

        return Merge(list.Select(entry => entry.Part), list.Select(entry => entry.Bone).ToList());
    }

    /// <summary>The merge behind both: with <paramref name="bones"/> (one per part) it writes bones and weights.</summary>
    private static ArrayMesh Merge(IEnumerable<MeshPart> parts, IReadOnlyList<int>? bones)
    {
        var list = parts.ToList();
        var anyGlow = list.Any(part => part.Glow > 0f);
        var surface = new SurfaceTool();
        if (bones is not null)
        {
            // Before Begin: SurfaceTool takes the weight count only while no surface is begun.
            surface.SetSkinWeightCount(SurfaceTool.SkinWeightCount.Skin4Weights);
        }

        surface.Begin(Mesh.PrimitiveType.Triangles);

        var offset = 0;
        for (var p = 0; p < list.Count; p++)
        {
            var (mesh, at, role, glow, twinkle) = list[p];
            var arrays = mesh.SurfaceGetArrays(0);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            var normalBasis = at.Basis.Inverse().Transposed();
            var (u, v) = role is null ? (-1f, -1f) : ArtPalette.UvCentre(role);
            int[] influences = bones is null ? [] : [bones[p], 0, 0, 0];
            for (var i = 0; i < vertices.Length; i++)
            {
                if (role is not null)
                {
                    surface.SetUV(new Vector2(u, v));
                }

                if (anyGlow)
                {
                    surface.SetUV2(new Vector2(glow, twinkle ? 1f : 0f));
                }

                if (bones is not null)
                {
                    surface.SetBones(influences);
                    surface.SetWeights(FullWeight);
                }

                surface.SetNormal((normalBasis * normals[i]).Normalized());
                surface.AddVertex(at * vertices[i]);
            }

            foreach (var index in indices)
            {
                surface.AddIndex(offset + index);
            }

            offset += vertices.Length;
        }

        return surface.Commit();
    }
}
