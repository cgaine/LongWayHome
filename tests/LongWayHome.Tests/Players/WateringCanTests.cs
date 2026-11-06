using LongWayHome.Domain.Players;

namespace LongWayHome.Tests.Players;

public class WateringCanTests
{
    [Fact]
    public void BR_W1_new_can_starts_full_and_not_upgraded()
    {
        var can = new WateringCan(20);

        Assert.Equal(20, can.Capacity);
        Assert.Equal(20, can.Charges);
        Assert.False(can.Upgraded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BR_W1_can_with_capacity_below_1_throws(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WateringCan(capacity));
    }

    [Fact]
    public void BR_W1_can_with_capacity_1_is_full()
    {
        var can = new WateringCan(1);

        Assert.Equal(1, can.Capacity);
        Assert.Equal(1, can.Charges);
    }

    // ---- #25: using and refilling charges ----

    [Fact]
    public void BR_W1_using_charges_never_takes_an_empty_can_below_0()
    {
        var can = new WateringCan(3, 1, false);

        can.TryUseCharge();
        can.TryUseCharge();
        can.TryUseCharge();

        Assert.Equal(0, can.Charges);
        Assert.Equal(3, can.Capacity);
    }

    [Fact]
    public void BR_W1_refilling_never_takes_charges_above_capacity()
    {
        var can = new WateringCan(20);

        can.Refill();
        can.Refill();

        Assert.Equal(can.Capacity, can.Charges);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(21)]
    public void Restored_can_rejects_charges_outside_0_to_capacity(int charges)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WateringCan(20, charges, false));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(7, false)]
    [InlineData(40, true)]
    public void Restored_can_keeps_its_capacity_charges_and_upgraded_flag(int charges, bool upgraded)
    {
        var can = new WateringCan(40, charges, upgraded);

        Assert.Equal(40, can.Capacity);
        Assert.Equal(charges, can.Charges);
        Assert.Equal(upgraded, can.Upgraded);
    }

    [Fact]
    public void BR_W2_using_a_charge_on_a_full_can_returns_true_and_leaves_one_fewer()
    {
        var can = new WateringCan(20);

        var used = can.TryUseCharge();

        Assert.True(used);
        Assert.Equal(can.Capacity - 1, can.Charges);
    }

    [Fact]
    public void BR_W2_using_the_last_charge_succeeds_and_leaves_0()
    {
        var can = new WateringCan(20, 1, false);

        var used = can.TryUseCharge();

        Assert.True(used);
        Assert.Equal(0, can.Charges);
    }

    [Fact]
    public void BR_W2_using_a_charge_on_an_empty_can_returns_false_and_changes_nothing()
    {
        var can = new WateringCan(20, 0, false);

        var used = can.TryUseCharge();

        Assert.False(used);
        Assert.Equal(0, can.Charges);
        Assert.Equal(20, can.Capacity);
    }

    [Fact]
    public void BR_W2_each_use_takes_exactly_one_charge_until_the_can_is_empty()
    {
        var can = new WateringCan(5);

        var results = Enumerable.Range(0, can.Capacity + 1).Select(_ => (Used: can.TryUseCharge(), can.Charges)).ToList();

        Assert.Equal(
            [(true, 4), (true, 3), (true, 2), (true, 1), (true, 0), (false, 0)],
            results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(19)]
    [InlineData(20)]
    public void BR_W3_refill_sets_charges_to_capacity(int charges)
    {
        var can = new WateringCan(20, charges, false);

        can.Refill();

        Assert.Equal(can.Capacity, can.Charges);
    }

    [Fact]
    public void BR_W3_refill_fills_an_upgraded_can_to_its_own_capacity()
    {
        var can = new WateringCan(40, 3, true);

        can.Refill();

        Assert.Equal(40, can.Charges);
        Assert.True(can.Upgraded);
    }

    // ---- #46: the Can upgrade ----

    [Fact]
    public void BR_W4_upgrade_sets_capacity_to_40_and_fills_the_can()
    {
        var can = new WateringCan(20);

        can.Upgrade(40);

        Assert.Equal(40, can.Capacity);
        Assert.Equal(40, can.Charges);
        Assert.True(can.Upgraded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(19)]
    public void BR_W4_upgrade_fills_a_part_empty_can_to_the_new_capacity(int charges)
    {
        var can = new WateringCan(20, charges, false);

        can.Upgrade(40);

        Assert.Equal(40, can.Capacity);
        Assert.Equal(40, can.Charges);
        Assert.True(can.Upgraded);
    }

    [Fact]
    public void AC7_upgrading_an_already_upgraded_can_throws_and_changes_nothing()
    {
        var can = new WateringCan(20);
        can.Upgrade(40);
        can.TryUseCharge();

        Assert.Throws<InvalidOperationException>(() => can.Upgrade(80));
        Assert.Equal(40, can.Capacity);
        Assert.Equal(39, can.Charges);
        Assert.True(can.Upgraded);
    }

    [Fact]
    public void AC7_upgrading_a_restored_upgraded_can_throws()
    {
        var can = new WateringCan(40, 40, true);

        Assert.Throws<InvalidOperationException>(() => can.Upgrade(80));
        Assert.Equal(40, can.Capacity);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(19)]
    [InlineData(0)]
    [InlineData(-1)]
    public void AC7_upgrading_to_a_capacity_not_above_the_current_one_throws_and_changes_nothing(int upgradedCapacity)
    {
        var can = new WateringCan(20, 7, false);

        Assert.Throws<ArgumentOutOfRangeException>(() => can.Upgrade(upgradedCapacity));
        Assert.Equal(20, can.Capacity);
        Assert.Equal(7, can.Charges);
        Assert.False(can.Upgraded);
    }

    [Fact]
    public void BR_W1_charges_stay_within_0_and_the_upgraded_capacity()
    {
        var can = new WateringCan(20, 3, false);

        can.Upgrade(40);
        var uses = Enumerable.Range(0, 41).Count(_ => can.TryUseCharge());
        var emptyCharges = can.Charges;
        can.Refill();
        can.Refill();

        Assert.Equal(40, uses);
        Assert.Equal(0, emptyCharges);
        Assert.Equal(40, can.Charges);
        Assert.InRange(can.Charges, 0, can.Capacity);
    }

    [Fact]
    public void BR_W3_refill_after_the_upgrade_fills_to_the_upgraded_capacity()
    {
        var can = new WateringCan(20);
        can.Upgrade(40);
        Enumerable.Range(0, 30).ToList().ForEach(_ => can.TryUseCharge());

        can.Refill();

        Assert.Equal(40, can.Charges);
    }
}
