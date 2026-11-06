namespace LongWayHome.Application.Games;

/// <summary>
/// The camera's turn, zoom and zoom-linked tilt around the farmer (REQ-52). Pure maths: the Godot camera reads its input actions,
/// updates this value each physics frame and places itself at <see cref="Offset"/> from the point it follows.
/// Holds no rules and is not saved.
/// </summary>
/// <param name="YawDegrees">
/// The compass heading the camera looks along, clockwise seen from above: 0 = looking north (-Z), 90 = looking
/// east (+X). Always in [0, 360).
/// </param>
/// <param name="Zoom">0 at <see cref="NearestDistance"/>, 1 at <see cref="FarthestDistance"/>; always in [0, 1].</param>
public readonly record struct CameraView(float YawDegrees, float Zoom)
{
    /// <summary>How fast a full stick or held key turns the view.</summary>
    public const float RotateDegreesPerSecond = 120f;

    /// <summary>How fast a full stick or held key zooms, as a fraction of the zoom range per second.</summary>
    public const float ZoomPerSecond = 0.6f;

    /// <summary>The camera's distance from the farmer at zoom 0.</summary>
    public const float NearestDistance = 7f;

    /// <summary>The camera's distance from the farmer at zoom 1.</summary>
    public const float FarthestDistance = 18.5f;

    /// <summary>How far the camera looks down from horizontal at zoom 0: lower, nearer the horizon.</summary>
    public const float NearestPitchDegrees = 30f;

    /// <summary>How far the camera looks down from horizontal at zoom 1: steep, but never fully top-down.</summary>
    public const float FarthestPitchDegrees = 60f;

    /// <summary>Looking north from halfway between the limits: the view the game has always had.</summary>
    public static CameraView Default { get; } = new(0f, 0.5f);

    /// <summary>The camera's distance from the point it follows, from 7 at zoom 0 to 18.5 at zoom 1.</summary>
    public float Distance => NearestDistance + (FarthestDistance - NearestDistance) * Zoom;

    /// <summary>
    /// How far the camera looks down from horizontal at the farmer, rising linearly with zoom from 30° (nearest) to
    /// 60° (farthest); 45° at the default zoom. Instant with zoom, so pitch and distance always correspond.
    /// </summary>
    public float PitchDegrees => NearestPitchDegrees + (FarthestPitchDegrees - NearestPitchDegrees) * Zoom;

    /// <summary>The camera's position relative to the point it follows, so that it looks along the yaw.</summary>
    public (float X, float Y, float Z) Offset
    {
        get
        {
            var pitch = PitchDegrees * MathF.PI / 180f;
            var yaw = YawDegrees * MathF.PI / 180f;
            var horizontal = Distance * MathF.Cos(pitch);
            return (-horizontal * MathF.Sin(yaw), Distance * MathF.Sin(pitch), horizontal * MathF.Cos(yaw));
        }
    }

    /// <summary>
    /// The view after turning for <paramref name="seconds"/> with <paramref name="input"/> in [-1, 1] (positive =
    /// right, which turns the heading clockwise). The yaw wraps into [0, 360).
    /// </summary>
    public CameraView Turned(float input, float seconds) =>
        this with { YawDegrees = Wrap(YawDegrees + input * RotateDegreesPerSecond * seconds) };

    /// <summary>
    /// The view after zooming for <paramref name="seconds"/> with <paramref name="input"/> in [-1, 1] (positive =
    /// out). The zoom stops at its limits.
    /// </summary>
    public CameraView Zoomed(float input, float seconds) =>
        this with { Zoom = Math.Clamp(Zoom + input * ZoomPerSecond * seconds, 0f, 1f) };

    /// <summary>
    /// Turns a movement input (from <c>Input.GetVector</c>: <paramref name="x"/> positive = right,
    /// <paramref name="y"/> negative = forward) into a world-space move on the ground plane, relative to the camera's
    /// yaw, so forward always walks away from the camera. Preserves the input's length; at yaw 0 it returns the input
    /// unchanged (issue #70, REQ-52).
    /// </summary>
    public (float X, float Z) ToWorldMove(float x, float y) => throw new NotImplementedException();

    private static float Wrap(float degrees)
    {
        var wrapped = degrees % 360f;
        if (wrapped < 0f)
        {
            wrapped += 360f;
        }

        // A tiny negative remainder can round up to exactly 360 in single precision.
        return wrapped >= 360f ? 0f : wrapped;
    }
}
