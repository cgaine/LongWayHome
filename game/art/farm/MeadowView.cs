#nullable enable

using System.Collections.Generic;
using System.Linq;
using Godot;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Game.Core;

namespace LongWayHome.Game.Art;

/// <summary>
/// The farm meadow (#77, ART_DIRECTION §7.2), the script of <c>res://art/farm/Meadow.tscn</c>. On ready it builds,
/// in code, the meshes of the scene's nodes:
/// <list type="bullet">
/// <item>the ground layers, flat and facing up, each a little above the last so they never z-fight: <c>Ground</c>
/// (the whole farm inside its walls, x −36..36.5, z −13..31.5, y 0), <c>ShadePatches</c> (y 0.003) and <c>Path</c>
/// (the 1.2 m strip from the door to the grid's north edge, y 0.008). Over the 72 x 36 grid only <c>Ground</c> lies,
/// below the plot base (y 0.002) and the Tile slabs (#78); there is no mown margin or edging, since every Tile of the
/// grid is farmable (#150);</item>
/// <item>the <c>PathStones</c>, from <see cref="MeadowLayout.PathStones"/>;</item>
/// <item>the scattered props from <see cref="MeadowLayout.Generate(int)"/> with <see cref="MeadowLayout.FarmSeed"/>:
/// one <c>MultiMeshInstance3D</c> per part, named <c>&lt;Kind&gt;&lt;Part&gt;</c>, each instance placed with its
/// prop's yaw and scale;</item>
/// <item>the wild grass cover, built here in code under a <c>GrassCover</c> node: the 1 m patches of
/// <see cref="MeadowLayout.GrassCover"/>, one <c>MultiMeshInstance3D</c> per <see cref="GrassTileArt"/> layout
/// (<c>Grass0</c> to <c>Grass7</c>) drawing its <see cref="GrassTileModels.MeadowMeshFor"/> mesh, coloured by its UVs under
/// the shared material, so the whole meadow's grass is eight draws.</item>
/// </list>
/// Every mesh is a low-poly primitive, or several merged into one, and takes its colour from the shared palette
/// material through <see cref="ArtMaterials.Apply"/> with its <see cref="MeadowLayout.PartRoles"/> role. Nothing here
/// has collision, so the meadow never blocks the Player or the camera.
/// <para>
/// The grass follows the Season (#79): after building, and again on <see cref="GameSession.NightCompleted"/>, the only
/// time the date changes (BR-G1), it reads the <see cref="GameRoot"/> session's <c>Game.Date.Season</c> and applies
/// <see cref="SeasonGrass.For"/> to <c>Ground</c>, <c>ShadePatches</c>, <c>TuftBlades</c> and the
/// grass cover (each layout's MultiMesh takes the Season's mesh). With
/// no session (the data failed to load) it stays in summer. Native plants, rocks, stumps, saplings and path
/// never change. Only reads the date; changes no game state.
/// </para>
/// </summary>
public partial class MeadowView : Node3D
{
    private const float GroundMinX = MeadowLayout.GroundWestX;
    private const float GroundMaxX = MeadowLayout.RiverWestX;
    private const float GroundMinZ = MeadowLayout.GroundNorthZ;
    private const float GroundMaxZ = MeadowLayout.GroundSouthZ;
    private const float PathHalfWidth = MeadowLayout.PathHalfWidth;
    private const float DoorZ = MeadowLayout.DoorZ;

    private const float PatchY = 0.003f;
    private const float PathY = 0.008f;

    private const string GameRootPath = "/root/GameRoot";

    private GameSession? _session;

    /// <summary>The tuft props, kept to rebuild the tufts' instance transforms at each Season's height.</summary>
    private List<MeadowProp> _tuftProps = [];

    /// <summary>The grass cover's MultiMeshes, by layout, whose mesh each Season swaps.</summary>
    private readonly List<(int Variant, MultiMesh Multimesh)> _cover = [];

    /// <summary>The Season grass last applied (null before the first), so an unchanged Season does nothing.</summary>
    private SeasonGrass? _applied;

    public override void _Ready()
    {
        var props = MeadowLayout.Generate(MeadowLayout.FarmSeed).ToLookup(p => p.Kind);
        List<MeadowProp> Of(string kind) => props[kind].ToList();

        Ground("Ground", new PlaneMesh
        {
            Size = new Vector2(GroundMaxX - GroundMinX, GroundMaxZ - GroundMinZ),
            CenterOffset = new Vector3((GroundMinX + GroundMaxX) / 2f, 0f, (GroundMinZ + GroundMaxZ) / 2f),
        });
        Ground("ShadePatches", ShadePatches(props["ShadePatch"]));
        Ground("Path", Flat(PathY, (-PathHalfWidth, DoorZ, PathHalfWidth, MeadowLayout.GridNorthZ)));

        Fill("PathStones", Shapes.PathStone(), MeadowLayout.PathStones(), shadows: false);

        _tuftProps = Of("Tuft");
        Fill("TuftBlades", Shapes.TuftBlades(), _tuftProps, shadows: false);
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
        BuildGrassCover();

        _session = GetNode<GameRoot>(GameRootPath).Session;
        if (_session is null)
        {
            // The data failed to load and GameRoot is already quitting: the meadow stays in summer.
            ApplySeason(Season.Summer);
            return;
        }

        _session.NightCompleted += OnNightCompleted;
        OnNightCompleted();
    }

    public override void _ExitTree()
    {
        if (_session is not null)
        {
            _session.NightCompleted -= OnNightCompleted;
        }
    }

    /// <summary>Shows the current Season's grass: on ready, and after each Night, the only time it changes (BR-G1).</summary>
    private void OnNightCompleted() => ApplySeason(_session!.Game.Date.Season);

    /// <summary>
    /// Gives the ground and the tufts <paramref name="season"/>'s roles, sets the tufts' height and
    /// shows or hides the shade patches. Only swaps swatches, visibility and instance transforms; creates and frees no
    /// nodes, so the swap costs nothing on the Night it happens.
    /// </summary>
    private void ApplySeason(Season season)
    {
        var grass = SeasonGrass.For(season);
        if (grass == _applied)
        {
            return;
        }

        ArtMaterials.Apply(GetNode<MeshInstance3D>("Ground"), grass.Ground);

        var shadePatches = GetNode<MeshInstance3D>("ShadePatches");
        ArtMaterials.Apply(shadePatches, grass.ShadePatches);
        shadePatches.Visible = grass.ShowShadePatches;

        foreach (var (variant, multimesh) in _cover)
        {
            multimesh.Mesh = GrassTileModels.MeadowMeshFor(variant, grass);
        }

        var tufts = GetNode<MultiMeshInstance3D>("TuftBlades");
        ArtMaterials.Apply(tufts, grass.Tufts);
        if (!Mathf.IsEqualApprox(grass.TuftHeightScale, _applied?.TuftHeightScale ?? 1f))
        {
            // Rebuilt from the props each time, so the heights never compound across Seasons.
            var height = new Vector3(1f, grass.TuftHeightScale, 1f);
            for (var i = 0; i < _tuftProps.Count; i++)
            {
                var t = PropTransform(_tuftProps[i]);
                tufts.Multimesh.SetInstanceTransform(i, t with { Basis = t.Basis.Scaled(height) });
            }
        }

        _applied = grass;
    }

    /// <summary>
    /// The wild grass cover: one MultiMesh per layout under a <c>GrassCover</c> node, its instances the layout's patches
    /// from <see cref="MeadowLayout.GrassCover"/>, coloured by the merged mesh's UVs under the shared material. The mesh
    /// itself is set for the Season by <see cref="ApplySeason"/>.
    /// </summary>
    private void BuildGrassCover()
    {
        var root = new Node3D { Name = "GrassCover" };
        AddChild(root);
        var patches = MeadowLayout.GrassCover().ToLookup(p => p.Kind);
        for (var variant = 0; variant < GrassTileArt.Variants; variant++)
        {
            var placed = patches[$"Grass{variant}"].ToList();
            var multimesh = new MultiMesh
            {
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                Mesh = GrassTileModels.MeadowMeshFor(variant, SeasonGrass.For(Season.Summer)),
                InstanceCount = placed.Count,
            };
            for (var i = 0; i < placed.Count; i++)
            {
                multimesh.SetInstanceTransform(i, PropTransform(placed[i]));
            }

            root.AddChild(new MultiMeshInstance3D
            {
                Name = $"Grass{variant}",
                Multimesh = multimesh,
                MaterialOverride = ArtMaterials.Shared,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
            _cover.Add((variant, multimesh));
        }
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
            multimesh.SetInstanceTransform(i, PropTransform(props[i]));
        }

        var node = GetNode<MultiMeshInstance3D>(name);
        node.Multimesh = multimesh;
        node.CastShadow = shadows
            ? GeometryInstance3D.ShadowCastingSetting.On
            : GeometryInstance3D.ShadowCastingSetting.Off;
        Colour(node);
    }

    /// <summary>A prop standing on the ground at its position, turned by its yaw and sized by its scale.</summary>
    private static Transform3D PropTransform(MeadowProp p) => new(
        new Basis(Vector3.Up, Mathf.DegToRad(p.YawDegrees)).Scaled(Vector3.One * p.Scale),
        new Vector3(p.X, 0f, p.Z));

    private static void Colour(GeometryInstance3D node) =>
        ArtMaterials.Apply(node, MeadowLayout.PartRoles[node.Name.ToString()]);

    /// <summary>Each patch outline as a triangle fan around its centre, kept on the ground plane.</summary>
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
        new(Mathf.Clamp(x, GroundMinX, GroundMaxX), y, Mathf.Clamp(z, GroundMinZ, GroundMaxZ));

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

        /// <summary>Merges the parts into one mesh (<see cref="ArtMeshes.Merge"/>), so each part is one MultiMesh draw.</summary>
        private static Mesh Merge(params (Mesh Mesh, Transform3D At)[] parts) =>
            ArtMeshes.Merge(parts.Select(p => new MeshPart(p.Mesh, p.At)));
    }
}
