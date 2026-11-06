namespace LongWayHome.Domain.Farming;

/// <summary>A Planting's growth status. Derived from DaysGrown and the Crop type's growDays, never stored.
/// Serialized as <c>growing</c>, <c>ripe</c>.</summary>
public enum PlantingStatus
{
    Growing,
    Ripe,
}
