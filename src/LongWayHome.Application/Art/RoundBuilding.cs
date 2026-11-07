using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>
/// The shared shape of Ember Bay's round, grown buildings (ART_DIRECTION §7.1 and §7.3, the concept art's "Round
/// buildings" page): a drum of mauve timber staves on a stone base, under a cone of overlapping leaves with a fringe of
/// leaf tips, living trunk ribs climbing the wall and running up the roof, and a sprouting crown. Every piece can be
/// alive or run-down by its angle round the drum, so one corner can come back to life before the rest (the general
/// store's Maggie's corner). Positions are building-local: <see cref="Centre"/> on the ground, angle 0 facing +Z.
/// Pure C#; the parts follow the <see cref="CropPart"/> conventions.
/// </summary>
internal sealed class RoundBuilding
{
    private const float StaveDepth = 0.08f;
    private const float BaseTop = 0.3f;
    private const float LeafLength = 1.0f;
    private const float LeafTilt = 6f;

    /// <param name="prefix">Prefixed to every part name, so two buildings in one table don't clash.</param>
    /// <param name="centre">The drum's centre on the ground.</param>
    /// <param name="wallRadius">The drum's outer radius.</param>
    /// <param name="wallTop">The height of the wall, where the eaves start.</param>
    /// <param name="eaveRadius">How far the roof's eaves reach from the centre.</param>
    /// <param name="apexY">The height of the roof cone's apex.</param>
    public RoundBuilding(string prefix, Vector3 centre, float wallRadius, float wallTop, float eaveRadius, float apexY)
    {
        Prefix = prefix;
        Centre = centre;
        WallRadius = wallRadius;
        WallTop = wallTop;
        EaveRadius = eaveRadius;
        ApexY = apexY;
        var rise = apexY - wallTop;
        SlopeLength = MathF.Sqrt(eaveRadius * eaveRadius + rise * rise);
        SlopeCos = eaveRadius / SlopeLength;
        SlopeSin = rise / SlopeLength;
        SlopeDegrees = MathF.Atan2(rise, eaveRadius) * 180f / MathF.PI;
    }

    public string Prefix { get; }

    public Vector3 Centre { get; }

    public float WallRadius { get; }

    public float WallTop { get; }

    public float EaveRadius { get; }

    public float ApexY { get; }

    public float SlopeLength { get; }

    public float SlopeCos { get; }

    public float SlopeSin { get; }

    public float SlopeDegrees { get; }

    // ---- the drum ----

    /// <summary>
    /// The dark core, the stone base and the staves, each stave in two pieces split at a height that steps round the
    /// drum. A stave is mauve where <paramref name="alive"/> holds for its angle, greyed where not; the
    /// <paramref name="missing"/> pieces (stave index, upper?) are left out to show the core. With <paramref name="stoneTo"/>
    /// above zero the wall is stone up to that height (a stone storey, the blacksmith's): staggered courses of blocks, each
    /// about 0.34 m tall, in <c>stone</c> and <c>rock</c>, standing a little proud of the staves, and single staves run
    /// from there to the top.
    /// </summary>
    public void Drum(List<CropPart> parts, int staves, Func<float, bool> alive, IReadOnlyCollection<(int Stave, bool Upper)> missing, float stoneTo = 0f)
    {
        parts.Add(Cylinder("Core", 2f * (WallRadius - StaveDepth), WallTop, 2f * (WallRadius - StaveDepth), Centre + new Vector3(0f, WallTop / 2f, 0f), "timber-trim"));
        parts.Add(Cylinder("Base", 2f * WallRadius + 0.16f, BaseTop + 0.02f, 2f * WallRadius + 0.14f, Centre + new Vector3(0f, (BaseTop + 0.02f) / 2f, 0f), "stone"));

        var width = 2f * WallRadius * MathF.Sin(MathF.PI / staves) - 0.015f;
        for (var i = 0; i < staves; i++)
        {
            var angle = 360f * i / staves;
            var live = alive(Signed(angle));
            // The split steps round the drum between about 37% and 77% of the wall, so it suits any wall height; on a
            // stone storey the staves start where the stone ends.
            var split = stoneTo > 0f ? stoneTo : WallTop * (0.37f + 0.1f * (i * 7 % 5));
            var lower = live ? (i % 4 == 2 ? "timber-side" : "timber") : (i % 3 == 1 ? "timber-side" : "wall-grey");
            var upper = live ? (i % 4 == 0 ? "timber-side" : "timber") : (i % 3 == 2 ? "timber-side" : "wall-grey");
            var tilt = live ? 0f : 0.6f * (i % 3 - 1);
            if (stoneTo <= 0f && !missing.Contains((i, false)))
            {
                var height = split - BaseTop - 0.01f;
                parts.Add(Box($"Stave{i}Lower", width, height, StaveDepth, OnWall(angle, 0f, BaseTop + height / 2f, -StaveDepth / 2f), lower, yaw: angle, roll: tilt));
            }

            if (!missing.Contains((i, true)))
            {
                var height = WallTop - split - 0.01f;
                parts.Add(Box($"Stave{i}Upper", width, height, StaveDepth, OnWall(angle, 0f, WallTop - height / 2f, -StaveDepth / 2f), upper, yaw: angle, roll: -tilt));
            }

            for (var g = 0; g < 2; g++)
            {
                var y = WallTop * (0.25f + 0.33f * g + 0.05f * (i * 3 % 4));
                var piece = (i, y >= split);
                if (missing.Contains(piece) || y > WallTop - 0.1f || y < stoneTo + 0.1f) continue;
                parts.Add(Box($"Grain{i}_{g}", width * 0.6f, 0.016f, 0.01f, OnWall(angle, 0.04f * (g * 2 - 1), y, 0.003f), "timber-grain",
                    yaw: angle, roll: 3f * (g * 2 - 1)));
            }
        }

        if (stoneTo <= 0f) return;
        var courses = Math.Max(1, (int)MathF.Round((stoneTo - BaseTop) / 0.34f));
        var course = (stoneTo - BaseTop) / courses;
        for (var c = 0; c < courses; c++)
        {
            for (var i = 0; i < staves; i++)
            {
                // Every other course is offset half a block, so the joints never line up.
                var angle = 360f * (i + 0.5f * (c % 2)) / staves;
                var role = (i * 5 + c * 3) % 4 == 0 ? "rock" : "stone";
                var lean = 0.8f * ((i + c) % 3 - 1);
                parts.Add(Box($"Stone{c}_{i}", width - 0.02f, course - 0.025f, StaveDepth + 0.06f,
                    OnWall(angle, 0f, BaseTop + course * (c + 0.5f), -StaveDepth / 2f + 0.03f), role, yaw: angle, roll: lean));
            }
        }
    }

    // ---- the ribs ----

    /// <summary>
    /// A living rib at <paramref name="angle"/>: a twisting trunk up the wall, bending out under the eaves, then up the
    /// roof above the leaves. Alive it is violet with a leaf cluster at the eave; run-down it is grey with dead twigs.
    /// </summary>
    public void Rib(List<CropPart> parts, int index, float angle, bool alive)
    {
        var role = alive ? "trunk" : "trunk-grey";
        var top = WallTop - 0.05f;
        for (var i = 0; i < 4; i++)
        {
            var bottom = top * i / 4f;
            var upper = top * (i + 1) / 4f;
            var at = OnWall(angle + 0.8f * (i % 2 * 2 - 1), 0f, (bottom + upper) / 2f, 0.1f);
            parts.Add(new CropPart($"{Prefix}Rib{index}Seg{i}", CropPartShape.Cylinder, 0.34f - 0.025f * i, upper - bottom, 0.31f - 0.025f * i,
                at.X, at.Y, at.Z, 0f, 15f * i + 11f * index, 0f, role));
        }

        parts.Add(Beam($"Rib{index}Elbow", 0.24f, 0.22f, Radial(angle, WallRadius + 0.1f, WallTop - 0.1f), OnRoof(angle, 0f, 0.3f), role));
        var along = new[] { 0f, SlopeLength * 0.24f, SlopeLength * 0.48f, SlopeLength * 0.7f };
        for (var i = 0; i < along.Length - 1; i++)
        {
            var thickness = 0.24f - 0.035f * i;
            parts.Add(Beam($"Rib{index}Roof{i}", thickness, thickness, OnRoof(angle, along[i], 0.3f), OnRoof(angle, along[i + 1] + 0.05f, 0.3f), role));
        }

        var tip = Radial(angle, EaveRadius + 0.08f, WallTop + 0.1f);
        if (alive)
        {
            parts.Add(Sphere($"Rib{index}Cluster", 0.5f, 0.38f, tip, "roof-tips"));
            parts.Add(Sphere($"Rib{index}ClusterUnder", 0.3f, 0.24f, Radial(angle + 4f, EaveRadius + 0.24f, WallTop + 0.02f), "leaf-underside"));
        }
        else
        {
            (float Yaw, float Tilt, float Length)[] twigs = [(-40f, 55f, 0.42f), (5f, 35f, 0.36f), (45f, 60f, 0.3f)];
            for (var i = 0; i < twigs.Length; i++)
            {
                parts.Add(Rising($"Rib{index}Twig{i}", 0.035f, twigs[i].Length, tip, twigs[i].Tilt, angle + twigs[i].Yaw, "trunk-grey"));
            }
        }
    }

    // ---- the leaf roof ----

    /// <summary>
    /// The roof's dark core cone and its rings of overlapping leaves up to the crown cap, with the eave fringe: green
    /// (and glowing tips) where <paramref name="alive"/> holds for the angle, wilted and thinned where not, with the
    /// <paramref name="gaps"/> (ring, from angle, to angle) left open in the run-down parts.
    /// </summary>
    public void Roof(List<CropPart> parts, Func<float, bool> alive, IReadOnlyList<(int Ring, float From, float To)> gaps, bool crownAlive)
    {
        var anyAlive = Enumerable.Range(0, 36).Any(i => alive(Signed(i * 10f)));
        parts.Add(Cylinder("RoofCore", 2f * EaveRadius, ApexY - WallTop, 0f, Centre + new Vector3(0f, (WallTop + ApexY) / 2f, 0f), anyAlive ? "roof-far" : "timber-trim"));

        const float ringStart = 0.45f;
        const float ringStep = 0.72f;
        for (var ring = 0; ; ring++)
        {
            var s = ringStart + ringStep * ring;
            var radius = EaveRadius - s * SlopeCos;
            if (radius < 0.9f) break;
            var count = (int)MathF.Round(2f * MathF.PI * radius / 0.85f);
            var width = 2f * MathF.PI * radius / count * 1.25f;
            for (var i = 0; i < count; i++)
            {
                var angle = Signed(360f * i / count + (ring % 2) * 180f / count);
                var live = alive(angle);
                if (!live && gaps.Any(gap => gap.Ring == ring && angle >= gap.From && angle <= gap.To)) continue;
                var role = live ? (ring % 2 == 0 ? "foliage" : "roof-rows") : (ring % 2 == 0 ? "roof-wilted" : "tips-dry");
                var lift = 0.05f + 0.025f * ring + 0.012f * (i % 2);
                var wilt = live ? 0f : 4f * ((i + ring) % 3 - 1);
                var at = OnRoof(angle, s, lift);
                parts.Add(new CropPart($"{Prefix}Leaf{ring}_{i}", CropPartShape.Box, width, LeafLength, 0.04f, at.X, at.Y, at.Z,
                    SlopeDegrees - 90f - LeafTilt, angle, wilt, role));
            }
        }

        // The fringe of leaf tips under the eaves: every one where alive (glowing at night), sparse and dry where not.
        var tips = (int)MathF.Round(2f * MathF.PI * EaveRadius / 0.85f);
        for (var i = 0; i < tips; i++)
        {
            var angle = Signed(360f * i / tips + 6f);
            var live = alive(angle);
            if (!live && i % 3 != 0) continue;
            var length = live ? 0.3f : 0.42f;
            var at = Radial(angle, EaveRadius + 0.03f, WallTop + 0.02f - length / 2f);
            var tip = new CropPart($"{Prefix}Tip{i}", CropPartShape.Prism, 0.3f, length, 0.03f, at.X, at.Y, at.Z,
                live ? -8f : -22f, angle, 180f, live ? "roof-tips" : "tips-dry");
            parts.Add(live ? tip with { Glow = 0.9f } : tip);
        }

        // The crown cap over the top ends of the last ring, and a frond (alive) or dead twigs (run-down).
        const float capRadius = 1.0f;
        var bottom = WallTop + (EaveRadius - capRadius) * (ApexY - WallTop) / EaveRadius + 0.08f;
        var top = ApexY + 0.22f;
        parts.Add(Cylinder("CrownCap", 2f * capRadius, top - bottom, 0f, Centre + new Vector3(0f, (bottom + top) / 2f, 0f), crownAlive ? "roof-rows" : "roof-wilted"));
        var apex = Centre + new Vector3(0f, top - 0.05f, 0f);
        if (crownAlive)
        {
            var at = apex;
            float[] bends = [10f, 30f, 60f, 100f, 140f];
            for (var i = 0; i < bends.Length; i++)
            {
                var length = 0.22f - 0.02f * i;
                var dir = PartBox.Rotate(new Vector3(0f, length, 0f), bends[i], -20f, 0f);
                var centre = at + dir / 2f;
                parts.Add(new CropPart($"{Prefix}CrownFrond{i}", CropPartShape.Box, 0.07f, length, 0.07f, centre.X, centre.Y, centre.Z,
                    bends[i], -20f, 0f, "foliage", Glow: 0.9f));
                at += dir;
            }
        }
        else
        {
            (float Yaw, float Tilt, float Length)[] twigs = [(0f, 20f, 0.32f), (130f, 35f, 0.26f), (240f, 28f, 0.22f)];
            for (var i = 0; i < twigs.Length; i++)
            {
                parts.Add(Rising($"CrownTwig{i}", 0.035f, twigs[i].Length, apex, twigs[i].Tilt, twigs[i].Yaw, "trunk-grey"));
            }
        }
    }

    /// <summary>
    /// A leaf awning round the wall from <paramref name="from"/> to <paramref name="to"/> degrees: a ring of leaves
    /// sloping out and down from <paramref name="height"/>, with a fringe of tips along its edge.
    /// </summary>
    public void Awning(List<CropPart> parts, string name, float from, float to, float height, float depth = 0.8f)
    {
        const float slope = 25f;
        var (sin, cos) = MathF.SinCos(slope * MathF.PI / 180f);
        var midRadius = WallRadius + 0.05f + depth / 2f * cos;
        var span = (to - from) * MathF.PI / 180f * midRadius;
        var count = Math.Max(2, (int)MathF.Ceiling(span / 0.55f));
        var width = span / count * 1.25f;
        for (var i = 0; i < count; i++)
        {
            var angle = from + (to - from) * (i + 0.5f) / count;
            var at = Radial(angle, midRadius, height - depth / 2f * sin) + new Vector3(0f, 0.012f * (i % 2), 0f);
            parts.Add(new CropPart($"{Prefix}{name}Leaf{i}", CropPartShape.Box, width, depth, 0.04f, at.X, at.Y, at.Z,
                slope - 90f, angle, 0f, i % 2 == 0 ? "foliage" : "roof-rows"));
        }

        var edge = WallRadius + 0.05f + depth * cos;
        var tips = Math.Max(2, (int)MathF.Round((to - from) * MathF.PI / 180f * edge / 0.32f));
        for (var i = 0; i < tips; i++)
        {
            var angle = from + (to - from) * (i + 0.5f) / tips;
            var at = Radial(angle, edge + 0.02f, height - depth * sin - 0.1f);
            parts.Add(new CropPart($"{Prefix}{name}Tip{i}", CropPartShape.Prism, 0.24f, 0.22f, 0.03f, at.X, at.Y, at.Z, -6f, angle, 180f, "roof-tips", Glow: 0.9f));
        }

        // Two short living struts holding it out from the wall at its ends.
        foreach (var (end, angle) in new[] { ("A", from + 4f), ("B", to - 4f) })
        {
            parts.Add(Beam($"{name}Strut{end}", 0.07f, 0.07f, Radial(angle, WallRadius + 0.02f, height - 0.7f), Radial(angle, WallRadius + 0.05f + depth * cos * 0.8f, height - depth * sin * 0.8f), "trunk"));
        }
    }

    // ---- doors and windows ----

    /// <summary>An arched door at <paramref name="angle"/>: a timber-trim frame, the door leaf in <paramref name="role"/>, a knob and a step.</summary>
    public void Door(List<CropPart> parts, string name, float angle, float halfWidth, float shoulder, string role, bool spiral)
    {
        var archDiameter = 2f * halfWidth / 0.866f;
        parts.Add(Box($"{name}Frame", 2f * halfWidth + 0.2f, shoulder - BaseTop, 0.1f, OnWall(angle, 0f, (BaseTop + shoulder) / 2f, 0.005f), "timber-trim", yaw: angle));
        parts.Add(Pane($"{name}FrameArch", archDiameter + 0.23f, 0.1f, OnWall(angle, 0f, shoulder, 0.005f), angle, "timber-trim"));
        parts.Add(Box(name, 2f * halfWidth, shoulder - BaseTop, 0.05f, OnWall(angle, 0f, (BaseTop + shoulder) / 2f, 0.06f), role, yaw: angle));
        parts.Add(Pane($"{name}Arch", archDiameter, 0.05f, OnWall(angle, 0f, shoulder, 0.06f), angle, role));
        parts.Add(Sphere($"{name}Knob", 0.08f, 0.08f, OnWall(angle, halfWidth * 0.72f, 1.1f, 0.11f), "rock"));
        parts.Add(Box($"{name}Step", 2f * halfWidth + 0.4f, 0.14f, 0.4f, OnWall(angle, 0f, 0.07f, 0.2f), "stone", yaw: angle));
        if (!spiral) return;
        for (var i = 0; i < 9; i++)
        {
            var turn = 0.9f * i;
            var radius = 0.03f + 0.022f * i;
            var at = OnWall(angle, radius * MathF.Cos(turn), 1.25f + radius * MathF.Sin(turn), 0.088f);
            parts.Add(Box($"{name}Spiral{i}", 0.06f + 0.012f * i, 0.025f, 0.008f, at, "seed-pouch", yaw: angle, roll: turn * 180f / MathF.PI + 90f));
        }
    }

    /// <summary>
    /// A window at <paramref name="angle"/>: a timber-trim frame, a sill, glass in <paramref name="glass"/> and mullions.
    /// A round-topped window (a shop window) has a round arch; otherwise the game's pointed one.
    /// </summary>
    public void Window(List<CropPart> parts, string name, float angle, float width, float sill, float shoulder, string glass, bool roundTop, int mullions)
    {
        var frameArch = roundTop ? width + 0.14f : (width + 0.14f) / 0.866f;
        var glassArch = roundTop ? width : width / 0.866f;
        var sides = roundTop ? 24 : 0;
        parts.Add(Box($"{name}Frame", width + 0.14f, shoulder - sill, 0.05f, OnWall(angle, 0f, (sill + shoulder) / 2f, 0.03f), "timber-trim", yaw: angle));
        parts.Add(Pane($"{name}FrameArch", frameArch, 0.05f, OnWall(angle, 0f, shoulder, 0.03f), angle, "timber-trim") with { Sides = sides });
        parts.Add(Box($"{name}Sill", width + 0.26f, 0.06f, 0.12f, OnWall(angle, 0f, sill - 0.03f, 0.06f), "timber-side", yaw: angle));
        parts.Add(Box($"{name}Glass", width, shoulder - sill - 0.06f, 0.01f, OnWall(angle, 0f, (sill + 0.06f + shoulder) / 2f, 0.06f), glass, yaw: angle));
        parts.Add(Pane($"{name}GlassArch", glassArch, 0.01f, OnWall(angle, 0f, shoulder, 0.06f), angle, glass) with { Sides = sides });
        for (var m = 0; m < mullions; m++)
        {
            var along = width * ((m + 1f) / (mullions + 1f) - 0.5f);
            parts.Add(Box($"{name}Mullion{m}", 0.04f, shoulder - sill + glassArch * 0.35f, 0.014f, OnWall(angle, along, (sill + shoulder + glassArch * 0.35f) / 2f, 0.07f), "timber-trim", yaw: angle));
        }

        if (roundTop)
        {
            parts.Add(Box($"{name}Transom", width, 0.035f, 0.014f, OnWall(angle, 0f, shoulder, 0.07f), "timber-trim", yaw: angle));
        }
    }

    /// <summary>A warm pane just in front of a window's glass, glowing at night (for the night-only lights).</summary>
    public void WindowLight(List<CropPart> parts, string name, float angle, float width, float sill, float shoulder, bool roundTop)
    {
        var arch = roundTop ? width - 0.02f : (width - 0.02f) / 0.866f;
        parts.Add(Box($"{name}", width - 0.02f, shoulder - sill - 0.07f, 0.01f, OnWall(angle, 0f, (sill + 0.07f + shoulder) / 2f, 0.075f), "lantern", yaw: angle) with { Glow = 1f });
        parts.Add(Pane($"{name}Arch", arch, 0.01f, OnWall(angle, 0f, shoulder, 0.075f), angle, "lantern") with { Glow = 1f, Sides = roundTop ? 24 : 0 });
    }

    // ---- placement helpers ----

    /// <summary>The point at <paramref name="radius"/> from the centre at <paramref name="angle"/>, at height y.</summary>
    public Vector3 Radial(float angle, float radius, float y)
    {
        var a = angle * MathF.PI / 180f;
        return Centre + new Vector3(radius * MathF.Sin(a), y, radius * MathF.Cos(a));
    }

    /// <summary>A point on the wall at <paramref name="angle"/>: across it, at a height, out from its outer face.</summary>
    public Vector3 OnWall(float angle, float along, float y, float outward) =>
        Centre + PartBox.Rotate(new Vector3(along, 0f, WallRadius + outward), 0f, angle, 0f) + new Vector3(0f, y, 0f);

    /// <summary>The point <paramref name="s"/> metres up the roof from the eave at <paramref name="angle"/>, lifted off it.</summary>
    public Vector3 OnRoof(float angle, float s, float lift) =>
        Radial(angle, EaveRadius - s * SlopeCos + lift * SlopeSin, WallTop + s * SlopeSin + lift * SlopeCos);

    public CropPart Box(string name, float sizeX, float sizeY, float sizeZ, Vector3 at, string role, float pitch = 0f, float yaw = 0f, float roll = 0f) =>
        new($"{Prefix}{name}", CropPartShape.Box, sizeX, sizeY, sizeZ, at.X, at.Y, at.Z, pitch, yaw, roll, role);

    public CropPart Sphere(string name, float width, float height, Vector3 at, string role) =>
        new($"{Prefix}{name}", CropPartShape.Sphere, width, height, width, at.X, at.Y, at.Z, 0f, 0f, 0f, role);

    public CropPart Cylinder(string name, float bottom, float height, float top, Vector3 at, string role) =>
        new($"{Prefix}{name}", CropPartShape.Cylinder, bottom, height, top, at.X, at.Y, at.Z, 0f, 0f, 0f, role);

    /// <summary>A thin disc facing out of the wall at <paramref name="angle"/> (six-sided unless given more).</summary>
    public CropPart Pane(string name, float diameter, float thickness, Vector3 at, float angle, string role) =>
        new($"{Prefix}{name}", CropPartShape.Cylinder, diameter, thickness, diameter, at.X, at.Y, at.Z, 90f, angle, 0f, role);

    /// <summary>A box from <paramref name="from"/> to <paramref name="to"/> along its local Y.</summary>
    public CropPart Beam(string name, float sizeX, float sizeZ, Vector3 from, Vector3 to, string role)
    {
        var d = to - from;
        var horizontal = MathF.Sqrt(d.X * d.X + d.Z * d.Z);
        var yaw = horizontal < 1e-5f ? 0f : MathF.Atan2(d.X, d.Z) * 180f / MathF.PI;
        var pitch = MathF.Atan2(horizontal, d.Y) * 180f / MathF.PI;
        var centre = (from + to) / 2f;
        return new CropPart($"{Prefix}{name}", CropPartShape.Box, sizeX, d.Length() + sizeX * 0.5f, sizeZ, centre.X, centre.Y, centre.Z, pitch, yaw, 0f, role);
    }

    /// <summary>A box rising from <paramref name="from"/>, tilted <paramref name="tilt"/>° from upright towards its yaw.</summary>
    public CropPart Rising(string name, float size, float length, Vector3 from, float tilt, float yaw, string role)
    {
        var centre = from + PartBox.Rotate(new Vector3(0f, length / 2f, 0f), tilt, yaw, 0f);
        return new CropPart($"{Prefix}{name}", CropPartShape.Box, size, length, size, centre.X, centre.Y, centre.Z, tilt, yaw, 0f, role);
    }

    /// <summary>An angle in −180..180.</summary>
    public static float Signed(float degrees)
    {
        var wrapped = (degrees + 180f) % 360f;
        return (wrapped < 0f ? wrapped + 360f : wrapped) - 180f;
    }
}
