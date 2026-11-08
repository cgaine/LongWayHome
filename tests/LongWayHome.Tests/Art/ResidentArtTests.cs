using System.Numerics;
using LongWayHome.Application.Art;
using LongWayHome.Domain.Residents;

namespace LongWayHome.Tests.Art;

/// <summary>
/// #138: Maggie's and Clyde's part tables (ART_DIRECTION §6.2), with the <see cref="CropPart"/> conventions: every
/// colour a palette swatch, the farmer's chunky proportions, and two pointed ears each.
/// <para>
/// <b>Naming conventions these tests rely on.</b> An ear is a part whose name starts with <c>Ear</c> (and not
/// <c>Earth</c>); each Resident has exactly two, one on either side of the head (opposite signs of X), each a
/// <see cref="CropPartShape.Prism"/> or a cone (a <see cref="CropPartShape.Cylinder"/> whose top diameter,
/// <c>SizeZ</c>, is under half its bottom one), tilted rather than upright. Clyde's fishing rod is the parts named
/// <c>Rod*</c>, left out of his body's bounds since it rises over his shoulder. Maggie has <c>Bun*</c>,
/// <c>Glasses*</c> and <c>Apron*</c> parts; Clyde has <c>Cap*</c>, <c>Beard*</c> and <c>Rod*</c> parts.
/// </para>
/// </summary>
public class ResidentArtTests
{
    public static TheoryData<ResidentId> Drawn => new(ResidentId.Maggie, ResidentId.Clyde);

    private static bool Named(CropPart part, string prefix) => part.Name.StartsWith(prefix, StringComparison.Ordinal);

    private static bool IsEar(CropPart part) => Named(part, "Ear") && !Named(part, "Earth");

    private static (Vector3 Min, Vector3 Max) Extent(IEnumerable<CropPart> parts) =>
        PartBoxes.Extents(parts.Select(PartBoxes.Corners));

    private static IEnumerable<CropPart> Body(ResidentId id) =>
        ResidentArt.For(id).Where(part => !Named(part, "Rod"));

    private static HashSet<string> Hexes(ResidentId id) =>
        ResidentArt.For(id).Select(part => ArtPalette.Hex(part.Role)).ToHashSet(StringComparer.Ordinal);

    // ---- AC6: colours ----

    [Theory]
    [MemberData(nameof(Drawn))]
    public void AC6_Every_part_colour_is_an_ArtPalette_swatch(ResidentId id)
    {
        var parts = ResidentArt.For(id);
        var roles = ArtPalette.Swatches.Select(swatch => swatch.Role).ToHashSet(StringComparer.Ordinal);

        Assert.NotEmpty(parts);
        Assert.All(parts, part => Assert.True(roles.Contains(part.Role), $"{id}'s {part.Name} has unknown role '{part.Role}'"));
    }

    [Theory]
    [InlineData("#9A7EA8")]
    [InlineData("#3E6E6A")]
    [InlineData("#EDE3CF")]
    [InlineData("#3E6E7A")]
    [InlineData("#C9A040")]
    [InlineData("#4A5568")]
    public void AC6_The_specs_outfit_colours_are_in_the_palette(string hex)
    {
        Assert.Contains(hex, ArtPalette.Swatches.Select(swatch => swatch.Hex));
    }

    [Fact]
    public void AC6_Maggie_wears_the_lavender_cardigan_teal_skirt_and_cream_apron()
    {
        var hexes = Hexes(ResidentId.Maggie);

        Assert.Contains("#9A7EA8", hexes);
        Assert.Contains("#3E6E6A", hexes);
        Assert.Contains("#EDE3CF", hexes);
    }

    [Fact]
    public void AC6_Clyde_wears_the_teal_cap_mustard_oilskin_and_slate_trousers()
    {
        var hexes = Hexes(ResidentId.Clyde);

        Assert.Contains("#3E6E7A", hexes);
        Assert.Contains("#C9A040", hexes);
        Assert.Contains("#4A5568", hexes);
    }

    [Theory]
    [InlineData(ResidentId.Maggie, "Bun")]
    [InlineData(ResidentId.Maggie, "Glasses")]
    [InlineData(ResidentId.Maggie, "Apron")]
    [InlineData(ResidentId.Clyde, "Cap")]
    [InlineData(ResidentId.Clyde, "Beard")]
    [InlineData(ResidentId.Clyde, "Rod")]
    public void AC6_Each_resident_has_their_signature_parts(ResidentId id, string prefix)
    {
        Assert.Contains(ResidentArt.For(id), part => Named(part, prefix));
    }

    // ---- AC6: two pointed ears ----

    [Theory]
    [MemberData(nameof(Drawn))]
    public void AC6_Each_has_exactly_two_ears_one_on_either_side_of_the_head(ResidentId id)
    {
        var ears = ResidentArt.For(id).Where(IsEar).ToList();

        Assert.True(ears.Count == 2, $"{id} has {ears.Count} ear parts ({string.Join(", ", ears.Select(e => e.Name))}), not 2");
        Assert.True(ears[0].X * ears[1].X < 0, $"{id}'s ears are at x {ears[0].X} and {ears[1].X}, not either side");
        Assert.Equal(MathF.Abs(ears[0].X), MathF.Abs(ears[1].X), 1e-3f);
        var (_, max) = Extent(Body(id));
        Assert.All(ears, ear => Assert.True(
            ear.Y > max.Y * 0.6f,
            $"{id}'s {ear.Name} is at y {ear.Y}, below the head (top {max.Y})"));
    }

    [Theory]
    [MemberData(nameof(Drawn))]
    public void AC6_Both_ears_are_pointed_and_angled(ResidentId id)
    {
        var ears = ResidentArt.For(id).Where(IsEar).ToList();
        Assert.Equal(2, ears.Count);

        Assert.All(ears, ear =>
        {
            var pointed = ear.Shape == CropPartShape.Prism
                || (ear.Shape == CropPartShape.Cylinder && ear.SizeZ < ear.SizeX * 0.5f);
            Assert.True(pointed, $"{id}'s {ear.Name} is a {ear.Shape} (top {ear.SizeZ}, bottom {ear.SizeX}), not pointed");
            Assert.True(
                MathF.Abs(ear.PitchDegrees) + MathF.Abs(ear.RollDegrees) >= 5f,
                $"{id}'s {ear.Name} stands upright (pitch {ear.PitchDegrees}, roll {ear.RollDegrees}); ears angle up and back");
        });
    }

    // ---- shape and proportions ----

    [Theory]
    [MemberData(nameof(Drawn))]
    public void Every_part_has_a_unique_name_and_a_positive_size(ResidentId id)
    {
        var parts = ResidentArt.For(id);

        var duplicates = parts.GroupBy(part => part.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.True(duplicates.Count == 0, $"{id} has duplicate part names: {string.Join(", ", duplicates)}");
        Assert.All(parts, part => Assert.True(part.SizeX > 0f && part.SizeY > 0f, $"{id}'s {part.Name} has a zero size"));
    }

    [Theory]
    [MemberData(nameof(Drawn))]
    public void Each_stands_on_the_ground_about_the_farmers_size(ResidentId id)
    {
        var (min, max) = Extent(Body(id));

        Assert.InRange(min.Y, -0.05f, 0.05f);
        Assert.InRange(max.Y, 1.1f, 1.75f);
        Assert.True(max.X - min.X <= 1.0f && max.Z - min.Z <= 1.0f, $"{id}'s body is {max.X - min.X:0.##} x {max.Z - min.Z:0.##} m");
    }

    [Fact]
    public void Maggie_is_small_shorter_than_clyde_and_the_farmer()
    {
        var maggie = Extent(Body(ResidentId.Maggie)).Max.Y;
        var clyde = Extent(Body(ResidentId.Clyde)).Max.Y;
        var farmer = FarmerArt.Bounds(FarmerArt.Parts).Height;

        Assert.True(maggie < clyde, $"Maggie ({maggie:0.##} m) is not shorter than Clyde ({clyde:0.##} m)");
        Assert.True(maggie < farmer, $"Maggie ({maggie:0.##} m) is not shorter than the farmer ({farmer:0.##} m)");
    }

    [Theory]
    [InlineData(ResidentId.Tom)]
    [InlineData(ResidentId.June)]
    [InlineData(ResidentId.Jesse)]
    [InlineData(ResidentId.Mira)]
    public void The_other_four_have_no_model_yet(ResidentId id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentArt.For(id));
    }
}
