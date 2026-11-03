using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class SettlementTests {

    [Fact]
    public void The_net_pool_is_what_is_left_after_refunds_and_deductions() {
        Settlement settlement = new Settlement(grossPool: 1000m, refunded: 100m, deductions: 180m, dividends: Array.Empty<Dividend>(), carryover: 720m);

        Assert.Equal(720m, settlement.NetPool);
    }

    [Fact]
    public void An_undistributed_pool_carries_its_whole_net_over_without_breakage() {
        Settlement settlement = new Settlement(1000m, 100m, 180m, Array.Empty<Dividend>(), 720m);

        Assert.Equal(0m, settlement.TotalPayout);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void Breakage_is_the_net_pool_kept_by_rounding_down() {
        Combination anyCombination = Any.Combination().Generate();
        Dividend    dividend       = new Dividend(anyCombination, 7.00m, 100);

        Settlement settlement = new Settlement(1000m, 100m, 180m, new[] { dividend }, 0m);

        Assert.Equal(700m, settlement.TotalPayout);
        Assert.Equal(20m, settlement.Breakage);
    }

    [Fact]
    public void Breakage_is_negative_when_the_minimum_return_was_topped_up() {
        Combination anyCombination = Any.Combination().Generate();
        Dividend    dividend       = new Dividend(anyCombination, 8.00m, 100);

        Settlement settlement = new Settlement(1000m, 100m, 180m, new[] { dividend }, 0m);

        Assert.Equal(800m, settlement.TotalPayout);
        Assert.Equal(-80m, settlement.Breakage);
    }

    [Fact]
    public void The_total_payout_adds_up_every_dividend() {
        Dividend first  = new Dividend(Tickets.Bet(BetType.Place, 4), 5.00m, 10);
        Dividend second = new Dividend(Tickets.Bet(BetType.Place, 9), 1.25m, 40);

        Settlement settlement = new Settlement(100m, 0m, 0m, new[] { first, second }, 0m);

        Assert.Equal(100m, settlement.TotalPayout);
    }

    [Fact]
    public void A_pool_with_nothing_in_it_settles_to_nothing() {
        Settlement settlement = new Settlement(0m, 0m, 0m, Array.Empty<Dividend>(), 0m);

        Assert.Equal(0m, settlement.NetPool);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void Refunds_may_return_the_whole_pool() {
        Settlement settlement = new Settlement(100m, 100m, 0m, Array.Empty<Dividend>(), 0m);

        Assert.Equal(0m, settlement.NetPool);
    }

    [Fact]
    public void Deductions_may_take_everything_the_refunds_left() {
        Settlement settlement = new Settlement(100m, 40m, 60m, Array.Empty<Dividend>(), 0m);

        Assert.Equal(0m, settlement.NetPool);
    }

    [Fact]
    public void The_carryover_may_be_the_whole_net_pool() {
        Settlement settlement = new Settlement(100m, 0m, 10m, Array.Empty<Dividend>(), 90m);

        Assert.Equal(90m, settlement.Carryover);
    }

    [Fact]
    public void A_negative_gross_pool_is_refused() {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(-0.01m, 0m, 0m, Array.Empty<Dividend>(), 0m));

        Assert.Equal("grossPool", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("100.01")]
    public void Refunds_outside_zero_to_the_gross_pool_are_refused(string refunded) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement(100m, decimal.Parse(refunded, System.Globalization.CultureInfo.InvariantCulture), 0m, Array.Empty<Dividend>(), 0m));

        Assert.Equal("refunded", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("60.01")]
    public void Deductions_outside_zero_to_what_the_refunds_left_are_refused(string deductions) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement(100m, 40m, decimal.Parse(deductions, System.Globalization.CultureInfo.InvariantCulture), Array.Empty<Dividend>(), 0m));

        Assert.Equal("deductions", refusal.ParamName);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("90.01")]
    public void A_carryover_outside_zero_to_the_net_pool_is_refused(string carryover) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Settlement(100m, 0m, 10m, Array.Empty<Dividend>(), decimal.Parse(carryover, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal("carryover", refusal.ParamName);
    }

    [Fact]
    public void A_pool_that_declares_dividends_carries_nothing_over() {
        Dividend anyDividend = Any.Dividend().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, new[] { anyDividend }, 0.01m));

        Assert.Equal("carryover", refusal.ParamName);
    }

    [Fact]
    public void A_settlement_needs_a_list_of_dividends() {
        Assert.Throws<ArgumentNullException>(() => new Settlement(100m, 0m, 0m, null!, 0m));
    }

    [Fact]
    public void A_settlement_cannot_hold_a_null_dividend() {
        Dividend anyDividend = Any.Dividend().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, new Dividend[] { anyDividend, null! }, 0m));

        Assert.Equal("dividends", refusal.ParamName);
    }

    [Fact]
    public void A_settlement_does_not_follow_the_list_of_dividends_it_was_built_from() {
        List<Dividend> declared = new List<Dividend> { new Dividend(Tickets.Bet(BetType.Win, 3), 2.00m, 5) };

        Settlement settlement = new Settlement(10m, 0m, 0m, declared, 0m);
        declared.Clear();

        Assert.Single(settlement.Dividends);
    }

}
