#nullable enable

using System.Collections.Generic;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The pieces the area views (#136) build their scenery from, under the shared palette material: flat ground and water
/// rectangles in one palette role, one <see cref="MultiMeshInstance3D"/> per repeated mesh (as the meadow does, to keep
/// draw calls low), and a trunk collision per tree. None of it holds a rule.
/// </summary>
public static class AreaArt
{
    /// <summary>
    /// A flat rectangle from (<paramref name="minX"/>, <paramref name="minZ"/>) to (<paramref name="maxX"/>,
    /// <paramref name="maxZ"/>) at height <paramref name="y"/>, in <paramref name="role"/>'s colour, casting no shadow.
    /// </summary>
    public static MeshInstance3D Flat(string name, float minX, float maxX, float minZ, float maxZ, float y, string role)
    {
        var node = new MeshInstance3D
        {
            Name = name,
            Mesh = new PlaneMesh { Size = new Vector2(maxX - minX, maxZ - minZ) },
            Position = new Vector3((minX + maxX) / 2f, y, (minZ + maxZ) / 2f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        ArtMaterials.Apply(node, role);
        return node;
    }

    /// <summary>A ribbon <paramref name="halfWidth"/> either side of a centre line running along z, at height <paramref name="y"/>.</summary>
    public static MeshInstance3D Ribbon(
        string name, IReadOnlyList<(float X, float Z)> centre, float halfWidth, float y, string role)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        surface.SetNormal(Vector3.Up);
        for (var i = 0; i + 1 < centre.Count; i++)
        {
            var (x0, z0) = centre[i];
            var (x1, z1) = centre[i + 1];
            var a = new Vector3(x0 - halfWidth, y, z0);
            var b = new Vector3(x0 + halfWidth, y, z0);
            var c = new Vector3(x1 - halfWidth, y, z1);
            var d = new Vector3(x1 + halfWidth, y, z1);

            // Clockwise seen from above, Godot's front-face winding, so the strip faces up.
            surface.AddVertex(a);
            surface.AddVertex(b);
            surface.AddVertex(c);
            surface.AddVertex(b);
            surface.AddVertex(d);
            surface.AddVertex(c);
        }

        var node = new MeshInstance3D
        {
            Name = name,
            Mesh = surface.Commit(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        ArtMaterials.Apply(node, role);
        return node;
    }

    /// <summary>One instance of <paramref name="mesh"/> per prop, standing on the ground, turned and scaled by it.</summary>
    public static MultiMeshInstance3D Many(string name, Mesh mesh, IReadOnlyList<AreaProp> props)
    {
        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = props.Count,
        };
        for (var i = 0; i < props.Count; i++)
        {
            multimesh.SetInstanceTransform(i, PropTransform(props[i]));
        }

        return new MultiMeshInstance3D
        {
            Name = name,
            Multimesh = multimesh,
            MaterialOverride = ArtMaterials.Shared,
        };
    }

    /// <summary>
    /// One body with a trunk-sized upright cylinder per tree, so the Player walks under the canopies but not through
    /// the trunks.
    /// </summary>
    public static StaticBody3D Trunks(string name, IReadOnlyList<AreaProp> trees, float radius)
    {
        var body = new StaticBody3D { Name = name };
        for (var i = 0; i < trees.Count; i++)
        {
            var tree = trees[i];
            body.AddChild(new CollisionShape3D
            {
                Name = $"Trunk{i}",
                Shape = new CylinderShape3D { Radius = radius * tree.Scale, Height = 2f },
                Position = new Vector3(tree.X, 1f, tree.Z),
            });
        }

        return body;
    }

    /// <summary>A whole merged model under the shared material at a spot on the ground.</summary>
    public static MeshInstance3D Model(string name, Mesh mesh, float x, float z, float yawDegrees = 0f) => new()
    {
        Name = name,
        Mesh = mesh,
        MaterialOverride = ArtMaterials.Shared,
        Position = new Vector3(x, 0f, z),
        RotationDegrees = new Vector3(0f, yawDegrees, 0f),
    };

    private static Transform3D PropTransform(AreaProp p) => new(
        new Basis(Vector3.Up, Mathf.DegToRad(p.YawDegrees)).Scaled(Vector3.One * p.Scale),
        new Vector3(p.X, 0f, p.Z));
}
