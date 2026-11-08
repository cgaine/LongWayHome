#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Residents;

namespace LongWayHome.Game.Art;

/// <summary>
/// The townspeople's meshes (ART_DIRECTION §6.2, #138): each Resident's <see cref="ResidentArt"/> part table merged by
/// <see cref="PartMeshes"/> into one static mesh (no skeleton, no animation), built once and cached, and drawn with
/// <see cref="Material"/>, a copy of the shared palette material whose next pass is the inverted-hull outline, as the
/// farmer is. A Resident is two draws: the body and its outline (REQ-14). Used by the areas' <c>ResidentView</c>.
/// </summary>
public static class ResidentModels
{
    private const string OutlineShaderPath = "res://art/shaders/outline.gdshader";

    private static readonly Dictionary<ResidentId, ArrayMesh> Meshes = new();

    private static ShaderMaterial? _material;

    /// <summary>The merged mesh of <paramref name="id"/>, standing on y 0 and facing +Z.</summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The Resident has no model yet.</exception>
    public static ArrayMesh MeshFor(ResidentId id)
    {
        if (!Meshes.TryGetValue(id, out var mesh))
        {
            mesh = PartMeshes.Merge(ResidentArt.For(id));
            Meshes[id] = mesh;
        }

        return mesh;
    }

    /// <summary>A copy of <see cref="ArtMaterials.Shared"/> with the outline as its next pass, shared by every Resident.</summary>
    public static ShaderMaterial Material
    {
        get
        {
            if (_material is null)
            {
                _material = (ShaderMaterial)ArtMaterials.Shared.Duplicate();
                _material.NextPass = new ShaderMaterial { Shader = GD.Load<Shader>(OutlineShaderPath) };
            }

            return _material;
        }
    }
}
