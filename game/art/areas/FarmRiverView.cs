#nullable enable

using Godot;

namespace LongWayHome.Game.Art;

/// <summary>
/// The river down the farm's east side (#137, ART_DIRECTION §7.3), drawn as <see cref="TownView"/> draws the town's
/// stretch: a strip of shallows from the meadow's edge (x 16, the face of the scene's <c>Boundary/East</c> wall) to
/// x 16.8 and deep water beyond. Scenery only, with no collision; the farm's <c>river</c> water points stand on its bank.
/// Built once on start; holds no rules.
/// </summary>
public partial class FarmRiverView : Node3D
{
    /// <summary>The meadow's east edge and the river's west bank.</summary>
    private const float BankX = 16f;

    private const float MinZ = -16f;

    private const float MaxZ = 16f;

    public override void _Ready()
    {
        AddChild(AreaArt.Flat("RiverShallows", BankX, BankX + 0.8f, MinZ, MaxZ, -0.02f, "lake-shallow"));
        AddChild(AreaArt.Flat("River", BankX + 0.8f, BankX + 24f, MinZ, MaxZ, -0.03f, "lake-deep"));
    }
}
