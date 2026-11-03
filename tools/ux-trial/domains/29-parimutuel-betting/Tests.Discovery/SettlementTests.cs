namespace ParimutuelBetting.Tests.Discovery;

public sealed class SettlementTests {

    private static Dividend DividendOf(decimal amountPerUnit, int winningUnits) {
        return Any.Dividend().WithAmountPerUnit(amountPerUnit).WithWinningUnits(winningUnits).Generate();
    }

    [Fact]
    public void The_net_pool_is_what_remains_after_refunds_and_deductions() {
        Settlement settlement = new Settlement(1000m, 100m, 135m, Array.Empty<Dividend>(), 0m);

        Assert.Equal(765m, settlement.NetPool);
        Assert.Equal(1000m, settlement.GrossPool);
        Assert.Equal(100m, settlement.Refunded);
        Assert.Equal(135m, settlement.Deductions);
    }

    [Fact]
    public void A_pool_nobody_won_carries_its_whole_net_pool_over() {
        Settlement settlement = new Settlement(1000m, 100m, 135m, Array.Empty<Dividend>(), 765m);

        Assert.Equal(765m, settlement.Carryover);
        Assert.Equal(0m, settlement.TotalPayout);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void A_pool_that_carried_over_only_part_of_its_net_pool_keeps_the_rest_as_breakage() {
        Settlement settlement = new Settlement(1000m, 0m, 100m, Array.Empty<Dividend>(), 800m);

        Assert.Equal(100m, settlement.Breakage);
    }

    [Fact]
    public void The_total_payout_adds_every_dividend_and_breakage_is_what_rounding_kept() {
        // Arrange
        List<Dividend> dividends = new List<Dividend> { DividendOf(25.70m, 7), DividendOf(3.10m, 20) };

        // Act
        Settlement settlement = new Settlement(300m, 0m, 30m, dividends, 0m);

        // Assert
        Assert.Equal(25.70m * 7 + 3.10m * 20, settlement.TotalPayout);
        Assert.Equal(270m - 241.9m, settlement.Breakage);
    }

    [Fact]
    public void Breakage_is_negative_when_the_minimum_return_was_topped_up() {
        Settlement settlement = new Settlement(100m, 0m, 20m, new[] { DividendOf(1.00m, 100) }, 0m);

        Assert.Equal(-20m, settlement.Breakage);
    }

    [Fact]
    public void A_settlement_does_not_follow_the_list_of_dividends_it_was_built_from() {
        List<Dividend> dividends = new List<Dividend> { DividendOf(2.00m, 5) };

        Settlement settlement = new Settlement(100m, 0m, 0m, dividends, 0m);
        dividends.Clear();

        Assert.Single(settlement.Dividends);
    }

    [Fact]
    public void A_negative_gross_pool_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(-0.01m, 0m, 0m, Array.Empty<Dividend>(), 0m));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100.01")]
    public void Refunds_lie_between_nothing_and_the_gross_pool(string refunded) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, decimal.Parse(refunded, System.Globalization.CultureInfo.InvariantCulture), 0m, Array.Empty<Dividend>(), 0m));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("60.01")]
    public void Deductions_lie_between_nothing_and_the_pool_left_after_refunds(string deductions) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 40m, decimal.Parse(deductions, System.Globalization.CultureInfo.InvariantCulture), Array.Empty<Dividend>(), 0m));
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("50.01")]
    public void The_carryover_lies_between_nothing_and_the_net_pool(string carryover) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 40m, 10m, Array.Empty<Dividend>(), decimal.Parse(carryover, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Everything_may_be_refunded_or_deducted() {
        Settlement allRefunded = new Settlement(100m, 100m, 0m, Array.Empty<Dividend>(), 0m);
        Settlement allDeducted = new Settlement(100m, 0m, 100m, Array.Empty<Dividend>(), 0m);
        Settlement empty       = new Settlement(0m, 0m, 0m, Array.Empty<Dividend>(), 0m);

        Assert.Equal(0m, allRefunded.NetPool);
        Assert.Equal(0m, allDeducted.NetPool);
        Assert.Equal(0m, empty.NetPool);
    }

    [Fact]
    public void A_settlement_without_a_list_of_dividends_or_with_a_null_dividend_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Settlement(100m, 0m, 0m, null!, 0m));
        Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, new Dividend[] { null! }, 0m));
    }

    [Fact]
    public void A_pool_that_declares_dividends_carries_nothing_over() {
        Dividend[] dividends = { DividendOf(2.00m, 5) };

        Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, dividends, 1m));
    }

}
