using System.Collections.ObjectModel;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;

namespace LongWayHome.Domain.Buildings;

/// <summary>
/// A town Building (child of Game, one per <see cref="BuildingId"/>): its stage and what has been delivered toward the
/// next stage (BR-B1, BR-B2). What has been delivered only grows, through the Game's deliveries; it is never refunded.
/// </summary>
public sealed class Building
{
    private readonly SortedDictionary<MaterialId, int> _delivered;

    /// <summary>
    /// A Building at <paramref name="stage"/> with <paramref name="delivered"/> items and <paramref name="deliveredGold"/>
    /// gold handed over toward its next stage. An undefined material, a negative stage or gold, or a delivered
    /// quantity below 1 throws.
    /// </summary>
    public Building(
        BuildingId id, int stage, IReadOnlyDictionary<MaterialId, int>? delivered = null, int deliveredGold = 0)
    {
        // The id is checked by the Game, which holds exactly one Building per BuildingId.
        ArgumentOutOfRangeException.ThrowIfNegative(stage);
        ArgumentOutOfRangeException.ThrowIfNegative(deliveredGold);

        _delivered = [];
        foreach (var (material, quantity) in delivered ?? new Dictionary<MaterialId, int>())
        {
            if (!Enum.IsDefined(material))
            {
                throw new ArgumentOutOfRangeException(nameof(delivered), material, "Not a material.");
            }

            if (quantity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delivered), quantity, $"A delivered {material} quantity is at least 1.");
            }

            _delivered[material] = quantity;
        }

        Id = id;
        Stage = stage;
        Delivered = new ReadOnlyDictionary<MaterialId, int>(_delivered);
        DeliveredGold = deliveredGold;
    }

    public BuildingId Id { get; }

    /// <summary>The Building's current stage (BR-B1).</summary>
    public int Stage { get; }

    /// <summary>The items delivered toward the next stage, by material in enum order (BR-B2).</summary>
    public IReadOnlyDictionary<MaterialId, int> Delivered { get; }

    /// <summary>The gold delivered toward the next stage (BR-B2).</summary>
    public int DeliveredGold { get; private set; }

    /// <summary>How many of <paramref name="material"/> have been delivered toward the next stage.</summary>
    public int DeliveredOf(MaterialId material) => _delivered.GetValueOrDefault(material);

    /// <summary>The stage row for <see cref="Stage"/> + 1 of this Building, or null when there is none (BR-B1, BR-B4).</summary>
    public BuildingStage? NextStage(IReadOnlyList<BuildingStage> stages)
    {
        ArgumentNullException.ThrowIfNull(stages);

        // BR-B1: one stage at a time, so the next stage is always Stage + 1.
        return stages.FirstOrDefault(row => row.BuildingId == Id && row.Stage == Stage + 1);
    }

    /// <summary>Whether <see cref="Delivered"/> and <see cref="DeliveredGold"/> cover the next stage's full cost (BR-B3).</summary>
    public bool RebuildDue(IReadOnlyList<BuildingStage> stages)
    {
        var next = NextStage(stages);
        return next is not null
            && DeliveredGold >= next.CostGold.Value
            && next.CostItems.All(cost => DeliveredOf(cost.Material) >= cost.Quantity);
    }

    /// <summary>Records <paramref name="quantity"/> more of <paramref name="material"/> delivered (BR-B2); Game only.</summary>
    internal void AddDelivered(MaterialId material, int quantity) =>
        _delivered[material] = DeliveredOf(material) + quantity;

    /// <summary>Records <paramref name="gold"/> more gold delivered (BR-B2); Game only.</summary>
    internal void AddDeliveredGold(int gold) => DeliveredGold += gold;

    /// <summary>Raises the stage by one and clears the deliveries (BR-B3); Game only.</summary>
    internal void Rebuild() => throw new NotImplementedException();
}
