namespace LongWayHome.Domain.Catalog;

/// <summary>
/// Canonical Crop type identifiers. Serialized in snake_case (<c>emberroot</c>).
/// Sunbell and starcorn exist as values but have no data rows yet (REQ-42).
/// </summary>
public enum CropTypeId
{
    Emberroot,
    Dusktuber,
    Moonmelon,
    Sunbell,
    Starcorn,
}
