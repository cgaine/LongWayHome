using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Application.Art;

/// <summary>The farmer's animation clips (#103, ART_DIRECTION §6.1).</summary>
public enum FarmerClip
{
    Idle,
    Walk,
    Run,
    HoeSwing,
    Water,
    Plant,
    Harvest,
    Refill,
    Ship,
    Sleep,
    PassOut,
    AxeSwing,
    PickaxeSwing,
}

/// <summary>
/// One bone's rotation at <paramref name="Time"/> seconds into a clip, in degrees from the bone's rest (Euler YXZ, as
/// Godot's <c>RotationDegrees</c>).
/// </summary>
public sealed record FarmerKey(string Bone, float Time, float PitchDegrees, float YawDegrees, float RollDegrees);

/// <summary>The offset of <c>Hips</c> from its rest at <paramref name="Time"/> seconds into a clip, in metres.</summary>
public sealed record FarmerHipsKey(float Time, float Y, float Z);

/// <summary>One clip: its length, whether it loops, its bone keys and its <c>Hips</c> keys.</summary>
public sealed record FarmerClipSpec(
    FarmerClip Clip,
    float LengthSeconds,
    bool Loops,
    IReadOnlyList<FarmerKey> Keys,
    IReadOnlyList<FarmerHipsKey> Hips);

/// <summary>
/// The farmer's keyframe table (#103). Pure C#, no Godot: the Godot builder <c>FarmerAnimations</c> turns each clip
/// into an <c>Animation</c> with one rotation track per keyed bone and a position track for <c>Hips</c>.
/// <para>
/// <b>Conventions.</b> The farmer faces +Z with his own left on +X (<see cref="FarmerArt"/>), every bone's rest has no
/// rotation, so a key's angles are the bone's whole rotation (Euler YXZ, as Godot's <c>RotationDegrees</c>):
/// a positive pitch tips a bone's top forward, so it leans the torso forward and swings a hanging limb back (a
/// negative pitch lifts an arm or a leg forward); a positive yaw turns the front toward his left; a positive roll
/// lifts his left arm out and his right arm in. Between keys the builder interpolates linearly.
/// </para>
/// </summary>
public static class FarmerMoves
{
    private static readonly Dictionary<FarmerClip, FarmerClipSpec> Clips = new()
    {
        [FarmerClip.Idle] = Idle(),
        [FarmerClip.Walk] = Walk(),
        [FarmerClip.Run] = Run(),
        [FarmerClip.HoeSwing] = HoeSwing(),
        [FarmerClip.Water] = Water(),
        [FarmerClip.Plant] = Plant(),
        [FarmerClip.Harvest] = Harvest(),
        [FarmerClip.Refill] = Refill(),
        [FarmerClip.Ship] = Ship(),
        [FarmerClip.Sleep] = Sleep(),
        [FarmerClip.PassOut] = PassOut(),
    };

    /// <summary>The keyframes of <paramref name="clip"/>.</summary>
    public static FarmerClipSpec For(FarmerClip clip) =>
        Clips.TryGetValue(clip, out var spec)
            ? spec
            : throw new ArgumentOutOfRangeException(nameof(clip), clip, "Unknown farmer clip.");

    /// <summary>Slow breathing, the chest easing back 2°, and a small look to his left half way through.</summary>
    private static FarmerClipSpec Idle()
    {
        const float length = 2.4f;
        var keys = new ClipKeys();
        keys.Pose(0f, ("Chest", 0f, 0f, 0f), ("Head", 0f, 0f, 0f), ("LeftUpperArm", 0f, 0f, 0f), ("RightUpperArm", 0f, 0f, 0f));
        keys.Pose(0.9f, ("Head", 0f, 0f, 0f));
        keys.Pose(1.2f, ("Chest", -2f, 0f, 0f), ("Head", 2f, 10f, 0f), ("LeftUpperArm", 0f, 0f, 2f), ("RightUpperArm", 0f, 0f, -2f));
        keys.Pose(1.6f, ("Head", 2f, 10f, 0f));
        keys.Pose(2.0f, ("Head", 0f, 0f, 0f));
        keys.Pose(length, ("Chest", 0f, 0f, 0f), ("Head", 0f, 0f, 0f), ("LeftUpperArm", 0f, 0f, 0f), ("RightUpperArm", 0f, 0f, 0f));
        return new FarmerClipSpec(FarmerClip.Idle, length, true, keys.Keys, [new(0f, 0f, 0f), new(1.2f, -0.008f, 0f), new(length, 0f, 0f)]);
    }

    /// <summary>Legs swing ±25° from the hips, the arms ±20° the other way, the hips dip 0.03 m at each stride.</summary>
    private static FarmerClipSpec Walk() => Stride(FarmerClip.Walk, 0.8f, leg: 25f, knee: 30f, arm: 20f, elbow: -10f, lean: 3f, bob: 0.03f);

    /// <summary>Legs ±40°, arms ±35° bent at the elbow, the chest leaning 10° forward, the hips bobbing 0.05 m.</summary>
    private static FarmerClipSpec Run() => Stride(FarmerClip.Run, 0.55f, leg: 40f, knee: 55f, arm: 35f, elbow: -60f, lean: 10f, bob: 0.05f);

    /// <summary>
    /// A looping stride: the left leg forward at 0 (the right arm forward with it), the legs passing at a quarter,
    /// the right leg forward at half, the legs passing again at three quarters. The back leg's knee bends as it
    /// passes, and the hips sit lowest when the feet are furthest apart, so the feet stay near the ground.
    /// </summary>
    private static FarmerClipSpec Stride(FarmerClip clip, float length, float leg, float knee, float arm, float elbow, float lean, float bob)
    {
        var q = length / 4f;
        var keys = new ClipKeys();
        for (var i = 0; i <= 4; i++)
        {
            var t = i * q;
            // +1 left leg forward, -1 right leg forward, 0 passing.
            var phase = (i % 4) switch { 0 => 1f, 2 => -1f, _ => 0f };
            // The knee of the leg that just pushed off bends as it passes forward.
            var leftKnee = i % 4 == 3 ? knee : i % 4 == 0 ? knee * 0.2f : 0f;
            var rightKnee = i % 4 == 1 ? knee : i % 4 == 2 ? knee * 0.2f : 0f;
            keys.Pose(
                t,
                ("LeftUpperLeg", -leg * phase, 0f, 0f),
                ("RightUpperLeg", leg * phase, 0f, 0f),
                ("LeftLowerLeg", leftKnee, 0f, 0f),
                ("RightLowerLeg", rightKnee, 0f, 0f),
                ("LeftUpperArm", arm * phase, 0f, 0f),
                ("RightUpperArm", -arm * phase, 0f, 0f),
                ("LeftLowerArm", elbow, 0f, 0f),
                ("RightLowerArm", elbow, 0f, 0f),
                ("Chest", lean, -4f * phase, 0f),
                ("Spine", lean * 0.4f, 0f, 0f));
        }

        var hips = new List<FarmerHipsKey>();
        for (var i = 0; i <= 4; i++)
        {
            hips.Add(new FarmerHipsKey(i * q, i % 2 == 0 ? -bob : 0f, 0f));
        }

        return new FarmerClipSpec(clip, length, true, keys.Keys, hips);
    }

    /// <summary>
    /// Both arms raise the hoe over the right shoulder (0–0.25 s), swing it down in front onto the target tile
    /// (0.25–0.4 s), and recover. The right hand turns the hoe so its head is behind him when raised and on the ground
    /// in front at the strike.
    /// </summary>
    private static FarmerClipSpec HoeSwing()
    {
        const float length = 0.6f;
        var keys = new ClipKeys();
        keys.Rest(0f, "Chest", "Spine", "RightUpperArm", "LeftUpperArm", "RightLowerArm", "LeftLowerArm", "RightHand");
        keys.Pose(
            0.25f,
            ("Chest", -10f, -20f, 0f),
            ("Spine", -4f, -8f, 0f),
            ("RightUpperArm", -165f, 0f, -10f),
            ("LeftUpperArm", -150f, 0f, -25f),
            ("RightLowerArm", -20f, 0f, 0f),
            ("LeftLowerArm", -20f, 0f, 0f),
            ("RightHand", 90f, 0f, 0f));
        keys.Pose(
            0.4f,
            ("Chest", 28f, 0f, 0f),
            ("Spine", 10f, 0f, 0f),
            ("RightUpperArm", -55f, 0f, 0f),
            ("LeftUpperArm", -50f, 0f, -15f),
            ("RightLowerArm", -10f, 0f, 0f),
            ("LeftLowerArm", -10f, 0f, 0f),
            ("RightHand", 170f, 0f, 0f));
        keys.Pose(0.48f, ("Chest", 24f, 0f, 0f), ("RightHand", 160f, 0f, 0f), ("RightUpperArm", -55f, 0f, 0f), ("LeftUpperArm", -50f, 0f, -15f));
        keys.Pose(0.54f, ("RightHand", 80f, 0f, 0f));
        keys.Rest(length, "Chest", "Spine", "RightUpperArm", "LeftUpperArm", "RightLowerArm", "LeftLowerArm", "RightHand");
        return new FarmerClipSpec(FarmerClip.HoeSwing, length, false, keys.Keys, []);
    }

    /// <summary>The right arm lifts forward, the right hand tips the can 50° forward over the tile, and back.</summary>
    private static FarmerClipSpec Water()
    {
        const float length = 0.7f;
        var keys = new ClipKeys();
        keys.Rest(0f, "RightUpperArm", "RightLowerArm", "RightHand", "Chest");
        keys.Pose(0.18f, ("RightUpperArm", -60f, 0f, 5f), ("RightLowerArm", -10f, 0f, 0f), ("RightHand", 65f, 0f, 0f), ("Chest", 6f, 0f, 0f));
        keys.Pose(0.3f, ("RightHand", 115f, 0f, 0f));
        keys.Pose(0.5f, ("RightUpperArm", -60f, 0f, 5f), ("RightHand", 115f, 0f, 0f), ("Chest", 6f, 0f, 0f));
        keys.Pose(0.6f, ("RightHand", 65f, 0f, 0f));
        keys.Rest(length, "RightUpperArm", "RightLowerArm", "RightHand", "Chest");
        return new FarmerClipSpec(FarmerClip.Water, length, false, keys.Keys, []);
    }

    /// <summary>A crouch (hips down 0.2 m, knees bent), the right hand reaching to the ground in front, back up.</summary>
    private static FarmerClipSpec Plant()
    {
        const float length = 0.6f;
        var keys = new ClipKeys();
        string[] moving = ["LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftFoot", "RightFoot", "Spine", "Chest", "Head", "RightUpperArm", "RightLowerArm", "LeftUpperArm"];
        keys.Rest(0f, moving);
        keys.Pose(0.2f, Crouch(lean: 30f).Append(("RightUpperArm", -45f, 0f, 0f)).Append(("LeftUpperArm", -15f, 0f, 15f)).Append(("Head", -15f, 0f, 0f)).ToArray());
        keys.Pose(0.32f, ("RightUpperArm", -25f, 0f, 0f), ("RightLowerArm", -5f, 0f, 0f));
        keys.Pose(0.42f, Crouch(lean: 30f).Append(("RightUpperArm", -45f, 0f, 0f)).Append(("LeftUpperArm", -15f, 0f, 15f)).Append(("Head", -15f, 0f, 0f)).ToArray());
        keys.Rest(length, moving);
        return new FarmerClipSpec(FarmerClip.Plant, length, false, keys.Keys, [new(0f, 0f, 0f), new(0.2f, -0.2f, -0.04f), new(0.42f, -0.2f, -0.04f), new(length, 0f, 0f)]);
    }

    /// <summary>A crouch, both hands gripping at the ground in front, a pull up to chest height, and he stands.</summary>
    private static FarmerClipSpec Harvest()
    {
        const float length = 0.7f;
        var keys = new ClipKeys();
        string[] moving = ["LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftFoot", "RightFoot", "Spine", "Chest", "Head", "RightUpperArm", "RightLowerArm", "LeftUpperArm", "LeftLowerArm"];
        keys.Rest(0f, moving);
        keys.Pose(0.22f, Crouch(lean: 32f).Concat([("RightUpperArm", -40f, 0f, 10f), ("LeftUpperArm", -40f, 0f, -10f), ("Head", -15f, 0f, 0f)]).ToArray());
        keys.Pose(0.3f, ("RightUpperArm", -40f, 0f, 10f), ("LeftUpperArm", -40f, 0f, -10f));
        keys.Pose(
            0.5f,
            ("LeftUpperLeg", 0f, 0f, 0f), ("RightUpperLeg", 0f, 0f, 0f), ("LeftLowerLeg", 0f, 0f, 0f), ("RightLowerLeg", 0f, 0f, 0f),
            ("LeftFoot", 0f, 0f, 0f), ("RightFoot", 0f, 0f, 0f), ("Spine", 0f, 0f, 0f), ("Chest", -4f, 0f, 0f), ("Head", -6f, 0f, 0f),
            ("RightUpperArm", -55f, 0f, 15f), ("LeftUpperArm", -55f, 0f, -15f),
            ("RightLowerArm", -80f, 0f, 0f), ("LeftLowerArm", -80f, 0f, 0f));
        keys.Rest(length, moving);
        return new FarmerClipSpec(FarmerClip.Harvest, length, false, keys.Keys, [new(0f, 0f, 0f), new(0.22f, -0.2f, -0.04f), new(0.3f, -0.2f, -0.04f), new(0.5f, 0f, 0f), new(length, 0f, 0f)]);
    }

    /// <summary>He leans and dips the right hand (with the can) down and forward toward the water, and lifts it back.</summary>
    private static FarmerClipSpec Refill()
    {
        const float length = 0.8f;
        var keys = new ClipKeys();
        string[] moving = ["Spine", "Chest", "Head", "RightUpperArm", "RightHand", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg"];
        keys.Rest(0f, moving);
        var dip = new (string, float, float, float)[]
        {
            ("Spine", 15f, 0f, 0f), ("Chest", 30f, 0f, 0f), ("Head", -20f, 0f, 0f),
            ("RightUpperArm", -40f, 0f, 0f), ("RightHand", 30f, 0f, 0f),
            ("LeftUpperLeg", -20f, 0f, 0f), ("RightUpperLeg", -20f, 0f, 0f), ("LeftLowerLeg", 30f, 0f, 0f), ("RightLowerLeg", 30f, 0f, 0f),
        };
        keys.Pose(0.3f, dip);
        keys.Pose(0.4f, ("RightUpperArm", -30f, 0f, 0f), ("RightHand", 50f, 0f, 0f));
        keys.Pose(0.5f, dip);
        keys.Rest(length, moving);
        return new FarmerClipSpec(FarmerClip.Refill, length, false, keys.Keys, [new(0f, 0f, 0f), new(0.3f, -0.06f, 0f), new(0.5f, -0.06f, 0f), new(length, 0f, 0f)]);
    }

    /// <summary>Both arms lift forward and toss up and over, with a small hop of the shoulders.</summary>
    private static FarmerClipSpec Ship()
    {
        const float length = 0.6f;
        var keys = new ClipKeys();
        string[] moving = ["RightUpperArm", "LeftUpperArm", "RightLowerArm", "LeftLowerArm", "RightShoulder", "LeftShoulder", "Chest"];
        keys.Rest(0f, moving);
        keys.Pose(0.2f, ("RightUpperArm", -60f, 0f, 10f), ("LeftUpperArm", -60f, 0f, -10f), ("RightLowerArm", -40f, 0f, 0f), ("LeftLowerArm", -40f, 0f, 0f), ("Chest", 8f, 0f, 0f));
        keys.Pose(
            0.35f,
            ("RightUpperArm", -140f, 0f, 10f), ("LeftUpperArm", -140f, 0f, -10f), ("RightLowerArm", -10f, 0f, 0f), ("LeftLowerArm", -10f, 0f, 0f),
            ("RightShoulder", 0f, 0f, -12f), ("LeftShoulder", 0f, 0f, 12f), ("Chest", -8f, 0f, 0f));
        keys.Pose(0.45f, ("RightShoulder", 0f, 0f, 0f), ("LeftShoulder", 0f, 0f, 0f));
        keys.Rest(length, moving);
        return new FarmerClipSpec(FarmerClip.Ship, length, false, keys.Keys, [new(0f, 0f, 0f), new(0.35f, 0.02f, 0f), new(0.45f, 0f, 0f)]);
    }

    /// <summary>A yawn and a two-arm stretch (head back, arms up), then the shoulders droop.</summary>
    private static FarmerClipSpec Sleep()
    {
        const float length = 1.4f;
        var keys = new ClipKeys();
        string[] moving = ["Head", "Neck", "Chest", "RightUpperArm", "LeftUpperArm", "RightLowerArm", "LeftLowerArm", "RightShoulder", "LeftShoulder"];
        keys.Rest(0f, moving);
        var stretch = new (string, float, float, float)[]
        {
            ("Head", -25f, 0f, 0f), ("Neck", -8f, 0f, 0f), ("Chest", -8f, 0f, 0f),
            ("RightUpperArm", -165f, 0f, 15f), ("LeftUpperArm", -165f, 0f, -15f),
            ("RightLowerArm", -25f, 0f, 0f), ("LeftLowerArm", -25f, 0f, 0f),
            ("RightShoulder", 0f, 0f, -8f), ("LeftShoulder", 0f, 0f, 8f),
        };
        keys.Pose(0.5f, stretch);
        keys.Pose(0.8f, stretch);
        var droop = new (string, float, float, float)[]
        {
            ("Head", 18f, 0f, 0f), ("Neck", 5f, 0f, 0f), ("Chest", 12f, 0f, 0f),
            ("RightUpperArm", 4f, 0f, 2f), ("LeftUpperArm", 4f, 0f, -2f),
            ("RightLowerArm", 0f, 0f, 0f), ("LeftLowerArm", 0f, 0f, 0f),
            ("RightShoulder", 0f, 0f, 8f), ("LeftShoulder", 0f, 0f, -8f),
        };
        keys.Pose(1.15f, droop);
        keys.Pose(length, droop);
        return new FarmerClipSpec(FarmerClip.Sleep, length, false, keys.Keys, [new(0f, 0f, 0f), new(0.5f, 0.01f, 0f), new(0.8f, 0.01f, 0f), new(1.15f, -0.02f, 0f), new(length, -0.02f, 0f)]);
    }

    /// <summary>
    /// A wobble (the chest rolls ±8°), the knees buckle (the hips drop), and he tips forward onto the ground (hips at
    /// about y 0.2 m, the body pitched forward about 80°), holding the last pose.
    /// </summary>
    private static FarmerClipSpec PassOut()
    {
        const float length = 1.4f;
        var keys = new ClipKeys();
        string[] moving = ["Hips", "Spine", "Chest", "Head", "LeftUpperLeg", "RightUpperLeg", "LeftLowerLeg", "RightLowerLeg", "LeftUpperArm", "RightUpperArm", "LeftLowerArm", "RightLowerArm"];
        keys.Rest(0f, moving);
        keys.Pose(0.15f, ("Chest", 0f, 0f, 8f), ("Head", 0f, 0f, 6f));
        keys.Pose(0.35f, ("Chest", 0f, 0f, -8f), ("Head", 0f, 0f, -6f));
        keys.Pose(
            0.5f,
            ("Hips", 0f, 0f, 0f), ("Spine", 0f, 0f, 0f), ("Chest", 4f, 0f, 4f), ("Head", 10f, 0f, 3f),
            ("LeftUpperLeg", 0f, 0f, 0f), ("RightUpperLeg", 0f, 0f, 0f), ("LeftLowerLeg", 0f, 0f, 0f), ("RightLowerLeg", 0f, 0f, 0f),
            ("LeftUpperArm", 0f, 0f, 0f), ("RightUpperArm", 0f, 0f, 0f));
        keys.Pose(
            0.8f,
            ("Hips", 15f, 0f, 0f), ("Spine", 5f, 0f, 0f), ("Chest", 10f, 0f, 0f), ("Head", 15f, 0f, 0f),
            ("LeftUpperLeg", -55f, 0f, 0f), ("RightUpperLeg", -55f, 0f, 0f), ("LeftLowerLeg", 85f, 0f, 0f), ("RightLowerLeg", 85f, 0f, 0f),
            ("LeftUpperArm", -20f, 0f, 10f), ("RightUpperArm", -20f, 0f, -10f));
        var down = new (string, float, float, float)[]
        {
            ("Hips", 70f, 0f, 0f), ("Spine", 5f, 0f, 0f), ("Chest", 5f, 0f, 0f), ("Head", -10f, 25f, 0f),
            ("LeftUpperLeg", 15f, 0f, 0f), ("RightUpperLeg", 15f, 0f, 0f), ("LeftLowerLeg", 10f, 0f, 0f), ("RightLowerLeg", 10f, 0f, 0f),
            ("LeftUpperArm", -150f, 0f, 30f), ("RightUpperArm", -150f, 0f, -30f), ("LeftLowerArm", 0f, 0f, 0f), ("RightLowerArm", 0f, 0f, 0f),
        };
        keys.Pose(1.15f, down);
        keys.Pose(length, down);
        return new FarmerClipSpec(
            FarmerClip.PassOut,
            length,
            false,
            keys.Keys,
            [new(0f, 0f, 0f), new(0.5f, -0.02f, 0f), new(0.8f, -0.22f, 0f), new(1.15f, -0.35f, 0.1f), new(length, -0.35f, 0.1f)]);
    }

    /// <summary>The legs of a crouch with the hips 0.2 m down: thighs forward, shins back, feet flat; and a forward lean.</summary>
    private static IEnumerable<(string, float, float, float)> Crouch(float lean) =>
    [
        ("LeftUpperLeg", -80f, 0f, 0f), ("RightUpperLeg", -80f, 0f, 0f),
        ("LeftLowerLeg", 120f, 0f, 0f), ("RightLowerLeg", 120f, 0f, 0f),
        ("LeftFoot", -40f, 0f, 0f), ("RightFoot", -40f, 0f, 0f),
        ("Spine", lean * 0.4f, 0f, 0f), ("Chest", lean * 0.6f, 0f, 0f),
    ];

    /// <summary>Collects a clip's keys pose by pose.</summary>
    private sealed class ClipKeys
    {
        private readonly List<FarmerKey> _keys = new();

        public IReadOnlyList<FarmerKey> Keys => _keys;

        /// <summary>Keys each named bone at <paramref name="time"/> to (pitch, yaw, roll) degrees.</summary>
        public void Pose(float time, params (string Bone, float Pitch, float Yaw, float Roll)[] bones)
        {
            foreach (var (bone, pitch, yaw, roll) in bones)
            {
                _keys.Add(new FarmerKey(bone, time, pitch, yaw, roll));
            }
        }

        /// <summary>Keys each named bone at its rest at <paramref name="time"/>.</summary>
        public void Rest(float time, params string[] bones)
        {
            foreach (var bone in bones)
            {
                _keys.Add(new FarmerKey(bone, time, 0f, 0f, 0f));
            }
        }
    }
}

/// <summary>
/// Which clip the farmer plays, from what he is doing, and when the day-end clips may play (#103). Pure presentation
/// arithmetic on the tuning; no rule: the session alone decides what an action does and when the Day ends.
/// </summary>
public static class FarmerAnimationState
{
    /// <summary>Below this many tiles per second he stands still.</summary>
    public const float IdleBelowTilesPerSecond = 0.1f;

    /// <summary>The slowest the walk cycle plays, as a fraction of its speed, for a light analog tilt.</summary>
    public const float MinWalkPlaybackScale = 0.4f;

    /// <summary>
    /// <see cref="FarmerClip.Idle"/> below 0.1 tiles per second; <see cref="FarmerClip.Run"/> when run is held and he
    /// moves faster than the walk speed; otherwise <see cref="FarmerClip.Walk"/> (run held with a slow analog tilt
    /// still walks).
    /// </summary>
    public static FarmerClip Locomotion(float horizontalSpeed, float walkSpeed, bool runHeld)
    {
        if (horizontalSpeed < IdleBelowTilesPerSecond)
        {
            return FarmerClip.Idle;
        }

        return runHeld && horizontalSpeed > walkSpeed ? FarmerClip.Run : FarmerClip.Walk;
    }

    /// <summary>The walk cycle's playback speed: speed ÷ walk speed, clamped to 0.4..1, so the feet don't slide.</summary>
    public static float WalkPlaybackScale(float horizontalSpeed, float walkSpeed)
    {
        if (!(walkSpeed > 0f))
        {
            return 1f;
        }

        return Math.Clamp(horizontalSpeed / walkSpeed, MinWalkPlaybackScale, 1f);
    }

    /// <summary>The clip a successful Use plays: till swings the hoe; water, plant and harvest have their own.</summary>
    public static FarmerClip? ForUse(UseActionKind? action) => action switch
    {
        UseActionKind.Till => FarmerClip.HoeSwing,
        UseActionKind.Water => FarmerClip.Water,
        UseActionKind.Plant => FarmerClip.Plant,
        UseActionKind.Harvest => FarmerClip.Harvest,
        _ => null,
    };

    /// <summary>
    /// Real seconds of play from <paramref name="clockMinutes"/> to the day end: (DayEndMinutes − clock) ×
    /// DayLengthRealSeconds ÷ (DayEndMinutes − DayStartMinutes), never below 0.
    /// </summary>
    public static float RealSecondsUntilDayEnd(int clockMinutes, GameTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        var minutesLeft = tuning.DayEndMinutes - clockMinutes;
        if (minutesLeft <= 0)
        {
            return 0f;
        }

        return (float)((double)minutesLeft * tuning.DayLengthRealSeconds / (tuning.DayEndMinutes - tuning.DayStartMinutes));
    }

    /// <summary>
    /// Whether a sleep at <paramref name="clockMinutes"/> plays the <see cref="FarmerClip.Sleep"/> clip before ending
    /// the Day (BR-G3). The session keeps a part-minute the clock doesn't show, so the true clock may be up to a minute
    /// ahead: the clip plays only if it ends before 02:00 even from the start of the next minute. Otherwise the Day
    /// ends by Sleep at once, so a press before 02:00 is never turned into a pass out.
    /// </summary>
    public static bool SleepPlaysClip(int clockMinutes, GameTuning tuning) =>
        RealSecondsUntilDayEnd(clockMinutes + 1, tuning) > FarmerMoves.For(FarmerClip.Sleep).LengthSeconds;

    /// <summary>
    /// Whether the <see cref="FarmerClip.PassOut"/> clip is due at <paramref name="clockMinutes"/>: no more real time
    /// is left before the day end than the clip lasts. The session still ends the Day at 02:00 (BR-G3).
    /// </summary>
    public static bool PassOutDue(int clockMinutes, GameTuning tuning) =>
        RealSecondsUntilDayEnd(clockMinutes, tuning) <= FarmerMoves.For(FarmerClip.PassOut).LengthSeconds;

    /// <summary>
    /// Whether the <see cref="FarmerClip.PassOut"/> clip should start now, given the real seconds of play since the
    /// clock last showed a new minute: when the time left, less the part-minute already played (taken as at most one
    /// game minute), is no more than the clip lasts. True whenever <see cref="PassOutDue"/> is, and up to a game minute
    /// sooner, so the clip ends about when the clock reaches 02:00 rather than being cut short by the day end.
    /// </summary>
    public static bool PassOutStarts(int clockMinutes, float secondsIntoMinute, GameTuning tuning)
    {
        var left = RealSecondsUntilDayEnd(clockMinutes, tuning);
        var oneMinute = RealSecondsUntilDayEnd(tuning.DayEndMinutes - 1, tuning);
        var intoMinute = Math.Clamp(secondsIntoMinute, 0f, oneMinute);
        return left - intoMinute <= FarmerMoves.For(FarmerClip.PassOut).LengthSeconds;
    }
}
