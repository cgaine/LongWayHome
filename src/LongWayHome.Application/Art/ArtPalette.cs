namespace LongWayHome.Application.Art;

/// <summary>One palette swatch: an ART_DIRECTION colour role and its hex.</summary>
public readonly record struct ArtSwatch(string Role, string Hex);

/// <summary>The shared art palette (#76). Not implemented yet.</summary>
public static class ArtPalette
{
    public const int Columns = 8;
    public const int SwatchSize = 8;
    public const int ImageSize = 64;

    public static IReadOnlyList<ArtSwatch> Swatches => throw new NotImplementedException();

    public static (float U, float V) UvCentre(string role) => throw new NotImplementedException();

    public static byte[] EncodePng() => throw new NotImplementedException();
}
