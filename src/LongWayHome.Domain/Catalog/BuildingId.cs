namespace LongWayHome.Domain.Catalog;

/// <summary>
/// The town's Buildings: the key of each Game's Building and the vendor of a Shop listing. Serialized as
/// <c>general_store</c>, <c>carpenter</c>, <c>miras_house</c>, <c>blacksmith</c>.
/// </summary>
public enum BuildingId
{
    GeneralStore,
    Carpenter,
    MirasHouse,
    Blacksmith,
}
