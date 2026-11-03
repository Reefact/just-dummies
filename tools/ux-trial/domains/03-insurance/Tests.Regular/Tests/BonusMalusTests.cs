using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class BonusMalusTests {

    [Fact]
    public void A_coefficient_in_range_is_kept() {
        // Arrange
        double anyCoefficient = Any.Double().Between(BonusMalus.Floor, BonusMalus.Ceiling).Generate();

        // Act
        BonusMalus bonusMalus = new BonusMalus(anyCoefficient);

        // Assert
        Assert.Equal(anyCoefficient, bonusMalus.Coefficient);
    }

    [Theory]
    [InlineData(0.50)]
    [InlineData(3.50)]
    public void Both_ends_of_the_range_are_accepted(double coefficient) {
        Assert.Equal(coefficient, new BonusMalus(coefficient).Coefficient);
    }

    [Fact]
    public void The_limits_are_the_documented_ones() {
        Assert.Equal(0.50, BonusMalus.Floor);
        Assert.Equal(3.50, BonusMalus.Ceiling);
    }

    [Fact]
    public void NaN_is_refused_as_not_a_number() {
        Assert.Throws<ArgumentException>(() => new BonusMalus(double.NaN));
    }

    [Fact]
    public void A_coefficient_below_the_floor_is_out_of_range() {
        // Arrange
        double anyTooLow = Any.Double().LessThan(BonusMalus.Floor).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BonusMalus(anyTooLow));
    }

    [Fact]
    public void A_coefficient_above_the_ceiling_is_out_of_range() {
        // Arrange
        double anyTooHigh = Any.Double().GreaterThan(BonusMalus.Ceiling).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BonusMalus(anyTooHigh));
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(3.51)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void The_values_just_outside_and_the_infinities_are_out_of_range(double coefficient) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BonusMalus(coefficient));
    }

    [Fact]
    public void A_new_driver_starts_at_one() {
        Assert.Equal(1.00, BonusMalus.NewDriver.Coefficient);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(-1, -1)]
    public void A_negative_count_of_claims_is_refused(int responsible, int partial) {
        Assert.Throws<ArgumentOutOfRangeException>(() => BonusMalus.NewDriver.AfterYear(responsible, partial));
    }

    [Theory]
    [InlineData(1.00, 0.95)]
    [InlineData(2.00, 1.90)]
    [InlineData(0.52, 0.50)]    // 0.494 rounds down to 0.49, below the floor
    [InlineData(0.50, 0.50)]
    [InlineData(3.50, 3.32)]    // 3.325 is cut toward zero, not rounded up to 3.33
    [InlineData(1.10, 1.04)]    // 1.045 is cut toward zero
    public void A_claim_free_year_earns_a_five_percent_bonus_down_to_the_floor(double before, double expected) {
        BonusMalus after = new BonusMalus(before).AfterYear(0, 0);

        Assert.Equal(expected, after.Coefficient);
    }

    [Fact]
    public void A_claim_free_year_never_raises_the_coefficient_nor_goes_below_the_floor() {
        // Arrange
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();

        // Act
        BonusMalus after = anyBonusMalus.AfterYear(0, 0);

        // Assert
        Assert.True(after.Coefficient <= anyBonusMalus.Coefficient);
        Assert.True(after.Coefficient >= BonusMalus.Floor);
    }

    [Theory]
    [InlineData(1.00, 1, 0, 1.25)]
    [InlineData(1.00, 2, 0, 1.56)]    // 1.5625 cut toward zero
    [InlineData(1.00, 0, 1, 1.12)]    // 1.125 cut toward zero: a partial claim raises, it does not hold
    [InlineData(1.00, 0, 2, 1.26)]    // 1.265625
    [InlineData(1.00, 1, 1, 1.40)]    // 1.40625
    [InlineData(2.00, 1, 0, 2.50)]
    [InlineData(3.00, 1, 0, 3.50)]    // 3.75 capped at the ceiling
    [InlineData(3.50, 0, 1, 3.50)]
    [InlineData(0.50, 1, 0, 0.62)]    // 0.625 cut toward zero
    public void A_year_with_claims_raises_the_coefficient_up_to_the_ceiling(double before, int responsible, int partial, double expected) {
        BonusMalus after = new BonusMalus(before).AfterYear(responsible, partial);

        Assert.Equal(expected, after.Coefficient);
    }

    [Fact]
    public void Many_claims_stop_at_the_ceiling() {
        BonusMalus after = BonusMalus.NewDriver.AfterYear(100, 100);

        Assert.Equal(BonusMalus.Ceiling, after.Coefficient);
    }

    [Fact]
    public void A_year_with_a_claim_of_either_kind_raises_the_coefficient_without_crossing_the_ceiling() {
        // Arrange
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();
        int        anyResponsible = Any.Int32().Between(0, 6).Generate();
        int        anyPartial     = Any.Int32().Between(1, 6).Generate();

        // Act
        BonusMalus after = anyBonusMalus.AfterYear(anyResponsible, anyPartial);

        // Assert
        Assert.True(after.Coefficient >= anyBonusMalus.Coefficient);
        Assert.True(after.Coefficient <= BonusMalus.Ceiling);
    }

    [Fact]
    public void The_original_coefficient_is_left_untouched() {
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();
        double     before        = anyBonusMalus.Coefficient;

        anyBonusMalus.AfterYear(1, 1);

        Assert.Equal(before, anyBonusMalus.Coefficient);
    }

}
