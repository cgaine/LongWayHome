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
    /// <summary>
    /// Merges the parts into one mesh, so a multi-part shape is one draw. Normals go through the inverse transpose of
    /// each part's basis and are re-normalised, so squashed or rotated shapes keep true normals for stepped shading.
    /// A part with a role has its UVs set to that swatch's centre, for meshes that leave the <c>swatch</c> instance
    /// parameter unset; give every part a role or none.
    /// When at least one part glows, every vertex gets UV2 = (Glow, Twinkle ? 1 : 0), which the shared shader turns
    /// into emission at night (#94); otherwise the mesh has no UV2 array and renders exactly as before.
    /// </summary>
    public static ArrayMesh Merge(IEnumerable<MeshPart> parts)
    {
        var list = parts.ToList();
        var anyGlow = list.Any(part => part.Glow > 0f);
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        var offset = 0;
        foreach (var (mesh, at, role, glow, twinkle) in list)
        {
            var arrays = mesh.SurfaceGetArrays(0);
            var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            var normalBasis = at.Basis.Inverse().Transposed();
            var (u, v) = role is null ? (-1f, -1f) : ArtPalette.UvCentre(role);
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
