namespace LongWayHome.Application.Art;

/// <summary>One placed scenery piece in an area: its ground position (metres), yaw (degrees) and uniform scale.</summary>
public readonly record struct AreaProp(float X, float Z, float YawDegrees, float Scale);

/// <summary>
/// The seeded scatter the area layouts share (#136): props drawn uniformly over a rectangle, each kept only where the
/// layout says the ground is clear and at least a spacing from every prop already placed. A prop that finds no place in
/// <see cref="MaxAttempts"/> tries is skipped, so it always ends. The same seed gives the same props. Pure C#, no Godot.
/// </summary>
public static class AreaScatter
{
    /// <summary>How many positions are tried for one prop before it is skipped.</summary>
    public const int MaxAttempts = 200;

    /// <summary>
    /// Up to <paramref name="count"/> props in x <paramref name="minX"/>..<paramref name="maxX"/>, z
    /// <paramref name="minZ"/>..<paramref name="maxZ"/>, each where <paramref name="clear"/> holds and at least
    /// <paramref name="spacing"/> from the others, scaled <paramref name="minScale"/>..<paramref name="maxScale"/>.
    /// </summary>
    public static IReadOnlyList<AreaProp> Place(
        int seed,
        int count,
        (float MinX, float MaxX, float MinZ, float MaxZ) bounds,
        float spacing,
        (float Min, float Max) scale,
        Func<float, float, bool> clear)
    {
        ArgumentNullException.ThrowIfNull(clear);
        var random = new Random(seed);
        var props = new List<AreaProp>();
        for (var i = 0; i < count; i++)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var x = Lerp(bounds.MinX, bounds.MaxX, random.NextSingle());
                var z = Lerp(bounds.MinZ, bounds.MaxZ, random.NextSingle());
                if (!clear(x, z) || props.Exists(p => Distance(p.X, p.Z, x, z) < spacing))
                {
                    continue;
                }

                props.Add(new AreaProp(
                    x, z, random.NextSingle() * 360f, Lerp(scale.Min, scale.Max, random.NextSingle())));
                break;
            }
        }

        return props.AsReadOnly();
    }

    /// <summary>The ground distance between two points.</summary>
    public static float Distance(float x1, float z1, float x2, float z2) =>
        MathF.Sqrt(((x1 - x2) * (x1 - x2)) + ((z1 - z2) * (z1 - z2)));

    private static float Lerp(float from, float to, float t) => from + ((to - from) * t);
}
