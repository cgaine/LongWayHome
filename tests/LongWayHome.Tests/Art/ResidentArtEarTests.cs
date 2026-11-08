using LongWayHome.Application.Art;
using LongWayHome.Domain.Residents;

namespace LongWayHome.Tests.Art;

/// <summary>#138, ART_DIRECTION §6.2: the ears point "up and back", and the Residents' tables are built once.</summary>
public class ResidentArtEarTests
{
    [Theory]
    [InlineData(ResidentId.Maggie)]
    [InlineData(ResidentId.Clyde)]
    public void Ears_tip_back_and_out_from_the_head(ResidentId id)
    {
        var ears = ResidentArt.For(id).Where(part => part.Name.StartsWith("Ear", StringComparison.Ordinal)).ToList();

        Assert.Equal(2, ears.Count);
        Assert.All(ears, ear =>
        {
            // A negative pitch tips the +Y tip towards −Z (back); a roll opposite in sign to x leans it outwards.
            Assert.True(ear.PitchDegrees < 0f, $"{id}'s {ear.Name} pitch {ear.PitchDegrees} doesn't tip back");
            Assert.True(ear.RollDegrees * ear.X < 0f, $"{id}'s {ear.Name} roll {ear.RollDegrees} at x {ear.X} doesn't lean out");
        });
    }

    [Fact]
    public void Each_residents_table_is_built_once()
    {
        Assert.Same(ResidentArt.For(ResidentId.Maggie), ResidentArt.For(ResidentId.Maggie));
        Assert.Same(ResidentArt.For(ResidentId.Clyde), ResidentArt.For(ResidentId.Clyde));
    }

    [Fact]
    public void A_value_outside_the_enum_has_no_model()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ResidentArt.For((ResidentId)99));
    }
}
