using LongWayHome.Infrastructure.Saves;
using LongWayHome.Tests.Games;

namespace LongWayHome.Tests.Saves;

/// <summary>
/// #166 edge cases: a save that loads at store stage 4 has the farm Shipping Bin at once; a version 1 save loads at
/// stage 1 with none (BR-S3; domain-model.md, Data Retention).
/// </summary>
public sealed class ShippingBinSaveTests : IDisposable
{
    private readonly TempSaveFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private JsonSaveStore Store() => new(_folder.Path);

    [Fact]
    public void BR_S3_A_save_at_store_stage_4_loads_with_the_farm_shipping_bin()
    {
        Store().Save(1, TestGames.WithStoreAt(SaveGames.Rich(), stage: 4));

        Assert.True(Store().Load(1).HasShippingBin);
    }

    [Fact]
    public void BR_S3_A_save_at_store_stage_3_loads_without_the_farm_shipping_bin()
    {
        Store().Save(1, TestGames.WithStoreAt(SaveGames.Rich(), stage: 3));

        Assert.False(Store().Load(1).HasShippingBin);
    }

    [Fact]
    public void BR_S3_A_version_1_save_loads_at_store_stage_1_without_the_farm_shipping_bin()
    {
        _folder.WriteSlot(1, SaveDocuments.V1([]));

        var loaded = Store().Load(1);

        Assert.Equal(1, loaded.BuildingStage(Domain.Catalog.BuildingId.GeneralStore));
        Assert.False(loaded.HasShippingBin);
    }
}
