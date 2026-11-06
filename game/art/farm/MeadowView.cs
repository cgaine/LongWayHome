#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// The farm meadow (#77, ART_DIRECTION §7.2), the script of <c>res://art/farm/Meadow.tscn</c>. On ready it builds,
/// in code, the meshes of the scene's nodes:
/// <list type="bullet">
/// <item>the ground layers, flat and facing up, each a little above the last so they never z-fight: <c>Ground</c>
/// (32 x 26 m, y 0), <c>ShadePatches</c> (y 0.003), <c>MownMargin</c> (the 1 m band around the plot, y 0.006) and
/// <c>Path</c> (the 1.2 m strip from the door to the margin, y 0.008). Inside the plot only <c>Ground</c> lies, below
/// the plot base (y 0.002) and the Tile slabs (#78);</item>
/// <item>the <c>PathStones</c> and the stone <c>Edging</c> around the plot, from
/// <see cref="MeadowLayout.PathStones"/> and <see cref="MeadowLayout.EdgingStones"/>;</item>
/// <item>the scattered props from <see cref="MeadowLayout.Generate(int)"/> with <see cref="MeadowLayout.FarmSeed"/>:
/// one <c>MultiMeshInstance3D</c> per part, named <c>&lt;Kind&gt;&lt;Part&gt;</c>, each instance placed with its
/// prop's yaw and scale.</item>
/// </list>
/// Every mesh is a low-poly primitive, or several merged into one, and takes its colour from the shared palette
/// material through <see cref="ArtMaterials.Apply"/> with its <see cref="MeadowLayout.PartRoles"/> role. Nothing here
/// has collision, so the meadow never blocks the Player or the camera. Reads and changes no game state.
/// </summary>
public partial class MeadowView : Node3D
{
    private const float GroundHalfX = 16f;
    private const float GroundHalfZ = 13f;
    private const float PlotHalfX = MeadowLayout.PlotHalfX;
    private const float PlotHalfZ = MeadowLayout.PlotHalfZ;
    private const float MarginHalfX = PlotHalfX + MeadowLayout.MarginWidth;
    private const float MarginHalfZ = PlotHalfZ + MeadowLayout.MarginWidth;
    private const float PathHalfWidth = MeadowLayout.PathHalfWidth;
    private const float DoorZ = -7f;

    private const float PatchY = 0.003f;
    private const float MarginY = 0.006f;
    private const float PathY = 0.008f;

    public override void _Ready()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed).ToLookup(p => p.Kind);
        List<MeadowProp> Of(string kind) => props[kind].ToList();

        Ground("Ground", new PlaneMesh { Size = new Vector2(2f * GroundHalfX, 2f * GroundHalfZ) });
        Ground("ShadePatches", ShadePatches(props["ShadePatch"]));
        Ground("MownMargin", MownMargin());
        Ground("Path", Flat(PathY, (-PathHalfWidth, DoorZ, PathHalfWidth, -MarginHalfZ)));

        Fill("PathStones", Shapes.PathStone(), MeadowLayout.PathStones(), shadows: false);
        Fill("Edging", Shapes.EdgingStone(), MeadowLayout.EdgingStones(), shadows: false);

        Fill("TuftBlades", Shapes.TuftBlades(), Of("Tuft"), shadows: false);
        Fill("FrondFrond", Shapes.FrondFrond(), Of("Frond"), shadows: false);
        Fill("FrondCurl", Shapes.FrondCurl(), Of("Frond"), shadows: false);
        Fill("PuffballStem", Shapes.PuffballStem(), Of("Puffball"), shadows: false);
        Fill("PuffballHead", Shapes.PuffballHead(), Of("Puffball"), shadows: false);
        Fill("BeadStalkStalk", Shapes.BeadStalkStalk(), Of("BeadStalk"), shadows: false);
        Fill("BeadStalkBeads", Shapes.BeadStalkBeads(), Of("BeadStalk"), shadows: false);
        Fill("RockRock", Shapes.Rock(), Of("Rock"), shadows: true);
        Fill("StumpBark", Shapes.StumpBark(), Of("Stump"), shadows: true);
        Fill("StumpTop", Shapes.StumpTop(), Of("Stump"), shadows: true);
        Fill("SaplingStem", Shapes.SaplingStem(), Of("Sapling"), shadows: true);
        Fill("SaplingCrown", Shapes.SaplingCrown(), Of("Sapling"), shadows: true);
    }

    private void Ground(string name, Mesh mesh)
    {
        var node = GetNode<MeshInstance3D>(name);
        node.Mesh = mesh;
        node.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        Colour(node);
    }

    private void Fill(string name, Mesh mesh, IReadOnlyList<MeadowProp> props, bool shadows)
    {
        var multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = mesh,
            InstanceCount = props.Count,
        };
        for (var i = 0; i < props.Count; i++)
        {
            var p = props[i];
            var basis = new Basis(Vector3.Up, Mathf.DegToRad(p.YawDegrees)).Scaled(Vector3.One * p.Scale);
            multimesh.SetInstanceTransform(i, new Transform3D(basis, new Vector3(p.X, 0f, p.Z)));
        }

        var node = GetNode<MultiMeshInstance3D>(name);
        node.Multimesh = multimesh;
        node.CastShadow = shadows
            ? GeometryInstance3D.ShadowCastingSetting.On
            : GeometryInstance3D.ShadowCastingSetting.Off;
        Colour(node);
    }

    private static void Colour(GeometryInstance3D node) =>
        ArtMaterials.Apply(node, MeadowLayout.PartRoles[node.Name.ToString()]);

    /// <summary>Each patch outline as a triangle fan around its centre, kept on the ground plane's 32 x 26 m.</summary>
    private static Mesh ShadePatches(IEnumerable<MeadowProp> patches)
    {
        var surface = UpFacing();
        foreach (var patch in patches)
        {
            var outline = MeadowLayout.ShadePatchOutline(patch)
                .Select(p => OnGround(p.X, p.Z, PatchY))
                .ToList();
            var centre = OnGround(patch.X, patch.Z, PatchY);
            for (var i = 0; i < outline.Count; i++)
            {
                UpTriangle(surface, centre, outline[i], outline[(i + 1) % outline.Count]);
            }
        }

        return surface.Commit();
    }

    private static Vector3 OnGround(float x, float z, float y) =>
        new(Mathf.Clamp(x, -GroundHalfX, GroundHalfX), y, Mathf.Clamp(z, -GroundHalfZ, GroundHalfZ));

    /// <summary>The margin rectangle x −7..7, z −5.5..5.5 less the plot: north and south strips, then west and east.</summary>
    private static Mesh MownMargin() => Flat(
        MarginY,
        (-MarginHalfX, -MarginHalfZ, MarginHalfX, -PlotHalfZ),
        (-MarginHalfX, PlotHalfZ, MarginHalfX, MarginHalfZ),
        (-MarginHalfX, -PlotHalfZ, -PlotHalfX, PlotHalfZ),
        (PlotHalfX, -PlotHalfZ, MarginHalfX, PlotHalfZ));

    /// <summary>Flat up-facing rectangles (min x, min z, max x, max z) at height <paramref name="y"/>.</summary>
    private static Mesh Flat(float y, params (float X0, float Z0, float X1, float Z1)[] rects)
    {
        var surface = UpFacing();
        foreach (var (x0, z0, x1, z1) in rects)
        {
            var a = new Vector3(x0, y, z0);
            var b = new Vector3(x1, y, z0);
            var c = new Vector3(x1, y, z1);
            var d = new Vector3(x0, y, z1);
            UpTriangle(surface, a, b, c);
            UpTriangle(surface, a, c, d);
        }

        return surface.Commit();
    }

    private static SurfaceTool UpFacing()
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        return surface;
    }

    /// <summary>Adds a triangle whose front faces up: Godot's front faces wind clockwise, seen from the front.</summary>
    private static void UpTriangle(SurfaceTool surface, Vector3 a, Vector3 b, Vector3 c)
    {
        if ((b - a).Cross(c - a).Y > 0f)
        {
            (b, c) = (c, b);
        }

        surface.SetNormal(Vector3.Up);
        surface.AddVertex(a);
        surface.SetNormal(Vector3.Up);
        surface.AddVertex(b);
        surface.SetNormal(Vector3.Up);
        surface.AddVertex(c);
    }

    /// <summary>
    /// The low-poly part shapes, each standing on the ground at its origin (y 0) and facing +Z at yaw 0. A part made
    /// of several primitives is merged into one mesh, so each part is still one MultiMesh draw.
    /// </summary>
    private static class Shapes
    {
        public static Mesh PathStone() => Merge(
            (Cylinder(0.25f, 0.25f, 0.04f, 7), At(new Vector3(0f, 0.01f, 0f), scale: new Vector3(1f, 1f, 0.8f))));

        /// <summary>About 0.25 m wide and 0.15 m tall, sunk so its top is at 0.105 m (0.116 m at scale 1.1).</summary>
        public static Mesh EdgingStone() => Merge(
            (Sphere(0.125f, 0.25f, 6, 3), At(new Vector3(0f, 0.03f, 0f), scale: new Vector3(1f, 0.6f, 0.8f))));

        /// <summary>Three thin blades fanned out and leaning away from the centre.</summary>
        public static Mesh TuftBlades() => Merge(
            Enumerable.Range(0, 3)
                .Select(i => (Mesh: (Mesh)new PrismMesh { Size = new Vector3(0.05f, 0.3f + 0.04f * i, 0.015f) },
                    At: Lean(i * 120f + 15f, 20f, 0.15f + 0.02f * i, 0.03f)))
                .ToArray());

        /// <summary>Two flat fronds leaning out on opposite sides.</summary>
        public static Mesh FrondFrond() => Merge(
            ((Mesh)new PrismMesh { Size = new Vector3(0.12f, 0.45f, 0.025f) }, Lean(0f, 32f, 0.225f, 0f)),
            ((Mesh)new PrismMesh { Size = new Vector3(0.1f, 0.38f, 0.025f) }, Lean(160f, 38f, 0.19f, 0f)));

        /// <summary>A small curl standing at the tip of each frond.</summary>
        public static Mesh FrondCurl() => Merge(
            (Curl(), Tip(0f, 32f, 0.45f)),
            (Curl(), Tip(160f, 38f, 0.38f)));

        public static Mesh PuffballStem() => Merge((Cylinder(0.012f, 0.018f, 0.32f, 5), At(new Vector3(0f, 0.16f, 0f))));

        public static Mesh PuffballHead() => Merge((Sphere(0.08f, 0.14f, 6, 3), At(new Vector3(0f, 0.36f, 0f))));

        public static Mesh BeadStalkStalk() => Merge((Cylinder(0.01f, 0.015f, 0.5f, 4), At(new Vector3(0f, 0.25f, 0f))));

        /// <summary>Five amber beads up the top of the stalk, alternating sides.</summary>
        public static Mesh BeadStalkBeads() => Merge(
            Enumerable.Range(0, 5)
                .Select(i => (Mesh: (Mesh)Sphere(0.028f, 0.05f, 5, 2),
                    At: At(new Vector3(i % 2 == 0 ? 0.025f : -0.025f, 0.3f + 0.045f * i, 0f))))
                .ToArray());

        /// <summary>A squat, uneven lump about 0.6 m wide, sunk a little into the ground.</summary>
        public static Mesh Rock() => Merge(
            (Sphere(0.3f, 0.4f, 6, 3), At(new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0f, 8f), new Vector3(1f, 0.75f, 0.8f))),
            (Sphere(0.16f, 0.22f, 5, 2), At(new Vector3(0.22f, 0.04f, 0.12f), new Vector3(0f, 30f, 0f))));

        public static Mesh StumpBark() => Merge((Cylinder(0.2f, 0.26f, 0.35f, 7), At(new Vector3(0f, 0.175f, 0f))));

        /// <summary>The cut face: a thin disc inside the bark, standing 0.02 m proud of it.</summary>
        public static Mesh StumpTop() => Merge((Cylinder(0.18f, 0.18f, 0.03f, 7), At(new Vector3(0f, 0.355f, 0f))));

        public static Mesh SaplingStem() => Merge((Cylinder(0.025f, 0.04f, 0.75f, 5), At(new Vector3(0f, 0.375f, 0f))));

        /// <summary>Two stacked leafy lumps, topping out at about 1.24 m.</summary>
        public static Mesh SaplingCrown() => Merge(
            (Sphere(0.32f, 0.55f, 6, 3), At(new Vector3(0f, 0.85f, 0f))),
            (Sphere(0.22f, 0.38f, 6, 3), At(new Vector3(0.04f, 1.05f, 0.02f), new Vector3(0f, 25f, 0f))));

        private static Mesh Curl() => new TorusMesh
        {
            InnerRadius = 0.025f,
            OuterRadius = 0.055f,
            Rings = 6,
            RingSegments = 4,
        };

        private static CylinderMesh Cylinder(float top, float bottom, float height, int segments) => new()
        {
            TopRadius = top,
            BottomRadius = bottom,
            Height = height,
            RadialSegments = segments,
            Rings = 1,
        };

        private static SphereMesh Sphere(float radius, float height, int segments, int rings) => new()
        {
            Radius = radius,
            Height = height,
            RadialSegments = segments,
            Rings = rings,
        };

        private static Transform3D At(Vector3 position, Vector3? rotationDegrees = null, Vector3? scale = null)
        {
            var r = rotationDegrees ?? Vector3.Zero;
            var basis = Basis.FromEuler(new Vector3(Mathf.DegToRad(r.X), Mathf.DegToRad(r.Y), Mathf.DegToRad(r.Z)))
                * Basis.FromScale(scale ?? Vector3.One);
            return new Transform3D(basis, position);
        }

        /// <summary>
        /// A part whose centre is <paramref name="lift"/> up its own length, leaning <paramref name="tiltDegrees"/>
        /// from upright towards the heading <paramref name="yawDegrees"/>, with its foot <paramref name="offset"/> out.
        /// </summary>
        private static Transform3D Lean(float yawDegrees, float tiltDegrees, float lift, float offset) =>
            new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(yawDegrees)), Vector3.Zero)
            * new Transform3D(Basis.Identity, new Vector3(0f, 0f, offset))
            * new Transform3D(new Basis(Vector3.Right, Mathf.DegToRad(tiltDegrees)), Vector3.Zero)
            * new Transform3D(Basis.Identity, new Vector3(0f, lift, 0f));

        /// <summary>A curl at the tip of a part <paramref name="length"/> long leaning as in <see cref="Lean"/>, standing upright.</summary>
        private static Transform3D Tip(float yawDegrees, float tiltDegrees, float length)
        {
            var tilt = Mathf.DegToRad(tiltDegrees);
            var tip = new Vector3(0f, length * Mathf.Cos(tilt), length * Mathf.Sin(tilt));
            return new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(yawDegrees)), Vector3.Zero)
                * new Transform3D(new Basis(Vector3.Right, Mathf.Pi / 2f), tip);
        }

        /// <summary>
        /// Merges the parts into one mesh. Normals go through the inverse transpose of each part's basis and are
        /// re-normalised, so squashed shapes (the edging stones, rocks) keep true normals for stepped shading.
        /// </summary>
        private static Mesh Merge(params (Mesh Mesh, Transform3D At)[] parts)
        {
            var surface = new SurfaceTool();
            surface.Begin(Mesh.PrimitiveType.Triangles);
            var offset = 0;
            foreach (var (mesh, at) in parts)
            {
                var arrays = mesh.SurfaceGetArrays(0);
                var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                var normalBasis = at.Basis.Inverse().Transposed();
                for (var i = 0; i < vertices.Length; i++)
                {
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
}
