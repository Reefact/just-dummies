namespace ParimutuelBetting.Tests.Discovery;

public sealed class PoolRulesTests {

    public static TheoryData<decimal> TakeoutsInRange => new TheoryData<decimal> { 0m, 0.16m, 0.4999m };

    public static TheoryData<decimal> TakeoutsOutOfRange => new TheoryData<decimal> { -0.01m, 0.5m, 0.75m, 1m };

    public static TheoryData<decimal> BreakageStepsInRange => new TheoryData<decimal> { 0.01m, 0.05m, 0.10m, 1.00m };

    public static TheoryData<decimal> BreakageStepsOutOfRange => new TheoryData<decimal> { 0m, 0.009m, 0.015m, 0.105m, 1.01m, 2m, -0.10m };

    public static TheoryData<decimal> RatiosInRange => new TheoryData<decimal> { 1.00m, 1.10m, 1.50m };

    public static TheoryData<decimal> RatiosOutOfRange => new TheoryData<decimal> { 0m, 0.99m, 1.51m, 2m };

    [Fact]
    public void The_rules_keep_what_they_were_given() {
        // Arrange
        PoolRules rules = new PoolRules(0.16m, 0.10m, 1.05m, 2);

        // Assert
        Assert.Equal(0.16m, rules.TakeoutRate);
        Assert.Equal(0.10m, rules.BreakageStep);
        Assert.Equal(1.05m, rules.MinimumReturnRatio);
        Assert.Equal(2, rules.PlacesPaid);
    }

    [Theory]
    [MemberData(nameof(TakeoutsInRange))]
    public void A_takeout_below_one_half_is_accepted(decimal takeout) {
        PoolRules rules = Any.PoolRules().WithTakeoutRate(takeout).Generate();

        Assert.Equal(takeout, rules.TakeoutRate);
    }

    [Theory]
    [MemberData(nameof(TakeoutsOutOfRange))]
    public void A_negative_takeout_or_one_of_a_half_and_more_is_refused(decimal takeout) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoolRules(takeout, 0.10m, 1.00m, 1));
    }

    [Theory]
    [MemberData(nameof(BreakageStepsInRange))]
    public void A_breakage_step_of_one_cent_to_one_unit_is_accepted(decimal step) {
        PoolRules rules = Any.PoolRules().WithBreakageStep(step).Generate();

        Assert.Equal(step, rules.BreakageStep);
    }

    [Theory]
    [MemberData(nameof(BreakageStepsOutOfRange))]
    public void A_breakage_step_that_is_not_a_whole_number_of_cents_between_one_cent_and_one_unit_is_refused(decimal step) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoolRules(0.16m, step, 1.00m, 1));
    }

    [Theory]
    [MemberData(nameof(RatiosInRange))]
    public void A_minimum_return_between_the_stake_and_one_and_a_half_times_it_is_accepted(decimal ratio) {
        PoolRules rules = Any.PoolRules().WithMinimumReturnRatio(ratio).Generate();

        Assert.Equal(ratio, rules.MinimumReturnRatio);
    }

    [Theory]
    [MemberData(nameof(RatiosOutOfRange))]
    public void A_minimum_return_below_the_stake_or_above_one_and_a_half_times_it_is_refused(decimal ratio) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoolRules(0.16m, 0.10m, ratio, 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void One_to_three_places_can_be_paid(int places) {
        PoolRules rules = Any.PoolRules().WithPlacesPaid(places).Generate();

        Assert.Equal(places, rules.PlacesPaid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void Paying_no_place_or_more_than_three_is_refused(int places) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PoolRules(0.16m, 0.10m, 1.00m, places));
    }

}
