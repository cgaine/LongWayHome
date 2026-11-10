using System.Text.RegularExpressions;
using System.Xml.Linq;
using LongWayHome.Application.Art;
using LongWayHome.Application.Games;
using LongWayHome.Domain.Catalog;
using LongWayHome.Domain.Players;
using LongWayHome.Tests.Games;
using static LongWayHome.Tests.Game.SceneText;

namespace LongWayHome.Tests.Shop;

/// <summary>
/// #166 paths the red tests did not drive: the Can upgrade still sorts last, rows and icons for listings missing their
/// id, the backpack upgrade icon's file, and the farm's gate and Hud glue order.
/// </summary>
public class StoreStageUnlockExtraTests
{
    private static ShopListing BackpackListing(int? capacity) =>
        new("backpack_24", ShopItemKind.BackpackUpgrade, null, new Gold(400), BuildingId.GeneralStore, 2, null, true, null, capacity);

    private static ShopListing PlaceableListing(PlaceableId? placeable) =>
        new("chest_store", ShopItemKind.Placeable, null, new Gold(100), BuildingId.GeneralStore, 3, null, false, placeable, null);

    [Fact]
    public void BR_L5_The_Can_upgrade_still_sorts_after_the_placeables()
    {
        var game = BuildingGames.WithBuildings(BuildingGames.All(storeStage: 3), null, 5000);

        var kinds = ShopService.Offers(game, TestGames.Shipped).Select(listing => listing.Kind).ToList();

        Assert.Equal(ShopItemKind.CanUpgrade, kinds[^1]);
        Assert.Equal(ShopItemKind.Placeable, kinds[^2]);
    }

    [Fact]
    public void A_backpack_upgrade_row_with_no_capacity_throws() =>
        Assert.Throws<ArgumentException>(() => ShopText.Row(BackpackListing(null), TestGames.Shipped));

    [Fact]
    public void A_placeable_row_with_no_placeable_throws() =>
        Assert.Throws<ArgumentException>(() => ShopText.Row(PlaceableListing(null), TestGames.Shipped));

    [Fact]
    public void A_fence_listing_reads_Fence_and_shows_the_fence_icon()
    {
        var fence = PlaceableListing(PlaceableId.Fence);

        Assert.Equal("Fence — 100g", ShopText.Row(fence, TestGames.Shipped));
        Assert.Equal(ItemIcons.PlaceableIcon(PlaceableId.Fence), ItemIcons.For(fence));
    }

    [Fact]
    public void BR_K3_A_chest_that_dropped_says_so_and_seeds_keep_their_message()
    {
        var seed = TestGames.Shipped.ShopListings.Single(listing => listing.Id == "seed_emberroot");

        Assert.Equal("The chest dropped at your feet", ShopText.AfterBuy(PlaceableListing(PlaceableId.Chest), dropped: true));
        Assert.Equal("", ShopText.AfterBuy(PlaceableListing(PlaceableId.Chest), dropped: false));
        Assert.Equal(ShopText.SeedsDropped, ShopText.AfterBuy(seed, dropped: true));
        Assert.Equal("", ShopText.AfterBuy(BackpackListing(24), dropped: false));
    }

    [Fact]
    public void A_placeable_listing_with_no_placeable_has_no_icon() =>
        Assert.Null(ItemIcons.For(PlaceableListing(null)));

    [Fact]
    public void A_placeable_listing_with_no_placeable_is_never_offered()
    {
        var data = TestGames.Shipped with { ShopListings = [PlaceableListing(null)] };
        var game = BuildingGames.WithBuildings(BuildingGames.All(storeStage: 3), null, 5000);

        Assert.Empty(ShopService.Offers(game, data));
    }

    [Fact]
    public void AC8_The_backpack_upgrade_icon_has_its_godot_import_and_is_64_by_64_flat_in_palette_colours()
    {
        var path = Path.Combine(RepoRoot(), "game", "art", "icons", "backpack_upgrade.svg");
        Assert.True(File.Exists(path + ".import"), "backpack_upgrade.svg.import is missing");

        var svg = XDocument.Load(path);
        var palette = ArtPalette.Swatches.Select(swatch => swatch.Hex).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Equal("0 0 64 64", (string?)svg.Root!.Attribute("viewBox"));
        string[] forbidden = ["linearGradient", "radialGradient", "filter", "image", "style"];
        Assert.DoesNotContain(svg.Descendants(), element => forbidden.Contains(element.Name.LocalName));

        var paints = svg.Descendants()
            .SelectMany(element => new[] { element.Attribute("fill"), element.Attribute("stroke") })
            .Where(attribute => attribute is not null)
            .Select(attribute => attribute!.Value.Trim())
            .Where(value => value != "none")
            .ToList();
        Assert.NotEmpty(paints);
        Assert.All(paints, value => Assert.True(Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$") && palette.Contains(value), $"{value} is not a palette colour"));
        Assert.Contains(paints, value => string.Equals(value, ArtPalette.Hex("navy"), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BR_S3_The_gate_comes_before_Common_in_Farm_tscn_so_it_adds_the_bin_before_the_Hud_looks_again()
    {
        // Godot readies siblings in order, so the gate subscribes to NightCompleted before the Hud inside Common.
        var names = Nodes(ReadRepoFile("game", "scenes", "Farm.tscn"))
            .Where(node => node.Parent == ".")
            .Select(node => node.Name)
            .ToList();

        Assert.True(names.IndexOf("ShippingBinGate") >= 0, "Farm.tscn has no ShippingBinGate");
        Assert.True(names.IndexOf("ShippingBinGate") < names.IndexOf("Common"), "ShippingBinGate must come before Common");
    }

    [Fact]
    public void AC8_The_Hud_lets_go_of_the_old_bin_point_before_listening_to_a_new_one()
    {
        var source = Code(ReadRepoFile("game", "scripts", "Ui", "Hud.cs"));
        var find = Regex.Match(source, @"void FindBinPoint\(\)\s*\{(?<body>.*?)\n    \}", RegexOptions.Singleline);
        Assert.True(find.Success, "Hud has no FindBinPoint");
        var body = find.Groups["body"].Value;

        var release = body.IndexOf("ReleaseBinPoint()", StringComparison.Ordinal);
        var subscribe = body.IndexOf("PlayerEntered += ShowShipHint", StringComparison.Ordinal);
        Assert.True(release >= 0 && subscribe > release, "FindBinPoint must release the old point before subscribing");
        Assert.Matches(@"void ReleaseBinPoint\(\)\s*\{[^}]*PlayerEntered -= ShowShipHint;[^}]*PlayerExited -= HideShipHint;", source);
        Assert.Matches(@"void _ExitTree\(\)[\s\S]*ReleaseBinPoint\(\);", source);
    }
}
