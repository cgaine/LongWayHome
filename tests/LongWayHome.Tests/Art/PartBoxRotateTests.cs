using System.Numerics;
using LongWayHome.Application.Art;

namespace LongWayHome.Tests.Art;

/// <summary>
/// <c>PartBox.Rotate</c> (#104), which <see cref="FarmhouseArt"/> uses to place parts relative to a rotated part (the
/// door's brace and knob, the patch's stripe and rivets, the leaf tips, rising twigs): Godot's Euler YXZ,
/// Ry(yaw)·Rx(pitch)·Rz(roll), with the <see cref="CropPart"/> sign conventions.
/// </summary>
public class PartBoxRotateTests
{
    private const float Tolerance = 1e-5f;

    private static void AssertNear(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < Tolerance, $"expected {expected}, got {actual}");

    [Fact]
    public void Yaw_90_turns_plus_X_to_minus_Z_and_plus_Z_to_plus_X()
    {
        AssertNear(new Vector3(0f, 0f, -1f), PartBox.Rotate(Vector3.UnitX, 0f, 90f, 0f));
        AssertNear(Vector3.UnitX, PartBox.Rotate(Vector3.UnitZ, 0f, 90f, 0f));
    }

    [Fact]
    public void Positive_pitch_tips_plus_Y_towards_plus_Z()
    {
        AssertNear(Vector3.UnitZ, PartBox.Rotate(Vector3.UnitY, 90f, 0f, 0f));
    }

    [Fact]
    public void Positive_roll_lifts_plus_X()
    {
        AssertNear(Vector3.UnitY, PartBox.Rotate(Vector3.UnitX, 0f, 0f, 90f));
    }

    [Fact]
    public void Rolls_first_then_pitches_then_yaws()
    {
        // Rz(90): +X → +Y; Rx(90): +Y → +Z; Ry(90): +Z → +X.
        AssertNear(Vector3.UnitX, PartBox.Rotate(Vector3.UnitX, 90f, 90f, 90f));
        // Rx(90): +Y → +Z; Ry(90): +Z → +X (no roll).
        AssertNear(Vector3.UnitX, PartBox.Rotate(Vector3.UnitY, 90f, 90f, 0f));
    }

    [Fact]
    public void Rotating_keeps_the_length()
    {
        var v = new Vector3(0.3f, -1.2f, 2.5f);
        Assert.Equal(v.Length(), PartBox.Rotate(v, 33f, -71f, 12f).Length(), Tolerance);
    }
}
