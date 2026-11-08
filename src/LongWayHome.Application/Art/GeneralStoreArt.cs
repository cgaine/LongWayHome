using System.Numerics;

namespace LongWayHome.Application.Art;

/// <summary>The general store's rebuild stages (design doc, First building: General store; ART_DIRECTION §7.3).</summary>
public enum StoreStage
{
    /// <summary>Stage 1, open from day one: one living corner (a lit shop window, a small awning, a few crates) on an otherwise wilted building.</summary>
    MaggiesCorner,

    /// <summary>Stage 2: the whole building alive again.</summary>
    Reopened,

    /// <summary>Stage 3: a grown storeroom on the side, with sacks.</summary>
    Storeroom,

    /// <summary>Stage 4: a second awning, more crates and string lights.</summary>
    ExpandedShop,
}

/// <summary>
/// Maggie's general store, the first building the player rebuilds, in the round, grown style of the concept art's
/// "Round buildings" page ("A wider drum with a ring of leaf awning around the front, produce crates and a hanging sprout
/// sign") and ART_DIRECTION §7.3 ("a big round-topped shop window"). Pure C#, no Godot: the sandbox's builder merges these
/// parts into one mesh under the shared palette material. The art sandbox shows every stage; the town (#136) shows MaggiesCorner.
/// <para>
/// <b>Shape.</b> Building-local metres, origin at the drum's centre on the ground, the door facing +Z (towards the
/// square). The drum is 6.4 m across with 3 m walls; the cone's eaves reach 4.2 m out and its apex is at 6.6 m. The
/// ember door is at the front, the big round-topped shop window to its left (−X), a small pointed window to its right
/// and a round sprout sign hangs from a bracket beside the door. Awnings meet the wall at 2.5 m, above the windows. Four living ribs climb the wall at ±70° and ±135°.
/// </para>
/// <para>
/// <b>Stages.</b> Maggie's corner: the staves, rib and roof leaves round the shop window (−65° to −5°) alive, a short
/// awning over the window and two crates under it, the shop window lit at night; everything else run-down (grey staves
/// with missing pieces, a wilted roof with gaps, dead twigs at the crown, a boarded right window, weeds). Reopened: all alive, the awning ringing the front
/// (−80° to 80°) with four crates. Storeroom: as Reopened, plus a smaller grown drum joined on the east side with its
/// own door and sacks of seed by it. Expanded shop: as Storeroom, plus a second awning round the west side with more
/// crates, and string lights sagging along the front awning's edge that glow at night.
/// </para>
/// Parts are named by group: the store's own parts carry no prefix (<c>Stave*</c>, <c>Leaf*</c>, <c>Rib*</c>,
/// <c>Door*</c>, <c>ShopWindow*</c>, <c>SideWindow*</c>, <c>Awning*</c>, <c>Crate*</c>, <c>Sign*</c>, <c>Light*</c>,
/// <c>Weed*</c>, ...); the storeroom's start <c>Storeroom</c>.
/// </summary>
public static class GeneralStoreArt
{
    /// <summary>The drum's outer radius: 6.4 m across.</summary>
    public const float WallRadius = 3.2f;

    /// <summary>The roof cone's eave radius.</summary>
    public const float EaveRadius = 4.2f;

    /// <summary>The roof cone's apex height.</summary>
    public const float ApexY = 6.6f;

    private const float WallTop = 3.0f;

    /// <summary>Where the awnings meet the wall: above the shop window's round top.</summary>
    private const float AwningHeight = 2.5f;
    private const int Staves = 30;

    private const float ShopWindowAngle = -32f;
    private const float ShopWindowWidth = 1.5f;
    private const float ShopWindowSill = 0.6f;
    private const float ShopWindowShoulder = 1.5f;

    private const float SideWindowAngle = 34f;
    private const float SideWindowWidth = 0.68f;
    private const float SideWindowSill = 0.85f;
    private const float SideWindowShoulder = 1.5f;

    /// <summary>The angles of Maggie's corner: the shop window's side of the front.</summary>
    private const float CornerFrom = -65f;
    private const float CornerTo = -5f;

    private static readonly RoundBuilding Store = new("", Vector3.Zero, WallRadius, WallTop, EaveRadius, ApexY);

    /// <summary>The storeroom: a smaller drum grown onto the east side.</summary>
    private static readonly RoundBuilding Shed = new("Storeroom", new Vector3(4.35f, 0f, -0.6f), 1.7f, 2.3f, 2.3f, 4.4f);

    private static readonly Dictionary<StoreStage, IReadOnlyList<CropPart>> Cache = new();
    private static readonly Dictionary<StoreStage, IReadOnlyList<CropPart>> LightCache = new();

    /// <summary>The store at <paramref name="stage"/>. The same list for the same stage.</summary>
    public static IReadOnlyList<CropPart> For(StoreStage stage)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(stage, out var parts))
            {
                parts = Build(stage);
                Cache[stage] = parts;
            }

            return parts;
        }
    }

    /// <summary>
    /// The warm window panes lit after dark at <paramref name="stage"/>, in <c>lantern</c> just in front of the glass:
    /// only the shop window in Maggie's corner, both windows (and the storeroom's) after. Shown only at night.
    /// </summary>
    public static IReadOnlyList<CropPart> NightLights(StoreStage stage)
    {
        lock (LightCache)
        {
            if (!LightCache.TryGetValue(stage, out var parts))
            {
                var lights = new List<CropPart>();
                Store.WindowLight(lights, "NightShopWindow", ShopWindowAngle, ShopWindowWidth, ShopWindowSill, ShopWindowShoulder, roundTop: true);
                if (stage != StoreStage.MaggiesCorner)
                {
                    Store.WindowLight(lights, "NightSideWindow", SideWindowAngle, SideWindowWidth, SideWindowSill, SideWindowShoulder, roundTop: false);
                }

                parts = lights;
                LightCache[stage] = parts;
            }

            return parts;
        }
    }

    private static List<CropPart> Build(StoreStage stage)
    {
        var parts = new List<CropPart>();
        var corner = stage == StoreStage.MaggiesCorner;
        Func<float, bool> alive = corner ? angle => angle >= CornerFrom && angle <= CornerTo : _ => true;

        (int, bool)[] missing = corner ? [(7, false), (12, true), (20, false), (24, true)] : [];
        Store.Drum(parts, Staves, alive, missing);
        float[] ribs = [-70f, 70f, 135f, -135f];
        for (var i = 0; i < ribs.Length; i++)
        {
            Store.Rib(parts, i, ribs[i], alive(ribs[i]) || (corner && i == 0));
        }

        (int, float, float)[] gaps = [(1, 20f, 48f), (2, 110f, 140f), (2, -150f, -125f), (3, 60f, 95f), (4, -110f, -80f)];
        Store.Roof(parts, alive, gaps, crownAlive: !corner);

        // The ember door: the shop is open from day one, so it is always the fixed, warm one.
        Store.Door(parts, "Door", 0f, 0.55f, 1.85f, "ember", spiral: true);
        Store.Window(parts, "ShopWindow", ShopWindowAngle, ShopWindowWidth, ShopWindowSill, ShopWindowShoulder, "wrist-screen", roundTop: true, mullions: 2);
        Store.Window(parts, "SideWindow", SideWindowAngle, SideWindowWidth, SideWindowSill, SideWindowShoulder, corner ? "glass-dull" : "wrist-screen", roundTop: false, mullions: 1);
        if (corner)
        {
            parts.Add(Store.Box("SideWindowBoard0", 0.95f, 0.14f, 0.03f, Store.OnWall(SideWindowAngle, 0f, 1.25f, 0.1f), "wall-grey", yaw: SideWindowAngle, roll: 26f));
            parts.Add(Store.Box("SideWindowBoard1", 0.95f, 0.14f, 0.03f, Store.OnWall(SideWindowAngle, 0f, 1.27f, 0.13f), "timber-side", yaw: SideWindowAngle, roll: -22f));
        }

        SproutSign(parts);

        if (corner)
        {
            Store.Awning(parts, "Awning", -58f, -8f, AwningHeight);
            Crates(parts, "Crate", [(-48f, "ember"), (-20f, "dusktuber")]);
            Weeds(parts);
        }
        else
        {
            Store.Awning(parts, "Awning", -80f, -12f, AwningHeight);
            Store.Awning(parts, "AwningEast", 22f, 80f, AwningHeight);
            Crates(parts, "Crate", [(-55f, "ember"), (-28f, "dusktuber"), (28f, "sunbell-gold"), (55f, "grass-lit")]);
        }

        if (stage is StoreStage.Storeroom or StoreStage.ExpandedShop)
        {
            Storeroom(parts);
        }

        if (stage == StoreStage.ExpandedShop)
        {
            Store.Awning(parts, "AwningWest", -150f, -95f, AwningHeight);
            Crates(parts, "CrateWest", [(-140f, "moonmelon"), (-120f, "starcorn"), (-103f, "ember")]);
            StringLights(parts);
        }

        return parts;
    }

    /// <summary>The round sprout sign hanging from a bracket to the right of the door.</summary>
    private static void SproutSign(List<CropPart> parts)
    {
        const float angle = 17f;
        var wall = Store.OnWall(angle, 0f, 2.3f, 0.02f);
        var tip = Store.OnWall(angle, 0f, 2.3f, 0.62f);
        parts.Add(Store.Beam("SignBracket", 0.06f, 0.06f, wall, tip, "trunk"));
        var centre = Store.OnWall(angle, 0f, 1.83f, 0.55f);
        foreach (var (name, along) in new[] { ("SignChainA", -0.2f), ("SignChainB", 0.2f) })
        {
            var top = Store.OnWall(angle, along * 0.1f, 2.3f, 0.55f + along);
            var bottom = Store.OnWall(angle, along * 0.1f, 2.05f, 0.55f + along);
            parts.Add(Store.Beam(name, 0.02f, 0.02f, top, bottom, "rock"));
        }

        // The disc faces along the wall's tangent, so it reads from both sides of the path to the door.
        var yaw = angle + 90f;
        parts.Add(new CropPart("SignRim", CropPartShape.Cylinder, 0.78f, 0.06f, 0.78f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "timber-trim", Sides: 16));
        parts.Add(new CropPart("SignFace", CropPartShape.Cylinder, 0.66f, 0.08f, 0.66f, centre.X, centre.Y, centre.Z, 90f, yaw, 0f, "wood-cut", Sides: 16));
        var face = PartBox.Rotate(new Vector3(0f, 0f, 1f), 0f, yaw, 0f);
        foreach (var side in new[] { 1f, -1f })
        {
            var name = side > 0 ? "A" : "B";
            var stem = centre + face * (0.045f * side);
            parts.Add(new CropPart($"SignSprout{name}", CropPartShape.Box, 0.04f, 0.26f, 0.012f, stem.X, stem.Y - 0.06f, stem.Z, 0f, yaw, 0f, "foliage"));
            foreach (var (leaf, offset, roll) in new[] { ("L", -0.09f, 40f), ("R", 0.09f, -40f) })
            {
                var at = stem + PartBox.Rotate(new Vector3(offset, 0.1f, 0f), 0f, yaw, 0f);
                parts.Add(new CropPart($"SignLeaf{name}{leaf}", CropPartShape.Sphere, 0.16f, 0.07f, 0.03f, at.X, at.Y, at.Z, 90f, yaw, roll, "grass-lit"));
            }
        }
    }

    /// <summary>Produce crates on the ground under an awning, each with three of the crop on top.</summary>
    private static void Crates(List<CropPart> parts, string name, (float Angle, string Produce)[] crates)
    {
        for (var i = 0; i < crates.Length; i++)
        {
            var (angle, produce) = crates[i];
            var at = Store.Radial(angle, WallRadius + 0.55f, 0.22f);
            parts.Add(Store.Box($"{name}{i}", 0.62f, 0.44f, 0.46f, at, "door-faded", yaw: angle));
            parts.Add(Store.Box($"{name}{i}Slat", 0.64f, 0.06f, 0.48f, at + new Vector3(0f, 0.04f, 0f), "timber-side", yaw: angle));
            for (var p = 0; p < 3; p++)
            {
                var item = at + PartBox.Rotate(new Vector3(-0.18f + 0.18f * p, 0.29f + 0.02f * (p % 2), 0.02f * (p - 1)), 0f, angle, 0f);
                parts.Add(Store.Sphere($"{name}{i}Produce{p}", 0.2f, 0.18f, item, produce));
            }
        }
    }

    /// <summary>Weeds round the run-down drum, away from Maggie's corner and the door.</summary>
    private static void Weeds(List<CropPart> parts)
    {
        float[] weeds = [-160f, -120f, -90f, 30f, 60f, 95f, 140f, 175f];
        for (var i = 0; i < weeds.Length; i++)
        {
            var at = Store.Radial(weeds[i], WallRadius + 0.2f, 0f);
            var height = 0.2f + 0.05f * (i % 3);
            parts.Add(new CropPart($"Weed{i}A", CropPartShape.Prism, 0.18f, height, 0.02f, at.X, height / 2f + 0.015f, at.Z, 0f, weeds[i] + 15f * i, 8f, i % 2 == 0 ? "foliage" : "grass-shade"));
            parts.Add(new CropPart($"Weed{i}B", CropPartShape.Prism, 0.16f, height * 0.8f, 0.02f, at.X + 0.03f, height * 0.4f + 0.015f, at.Z, 0f, weeds[i] + 15f * i + 90f, -8f, i % 2 == 0 ? "grass-shade" : "foliage"));
        }
    }

    /// <summary>The storeroom: a smaller living drum on the east side, with its own door facing the front and sacks by it.</summary>
    private static void Storeroom(List<CropPart> parts)
    {
        Shed.Drum(parts, 18, _ => true, []);
        Shed.Rib(parts, 0, 60f, alive: true);
        Shed.Rib(parts, 1, 150f, alive: true);
        Shed.Roof(parts, _ => true, [], crownAlive: true);
        Shed.Door(parts, "Door", 20f, 0.42f, 1.55f, "timber-side", spiral: false);
        (float Angle, float Out, float Size)[] sacks = [(45f, 0.4f, 0.42f), (55f, 0.5f, 0.36f), (40f, 0.75f, 0.32f)];
        for (var i = 0; i < sacks.Length; i++)
        {
            var (angle, outward, size) = sacks[i];
            var at = Shed.Radial(angle, Shed.WallRadius + outward, size * 0.42f);
            parts.Add(Shed.Sphere($"Sack{i}", size, size * 0.9f, at, "seed-pouch"));
            parts.Add(Shed.Cylinder($"Sack{i}Tie", size * 0.35f, 0.05f, size * 0.35f, at + new Vector3(0f, size * 0.45f, 0f), "timber-side"));
            parts.Add(Shed.Sphere($"Sack{i}Patch", size * 0.3f, size * 0.3f, at + PartBox.Rotate(new Vector3(0f, 0.02f, size * 0.42f), 0f, angle, 0f), "timber"));
        }
    }

    /// <summary>String lights sagging between the front awning's struts, every bulb glowing at night.</summary>
    private static void StringLights(List<CropPart> parts)
    {
        float[] posts = [-78f, -40f, 0f, 40f, 78f];
        const float radius = WallRadius + 0.82f;
        const float height = 2.12f;
        var bulb = 0;
        for (var span = 0; span < posts.Length - 1; span++)
        {
            const int steps = 6;
            Vector3? previous = null;
            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var angle = posts[span] + (posts[span + 1] - posts[span]) * t;
                var sag = 0.28f * 4f * t * (1f - t);
                var at = Store.Radial(angle, radius, height - sag);
                if (previous is { } from)
                {
                    parts.Add(Store.Beam($"LightWire{span}_{i}", 0.012f, 0.012f, from, at, "boots"));
                }

                if (i > 0 && i < steps)
                {
                    parts.Add(Store.Sphere($"Light{bulb++}", 0.09f, 0.11f, at - new Vector3(0f, 0.06f, 0f), "lantern") with { Glow = 1f });
                }

                previous = at;
            }
        }
    }
}

/// <summary>
/// A building's rebuild sign (ART_DIRECTION §7.5 and the concept art's town square props): a grown board of mauve timber
/// on two living trunk posts, a trim of leaves along its top, and three pips for wood, stone and gold that fill with the
/// material's colour as each is delivered (empty pips are cream). At night a thin amber outline keeps it findable
/// (<see cref="NightOutline"/>). It stands about 1.6 m tall and 1.2 m wide, origin at its centre on the ground, facing
/// +Z. Pure C#, no Godot.
/// </summary>
public static class RebuildSignArt
{
    private const float BoardY = 1.05f;
    private const float BoardWidth = 1.2f;
    private const float BoardHeight = 0.62f;

    /// <summary>The sign with the given materials delivered. Parts named <c>Post*</c>, <c>Board*</c>, <c>Trim*</c>, <c>Pip*</c>.</summary>
    public static IReadOnlyList<CropPart> Parts(bool wood, bool stone, bool gold)
    {
        var parts = new List<CropPart>();
        foreach (var (name, x) in new[] { ("West", -0.5f), ("East", 0.5f) })
        {
            parts.Add(new CropPart($"Post{name}", CropPartShape.Cylinder, 0.13f, 1.45f, 0.1f, x, 0.725f, -0.02f, 0f, 20f, 0f, "trunk"));
            parts.Add(new CropPart($"Post{name}Bud", CropPartShape.Sphere, 0.16f, 0.12f, 0.16f, x, 1.48f, -0.02f, 0f, 0f, 0f, "roof-tips"));
        }

        parts.Add(new CropPart("Board", CropPartShape.Box, BoardWidth, BoardHeight, 0.07f, 0f, BoardY, 0.04f, 0f, 0f, 0f, "timber"));
        parts.Add(new CropPart("BoardEdge", CropPartShape.Box, BoardWidth + 0.06f, BoardHeight + 0.06f, 0.05f, 0f, BoardY, 0.02f, 0f, 0f, 0f, "timber-trim"));
        parts.Add(new CropPart("BoardGrain", CropPartShape.Box, 0.8f, 0.016f, 0.008f, 0f, BoardY - 0.2f, 0.078f, 0f, 0f, 2f, "timber-grain"));

        // The leaf trim along the top: overlapping leaves fanning across.
        for (var i = 0; i < 5; i++)
        {
            var x = -0.5f + 0.25f * i;
            var lift = 0.06f * (1f - MathF.Abs(i - 2) / 2f);
            parts.Add(new CropPart($"Trim{i}", CropPartShape.Sphere, 0.34f, 0.07f, 0.14f, x, BoardY + BoardHeight / 2f + 0.04f + lift, 0.05f,
                90f, 0f, (i - 2) * -14f, i % 2 == 0 ? "foliage" : "roof-tips", Sides: 10));
        }

        (string Name, bool Filled, string Role)[] pips = [("Wood", wood, "soil-tilled"), ("Stone", stone, "stone"), ("Gold", gold, "sunbell-gold")];
        for (var i = 0; i < pips.Length; i++)
        {
            var (name, filled, role) = pips[i];
            var x = -0.36f + 0.36f * i;
            parts.Add(new CropPart($"Pip{name}Rim", CropPartShape.Cylinder, 0.24f, 0.03f, 0.24f, x, BoardY, 0.085f, 90f, 0f, 0f, "timber-trim", Sides: 16));
            parts.Add(new CropPart($"Pip{name}", CropPartShape.Cylinder, 0.19f, 0.04f, 0.19f, x, BoardY, 0.095f, 90f, 0f, 0f, filled ? role : "target-highlight", Sides: 16));
        }

        return parts;
    }

    /// <summary>The thin amber outline round the board, glowing: shown only at night.</summary>
    public static IReadOnlyList<CropPart> NightOutline { get; } =
    [
        new("OutlineTop", CropPartShape.Box, BoardWidth + 0.14f, 0.035f, 0.02f, 0f, BoardY + BoardHeight / 2f + 0.05f, 0.06f, 0f, 0f, 0f, "lantern", Glow: 1f),
        new("OutlineBottom", CropPartShape.Box, BoardWidth + 0.14f, 0.035f, 0.02f, 0f, BoardY - BoardHeight / 2f - 0.05f, 0.06f, 0f, 0f, 0f, "lantern", Glow: 1f),
        new("OutlineWest", CropPartShape.Box, 0.035f, BoardHeight + 0.14f, 0.02f, -BoardWidth / 2f - 0.05f, BoardY, 0.06f, 0f, 0f, 0f, "lantern", Glow: 1f),
        new("OutlineEast", CropPartShape.Box, 0.035f, BoardHeight + 0.14f, 0.02f, BoardWidth / 2f + 0.05f, BoardY, 0.06f, 0f, 0f, 0f, "lantern", Glow: 1f),
    ];
}
