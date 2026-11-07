using LongWayHome.Application.Games;

namespace LongWayHome.Application.Art;

/// <summary>
/// The parts of the item in the farmer's right hand (#102), in the hand's space (origin at the <c>RightHand</c> bone,
/// y up from the grip). Pure C#, no Godot.
/// </summary>
public static class HeldItemArt
{
    /// <summary>The parts of the basic hoe, the basic watering can or a seed pouch in the crop's colour.</summary>
    public static IReadOnlyList<CropPart> PartsFor(HeldItem held) => throw new NotImplementedException();
}
