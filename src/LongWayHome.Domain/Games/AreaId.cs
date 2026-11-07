namespace LongWayHome.Domain.Games;

/// <summary>
/// The areas of the world (canonical-values.md): <c>farm</c>, <c>forest_path</c>, <c>town</c>, <c>lakeshore</c>,
/// <c>across_river</c>, <c>mine</c>. Only <see cref="Farm"/> is used until the areas exist.
/// </summary>
public enum AreaId
{
    Farm,
    ForestPath,
    Town,
    Lakeshore,
    AcrossRiver,
    Mine,
}
