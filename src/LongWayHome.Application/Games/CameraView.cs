namespace LongWayHome.Application.Games;

/// <summary>Placeholder for the red step (issue #68).</summary>
public readonly record struct CameraView(float YawDegrees, float Zoom)
{
    public const float RotateDegreesPerSecond = 0;
    public const float ZoomPerSecond = 0;
    public const float NearestDistance = 0;
    public const float FarthestDistance = 0;
    public const float PitchDegrees = 0;
    public static CameraView Default => default;
    public float Distance => throw new NotImplementedException();
    public (float X, float Y, float Z) Offset => throw new NotImplementedException();
    public CameraView Turned(float input, float seconds) => throw new NotImplementedException();
    public CameraView Zoomed(float input, float seconds) => throw new NotImplementedException();
}
