using JustDummies;

using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class PoolRulesTests {

    [Fact]
    public void Rules_keep_the_values_they_were_given() {
        // Act
        PoolRules rules = new PoolRules(0.16m, 0.10m, 1.05m, 2);

        // Assert
        Assert.Equal(0.16m, rules.TakeoutRate);
        Assert.Equal(0.10m, rules.BreakageStep);
        Assert.Equal(1.05m, rules.MinimumReturnRatio);
        Assert.Equal(2, rules.PlacesPaid);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.4999")]
    public void A_takeout_from_nothing_up_to_just_below_half_is_accepted(string rate) {
        // Act
        PoolRules rules = Arbitrary.PoolRules().WithTakeoutRate(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture)).Generate();

        // Assert
        Assert.Equal(decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture), rules.TakeoutRate);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("0.5")]
    [InlineData("0.9")]
    public void A_takeout_below_zero_or_from_half_is_refused(string rate) {
        // Arrange
        decimal takeoutRate = decimal.Parse(rate, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PoolRules().WithTakeoutRate(takeoutRate).Generate());
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("0.05")]
    [InlineData("1.00")]
    public void A_breakage_step_of_a_cent_to_a_unit_is_accepted(string step) {
        // Arrange
        decimal breakageStep = decimal.Parse(step, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        PoolRules rules = Arbitrary.PoolRules().WithBreakageStep(breakageStep).Generate();

        // Assert
        Assert.Equal(breakageStep, rules.BreakageStep);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.005")]
    [InlineData("0.015")]
    [InlineData("0.101")]
    [InlineData("1.01")]
    [InlineData("-0.10")]
    public void A_breakage_step_that_is_not_a_whole_number_of_cents_between_1_cent_and_1_is_refused(string step) {
        // Arrange
        decimal breakageStep = decimal.Parse(step, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PoolRules().WithBreakageStep(breakageStep).Generate());
    }

    [Theory]
    [InlineData("1.00")]
    [InlineData("1.50")]
    public void A_minimum_return_ratio_from_1_to_1_and_a_half_is_accepted(string ratio) {
        // Arrange
        decimal minimumReturnRatio = decimal.Parse(ratio, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        PoolRules rules = Arbitrary.PoolRules().WithMinimumReturnRatio(minimumReturnRatio).Generate();

        // Assert
        Assert.Equal(minimumReturnRatio, rules.MinimumReturnRatio);
    }

    [Theory]
    [InlineData("0.99")]
    [InlineData("1.51")]
    [InlineData("0")]
    public void A_minimum_return_ratio_outside_1_to_1_and_a_half_is_refused(string ratio) {
        // Arrange
        decimal minimumReturnRatio = decimal.Parse(ratio, System.Globalization.CultureInfo.InvariantCulture);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PoolRules().WithMinimumReturnRatio(minimumReturnRatio).Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void One_to_three_paid_places_are_accepted(int placesPaid) {
        // Act
        PoolRules rules = Arbitrary.PoolRules().WithPlacesPaid(placesPaid).Generate();

        // Assert
        Assert.Equal(placesPaid, rules.PlacesPaid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void Fewer_than_one_or_more_than_three_paid_places_are_refused(int placesPaid) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.PoolRules().WithPlacesPaid(placesPaid).Generate());
    }

}
