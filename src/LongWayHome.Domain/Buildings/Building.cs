using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Buildings;

/// <summary>
/// A town Building (child of Game, one per <see cref="BuildingId"/>): its stage and what has been delivered toward the
/// next stage (BR-B1, BR-B2).
/// </summary>
public sealed class Building
{
    public Building(
        BuildingId id, int stage, IReadOnlyDictionary<MaterialId, int>? delivered = null, int deliveredGold = 0)
    {
        // #163 red stub: no validation yet.
        Id = id;
        Stage = stage;
        Delivered = delivered ?? new Dictionary<MaterialId, int>();
        DeliveredGold = deliveredGold;
    }

    public BuildingId Id { get; }

    /// <summary>The Building's current stage (BR-B1).</summary>
    public int Stage { get; }

    /// <summary>The items delivered toward the next stage, by material (BR-B2).</summary>
    public IReadOnlyDictionary<MaterialId, int> Delivered { get; }

    /// <summary>The gold delivered toward the next stage (BR-B2).</summary>
    public int DeliveredGold { get; }

    /// <summary>The stage row for <see cref="Stage"/> + 1 of this Building, or null when there is none (BR-B1, BR-B4).</summary>
    public BuildingStage? NextStage(IReadOnlyList<BuildingStage> stages) => throw new NotImplementedException();

    /// <summary>Whether <see cref="Delivered"/> and <see cref="DeliveredGold"/> cover the next stage's full cost (BR-B3).</summary>
    public bool RebuildDue(IReadOnlyList<BuildingStage> stages) => throw new NotImplementedException();
}
