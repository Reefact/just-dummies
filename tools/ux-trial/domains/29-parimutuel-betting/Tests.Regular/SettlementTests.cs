using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class SettlementTests {

    private static readonly IReadOnlyList<Dividend> NoDividends = Array.Empty<Dividend>();

    [Fact]
    public void The_net_pool_is_the_gross_pool_less_refunds_and_deductions() {
        // Act
        Settlement settlement = new Settlement(100m, 20m, 8m, NoDividends, 0m);

        // Assert
        Assert.Equal(100m, settlement.GrossPool);
        Assert.Equal(20m, settlement.Refunded);
        Assert.Equal(8m, settlement.Deductions);
        Assert.Equal(72m, settlement.NetPool);
    }

    [Fact]
    public void An_empty_pool_settles_to_nothing() {
        // Act
        Settlement settlement = new Settlement(0m, 0m, 0m, NoDividends, 0m);

        // Assert
        Assert.Equal(0m, settlement.NetPool);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void Refunds_and_deductions_may_use_up_the_whole_gross_pool() {
        // Act
        Settlement settlement = new Settlement(100m, 60m, 40m, NoDividends, 0m);

        // Assert
        Assert.Equal(0m, settlement.NetPool);
    }

    [Fact]
    public void A_negative_gross_pool_is_refused() {
        // Arrange
        decimal anyNegative = Any.Decimal().Negative().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(anyNegative, 0m, 0m, NoDividends, 0m));
    }

    [Fact]
    public void Refunds_cannot_exceed_the_gross_pool() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 100.01m, 0m, NoDividends, 0m));
    }

    [Fact]
    public void Refunds_cannot_be_negative() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, -0.01m, 0m, NoDividends, 0m));
    }

    [Fact]
    public void Deductions_cannot_exceed_what_refunds_leave_of_the_gross_pool() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 30m, 70.01m, NoDividends, 0m));
    }

    [Fact]
    public void Deductions_cannot_be_negative() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 0m, -1m, NoDividends, 0m));
    }

    [Fact]
    public void The_carryover_cannot_exceed_the_net_pool() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 20m, 10m, NoDividends, 70.01m));
    }

    [Fact]
    public void The_whole_net_pool_may_be_carried_over() {
        // Act
        Settlement settlement = new Settlement(100m, 20m, 10m, NoDividends, 70m);

        // Assert
        Assert.Equal(70m, settlement.Carryover);
        Assert.Equal(0m, settlement.Breakage);
    }

    [Fact]
    public void The_carryover_cannot_be_negative() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Settlement(100m, 0m, 0m, NoDividends, -0.01m));
    }

    [Fact]
    public void A_pool_that_declares_dividends_carries_nothing_over() {
        // Arrange
        Dividend anyDividend = Arbitrary.Dividend().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, new[] { anyDividend }, 1m));
    }

    [Fact]
    public void A_settlement_needs_a_list_of_dividends() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Settlement(100m, 0m, 0m, null!, 0m));
    }

    [Fact]
    public void A_settlement_cannot_hold_a_null_dividend() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Settlement(100m, 0m, 0m, new Dividend[] { null! }, 0m));
    }

    [Fact]
    public void The_total_payout_adds_up_the_payout_of_every_dividend() {
        // Arrange
        Combination anyFirst  = Arbitrary.Combination().Generate();
        Combination anySecond = Arbitrary.Combination().Generate();

        Dividend[] dividends = { new Dividend(anyFirst, 12.40m, 3), new Dividend(anySecond, 2.50m, 10) };

        // Act
        Settlement settlement = Arbitrary.Settlement().WithGrossPool(1000m).WithRefunded(0m).WithDeductions(0m).WithDividends(dividends).Generate();

        // Assert
        Assert.Equal(62.20m, settlement.TotalPayout);
    }

    [Fact]
    public void Nothing_is_paid_out_when_there_is_no_dividend() {
        // Act
        Settlement settlement = new Settlement(100m, 0m, 10m, NoDividends, 90m);

        // Assert
        Assert.Equal(0m, settlement.TotalPayout);
    }

    [Fact]
    public void The_breakage_is_what_rounding_down_kept_of_the_net_pool() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        Dividend[] dividends = { new Dividend(anyCombination, 12.80m, 7) };

        // Act
        Settlement settlement = new Settlement(100m, 0m, 10m, dividends, 0m);

        // Assert
        Assert.Equal(90m, settlement.NetPool);
        Assert.Equal(89.60m, settlement.TotalPayout);
        Assert.Equal(0.40m, settlement.Breakage);
    }

    [Fact]
    public void The_breakage_is_negative_when_the_minimum_return_was_topped_up() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();

        Dividend[] dividends = { new Dividend(anyCombination, 1.20m, 90) };

        // Act
        Settlement settlement = new Settlement(100m, 0m, 25m, dividends, 0m);

        // Assert
        Assert.Equal(-33m, settlement.Breakage);
    }

    [Fact]
    public void The_dividends_cannot_be_changed_through_the_list_they_were_given() {
        // Arrange
        Combination anyCombination = Arbitrary.Combination().Generate();
        List<Dividend> dividends = new List<Dividend> { new Dividend(anyCombination, 1.20m, 90) };
        Settlement     settlement = new Settlement(100m, 0m, 25m, dividends, 0m);

        // Act
        dividends.Clear();

        // Assert
        Assert.Single(settlement.Dividends);
    }

}
