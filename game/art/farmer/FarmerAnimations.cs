#nullable enable

using System.Linq;
using Godot;
using LongWayHome.Application.Art;

namespace LongWayHome.Game.Art;

/// <summary>
/// Builds the farmer's <see cref="AnimationLibrary"/> (#103) from <see cref="FarmerMoves"/>: one
/// <see cref="Animation"/> per <see cref="FarmerClip"/>, named in snake case (<see cref="ClipName"/>). Each has a
/// rotation track on <c>Skeleton:&lt;Bone&gt;</c> for every bone in <see cref="FarmerArt.Bones"/> (a bone the clip
/// doesn't key holds its rest, so no pose is left over from the clip before) and a position track on
/// <c>Skeleton:Hips</c>: the rest position plus the clip's (0, Y, Z) offsets. The rests have no rotation, so a key's
/// quaternion is its Euler YXZ composed with the identity. Looping clips use <see cref="Animation.LoopModeEnum.Linear"/>.
/// The track paths are relative to the <c>AnimationPlayer</c>'s root node, the <c>Farmer</c> (its parent). Built once
/// and shared by every farmer.
/// </summary>
public static class FarmerAnimations
{
    private const string SkeletonPath = "Skeleton";

    private const string RootBone = "Hips";

    private static AnimationLibrary? _library;

    /// <summary>The library of all eleven clips, built on first use.</summary>
    public static AnimationLibrary Build()
    {
        if (_library is null)
        {
            var library = new AnimationLibrary();
            foreach (var clip in System.Enum.GetValues<FarmerClip>())
            {
                library.AddAnimation(ClipName(clip), BuildClip(FarmerMoves.For(clip)));
            }

            _library = library;
        }

        return _library;
    }

    /// <summary>The clip's animation name in the library.</summary>
    public static string ClipName(FarmerClip clip) => clip switch
    {
        FarmerClip.Idle => "idle",
        FarmerClip.Walk => "walk",
        FarmerClip.Run => "run",
        FarmerClip.HoeSwing => "hoe_swing",
        FarmerClip.Water => "water",
        FarmerClip.Plant => "plant",
        FarmerClip.Harvest => "harvest",
        FarmerClip.Refill => "refill",
        FarmerClip.Ship => "ship",
        FarmerClip.Sleep => "sleep",
        FarmerClip.PassOut => "pass_out",
        _ => throw new System.ArgumentOutOfRangeException(nameof(clip), clip, "Unknown farmer clip."),
    };

    private static Animation BuildClip(FarmerClipSpec spec)
    {
        var animation = new Animation
        {
            Length = spec.LengthSeconds,
            LoopMode = spec.Loops ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None,
        };

        var keysByBone = spec.Keys
            .GroupBy(key => key.Bone)
            .ToDictionary(group => group.Key, group => group.OrderBy(key => key.Time).ToList());

        foreach (var bone in FarmerArt.Bones)
        {
            var track = animation.AddTrack(Animation.TrackType.Rotation3D);
            animation.TrackSetPath(track, $"{SkeletonPath}:{bone.Name}");
            if (keysByBone.TryGetValue(bone.Name, out var keys))
            {
                foreach (var key in keys)
                {
                    animation.RotationTrackInsertKey(track, key.Time, Rotation(key));
                }
            }
            else
            {
                animation.RotationTrackInsertKey(track, 0f, Quaternion.Identity);
            }
        }

        var hipsRest = HipsRest();
        var hips = animation.AddTrack(Animation.TrackType.Position3D);
        animation.TrackSetPath(hips, $"{SkeletonPath}:{RootBone}");
        if (spec.Hips.Count == 0)
        {
            animation.PositionTrackInsertKey(hips, 0f, hipsRest);
        }
        else
        {
            foreach (var key in spec.Hips.OrderBy(key => key.Time))
            {
                animation.PositionTrackInsertKey(hips, key.Time, hipsRest + new Vector3(0f, key.Y, key.Z));
            }
        }

        return animation;
    }

    /// <summary>The key's rotation from the rest (identity), by Godot's Euler YXZ.</summary>
    private static Quaternion Rotation(FarmerKey key)
    {
        var euler = new Vector3(
            Mathf.DegToRad(key.PitchDegrees),
            Mathf.DegToRad(key.YawDegrees),
            Mathf.DegToRad(key.RollDegrees));
        return Basis.FromEuler(euler, EulerOrder.Yxz).GetRotationQuaternion();
    }

    /// <summary><c>Hips</c> is the root, so its rest position is its model-space position.</summary>
    private static Vector3 HipsRest()
    {
        var hips = FarmerArt.Bones.First(bone => bone.Name == RootBone);
        return new Vector3(hips.X, hips.Y, hips.Z);
    }
}
