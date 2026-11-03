using ParimutuelBetting.Domain.Tests.Expert.Generators;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class PoolRulesTests {

    public static TheoryData<decimal> TakeoutRatesInRange => new TheoryData<decimal> { 0m, 0.15m, 0.4999m };

    public static TheoryData<decimal> TakeoutRatesOutOfRange => new TheoryData<decimal> { -0.0001m, 0.5m, 0.75m, 1m };

    public static TheoryData<decimal> BreakageStepsInRange => new TheoryData<decimal> { 0.01m, 0.10m, 1.00m };

    public static TheoryData<decimal> BreakageStepsOutOfRange => new TheoryData<decimal> { 0m, -0.01m, 1.01m, 0.005m, 0.015m, 0.001m };

    public static TheoryData<decimal> MinimumReturnsInRange => new TheoryData<decimal> { 1.00m, 1.10m, 1.50m };

    public static TheoryData<decimal> MinimumReturnsOutOfRange => new TheoryData<decimal> { 0.99m, 0m, 1.51m, 2m };

    [Theory]
    [MemberData(nameof(TakeoutRatesInRange))]
    public void A_takeout_below_one_half_is_accepted(decimal takeoutRate) {
        PoolRules rules = Any.PoolRules().WithTakeoutRate(takeoutRate).Generate();

        Assert.Equal(takeoutRate, rules.TakeoutRate);
    }

    [Theory]
    [MemberData(nameof(TakeoutRatesOutOfRange))]
    public void A_takeout_that_is_negative_or_half_or_more_is_refused(decimal takeoutRate) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PoolRules().WithTakeoutRate(takeoutRate).Generate());

        Assert.Equal("takeoutRate", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(BreakageStepsInRange))]
    public void A_breakage_step_of_one_cent_to_one_unit_is_accepted(decimal breakageStep) {
        PoolRules rules = Any.PoolRules().WithBreakageStep(breakageStep).Generate();

        Assert.Equal(breakageStep, rules.BreakageStep);
    }

    [Theory]
    [MemberData(nameof(BreakageStepsOutOfRange))]
    public void A_breakage_step_that_is_not_a_whole_number_of_cents_in_range_is_refused(decimal breakageStep) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PoolRules().WithBreakageStep(breakageStep).Generate());

        Assert.Equal("breakageStep", refusal.ParamName);
    }

    [Theory]
    [MemberData(nameof(MinimumReturnsInRange))]
    public void A_minimum_return_from_the_stake_back_to_one_and_a_half_times_it_is_accepted(decimal ratio) {
        PoolRules rules = Any.PoolRules().WithMinimumReturnRatio(ratio).Generate();

        Assert.Equal(ratio, rules.MinimumReturnRatio);
    }

    [Theory]
    [MemberData(nameof(MinimumReturnsOutOfRange))]
    public void A_minimum_return_below_the_stake_or_above_one_and_a_half_times_it_is_refused(decimal ratio) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PoolRules().WithMinimumReturnRatio(ratio).Generate());

        Assert.Equal("minimumReturnRatio", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void A_pool_pays_one_to_three_places(int placesPaid) {
        PoolRules rules = Any.PoolRules().WithPlacesPaid(placesPaid).Generate();

        Assert.Equal(placesPaid, rules.PlacesPaid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void A_pool_cannot_pay_no_place_nor_more_than_three(int placesPaid) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.PoolRules().WithPlacesPaid(placesPaid).Generate());

        Assert.Equal("placesPaid", refusal.ParamName);
    }

}
