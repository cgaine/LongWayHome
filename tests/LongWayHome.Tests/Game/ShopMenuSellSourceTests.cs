using System.Text.RegularExpressions;

namespace LongWayHome.Tests.Game;

/// <summary>
/// Reads the real <c>game/scripts/Ui/ShopMenu.cs</c> and checks the Godot glue of #146's Sell section: it is built from
/// the session's <c>CanSellToMaggie</c>, <c>SellableCrops()</c>, <c>SellToMaggie</c> and <c>SellAllToMaggie</c>, with the
/// <c>ShopText</c> labels and crop icons, and shown only when the session says Maggie buys here. Each check looks for
/// the essential tokens of the wiring, not exact formatting; the menu holds no rules of its own.
/// </summary>
public class ShopMenuSellSourceTests
{
    private static string Source() => SceneText.ReadRepoFile("game", "scripts", "Ui", "ShopMenu.cs");

    // ---- AC10: the Sell section in town ----

    [Fact]
    public void AC10_shop_menu_lists_a_row_per_sellable_crop_with_its_crop_icon()
    {
        var source = Source();

        Assert.Contains(".SellableCrops()", source, StringComparison.Ordinal);
        Assert.Contains("ShopText.SellRow(", source, StringComparison.Ordinal);
        Assert.Contains("ItemIcons.CropIcon(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_shop_menu_has_a_sell_header_and_a_sell_all_row()
    {
        var source = Source();

        Assert.Contains("ShopText.SellHeader", source, StringComparison.Ordinal);
        Assert.Contains("ShopText.SellAllRow(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_shop_menu_shows_the_nothing_to_sell_line_when_no_crop_is_carried()
    {
        Assert.Contains("ShopText.NothingToSell", Source(), StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_pressing_a_sell_row_sells_through_the_session()
    {
        var source = Source();

        Assert.Matches(@"\.SellToMaggie\s*\(", source);
        Assert.Matches(@"\.SellAllToMaggie\s*\(\s*\)", source);
    }

    [Fact]
    public void AC10_a_sale_shows_what_it_paid_or_the_rejection_text()
    {
        var source = Source();

        Assert.Contains("ShopText.AfterSell(", source, StringComparison.Ordinal);
        Assert.Contains("ShopText.Rejection(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AC10_BR_G2_the_menu_with_the_sell_section_still_pauses_the_tree()
    {
        var source = Source();

        // The Sell section sits in the same paused panel, so the clock doesn't move while selling (BR-G2).
        Assert.Matches(@"\.SellToMaggie\s*\(", source);
        Assert.Matches(@"GetTree\(\)\.Paused\s*=\s*true", source);
    }

    // ---- AC11: no Sell section on the farm ----

    [Fact]
    public void AC11_the_sell_section_is_gated_on_can_sell_to_maggie()
    {
        var source = Source();

        Assert.Matches(new Regex(@"\bif\s*\([^\n]*\bCanSellToMaggie\b"), source);
    }

    [Fact]
    public void AC11_shop_menu_leaves_the_area_check_to_the_session()
    {
        var source = Source();

        // The menu asks the session; it never checks the area itself (the area rule is the session's).
        Assert.Contains("CanSellToMaggie", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AreaId.Town", source, StringComparison.Ordinal);
    }
}
