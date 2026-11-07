using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;

namespace LongWayHome.Tests.Art;

/// <summary>
/// AC1 of #94: which <see cref="CropArt"/> parts glow at night, and how. Only the ripe parts in the issue's table have
/// <see cref="CropPart.Glow"/> &gt; 0, so a glow also says "ripe, ready to harvest" (BR-T6): emberroot's <c>Core</c>
/// (1.0, steady), dusktuber's <c>Speckle0</c>–<c>Speckle7</c> (1.0, twinkle), moonmelon's <c>Rib0</c>–<c>Rib5</c> and
/// <c>Crown</c> (0.6, steady) and starcorn's <c>Sparkle0</c>–<c>Sparkle7</c> (1.0, twinkle). Sunbell has none.
/// </summary>
public class CropGlowTests
{
    private const float Tolerance = 1e-6f;

    private static readonly CropTypeId[] DrawnCrops =
        [CropTypeId.Emberroot, CropTypeId.Dusktuber, CropTypeId.Sunbell, CropTypeId.Starcorn, CropTypeId.Moonmelon];

    private static readonly GrowthStage[] UnripeStages = [GrowthStage.Seed, GrowthStage.Sprout, GrowthStage.Young];

    /// <summary>The issue's table: (crop, part name) → (Glow, Twinkle), all at the Ripe stage.</summary>
    private static Dictionary<(CropTypeId Crop, string Part), (float Glow, bool Twinkle)> ExpectedGlows()
    {
        var expected = new Dictionary<(CropTypeId, string), (float, bool)>
        {
            [(CropTypeId.Emberroot, "Core")] = (1.0f, false),
            [(CropTypeId.Moonmelon, "Crown")] = (0.6f, false),
        };
        for (var i = 0; i < 8; i++)
        {
            expected[(CropTypeId.Dusktuber, $"Speckle{i}")] = (1.0f, true);
            expected[(CropTypeId.Starcorn, $"Sparkle{i}")] = (1.0f, true);
        }

        for (var i = 0; i < 6; i++)
        {
            expected[(CropTypeId.Moonmelon, $"Rib{i}")] = (0.6f, false);
        }

        return expected;
    }

    public static TheoryData<CropTypeId, GrowthStage> UnripeModels()
    {
        var data = new TheoryData<CropTypeId, GrowthStage>();
        foreach (var crop in DrawnCrops)
        {
            foreach (var stage in UnripeStages)
            {
                data.Add(crop, stage);
            }
        }

        return data;
    }

    public static TheoryData<CropTypeId, GrowthStage> AllModels()
    {
        var data = new TheoryData<CropTypeId, GrowthStage>();
        foreach (var crop in DrawnCrops)
        {
            foreach (var stage in UnripeStages.Append(GrowthStage.Ripe))
            {
                data.Add(crop, stage);
            }
        }

        return data;
    }

    [Fact]
    public void AC1_crop_part_defaults_to_no_glow_and_no_twinkle()
    {
        var part = new CropPart("Probe", CropPartShape.Box, 0.1f, 0.1f, 0.1f, 0f, 0f, 0f, 0f, 0f, 0f, "foliage");

        Assert.Equal(0f, part.Glow);
        Assert.False(part.Twinkle);
    }

    [Fact]
    public void AC1_exactly_the_table_parts_glow_across_every_crop_and_stage()
    {
        var glowing = DrawnCrops
            .SelectMany(crop => UnripeStages.Append(GrowthStage.Ripe)
                .SelectMany(stage => CropArt.Parts(crop, stage)
                    .Where(part => part.Glow > 0f)
                    .Select(part => $"{crop}/{stage}/{part.Name}")))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var expected = ExpectedGlows().Keys
            .Select(key => $"{key.Crop}/{GrowthStage.Ripe}/{key.Part}")
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, glowing);
    }

    [Theory]
    [InlineData(CropTypeId.Emberroot)]
    [InlineData(CropTypeId.Dusktuber)]
    [InlineData(CropTypeId.Moonmelon)]
    [InlineData(CropTypeId.Starcorn)]
    public void AC1_ripe_table_parts_have_the_listed_glow_and_twinkle(CropTypeId crop)
    {
        var parts = CropArt.Parts(crop, GrowthStage.Ripe).ToDictionary(part => part.Name, StringComparer.Ordinal);
        var rows = ExpectedGlows().Where(row => row.Key.Crop == crop).ToList();
        Assert.NotEmpty(rows);

        foreach (var ((_, name), (glow, twinkle)) in rows)
        {
            Assert.True(parts.ContainsKey(name), $"{crop} Ripe has no part {name}");
            var part = parts[name];
            Assert.True(
                Math.Abs(part.Glow - glow) < Tolerance,
                $"{crop} Ripe {name}: Glow {part.Glow}, expected {glow}");
            Assert.True(part.Twinkle == twinkle, $"{crop} Ripe {name}: Twinkle {part.Twinkle}, expected {twinkle}");
        }
    }

    [Fact]
    public void AC1_moonmelon_glows_softly_and_steadily_over_the_whole_gourd()
    {
        // "Glows softly; the biggest night glow on the farm": 0.6 on the ribs and crown, the whole gourd.
        var moonmelon = CropArt.Parts(CropTypeId.Moonmelon, GrowthStage.Ripe).Where(part => part.Glow > 0f).ToList();

        Assert.Equal(7, moonmelon.Count);
        Assert.All(moonmelon, part => Assert.False(part.Twinkle));
        Assert.All(moonmelon, part => Assert.True(Math.Abs(part.Glow - 0.6f) < Tolerance, $"{part.Name}: {part.Glow}"));
    }

    [Theory]
    [MemberData(nameof(UnripeModels))]
    public void AC1_no_seed_sprout_or_young_part_glows(CropTypeId crop, GrowthStage stage)
    {
        var glowing = CropArt.Parts(crop, stage).Where(part => part.Glow > 0f).Select(part => part.Name).ToList();

        Assert.Empty(glowing);
    }

    [Fact]
    public void AC1_no_ripe_sunbell_part_glows()
    {
        var glowing = CropArt.Parts(CropTypeId.Sunbell, GrowthStage.Ripe)
            .Where(part => part.Glow > 0f)
            .Select(part => part.Name)
            .ToList();

        Assert.Empty(glowing);
    }

    [Theory]
    [MemberData(nameof(AllModels))]
    public void AC1_no_part_twinkles_without_a_glow_and_no_glow_exceeds_one(CropTypeId crop, GrowthStage stage)
    {
        foreach (var part in CropArt.Parts(crop, stage))
        {
            Assert.False(part.Twinkle && part.Glow <= 0f, $"{crop} {stage} {part.Name} twinkles with no glow");
            Assert.InRange(part.Glow, 0f, 1f);
        }
    }
}
