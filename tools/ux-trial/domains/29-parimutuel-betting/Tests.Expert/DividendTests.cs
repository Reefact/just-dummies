using ParimutuelBetting.Domain.Tests.Expert.Generators;
using ParimutuelBetting.Domain.Tests.Expert.Support;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class DividendTests {

    public static TheoryData<decimal> AmountsThatAreNotPositiveCents => new TheoryData<decimal> { 0m, -0.01m, -5m, 0.001m, 1.005m, 12.345m };

    [Fact]
    public void The_payout_is_the_amount_per_unit_times_the_winning_units() {
        Combination anyCombination = Any.Combination().Generate();

        Dividend dividend = new Dividend(anyCombination, 12.30m, 7);

        Assert.Equal(86.10m, dividend.Payout);
    }

    [Fact]
    public void The_smallest_dividend_is_one_cent_on_one_unit() {
        Dividend dividend = Any.Dividend().WithAmountPerUnit(0.01m).WithWinningUnits(1).Generate();

        Assert.Equal(0.01m, dividend.Payout);
    }

    [Theory]
    [MemberData(nameof(AmountsThatAreNotPositiveCents))]
    public void A_dividend_is_a_positive_whole_number_of_cents(decimal amountPerUnit) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Dividend().WithAmountPerUnit(amountPerUnit).Generate());

        Assert.Equal("amountPerUnit", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_dividend_is_declared_only_for_a_combination_with_winning_units(int winningUnits) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Dividend().WithWinningUnits(winningUnits).Generate());

        Assert.Equal("winningUnits", refusal.ParamName);
    }

    [Fact]
    public void A_dividend_needs_a_combination() {
        Assert.Throws<ArgumentNullException>(() => new Dividend(null!, 1.00m, 1));
    }

}
