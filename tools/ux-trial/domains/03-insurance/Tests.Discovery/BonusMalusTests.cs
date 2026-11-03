using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests.Discovery;

public sealed class BonusMalusTests {

    [Theory]
    [InlineData(0.50)]
    [InlineData(1.00)]
    [InlineData(1.37)]
    [InlineData(3.50)]
    public void A_coefficient_within_the_bounds_is_kept(double coefficient) {
        Assert.Equal(coefficient, new BonusMalus(coefficient).Coefficient);
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_coefficient_below_the_floor_is_refused(double coefficient) {
        Assert.Throws<ArgumentOutOfRangeException>("coefficient", () => new BonusMalus(coefficient));
    }

    [Theory]
    [InlineData(3.51)]
    [InlineData(100.0)]
    [InlineData(double.PositiveInfinity)]
    public void A_coefficient_above_the_ceiling_is_refused(double coefficient) {
        Assert.Throws<ArgumentOutOfRangeException>("coefficient", () => new BonusMalus(coefficient));
    }

    [Fact]
    public void Not_a_number_is_refused_as_an_invalid_argument() {
        Assert.Throws<ArgumentException>("coefficient", () => new BonusMalus(double.NaN));
    }

    [Fact]
    public void A_new_driver_starts_at_one() {
        Assert.Equal(1.00, BonusMalus.NewDriver.Coefficient);
    }

    [Theory]
    [InlineData(1.00, 0.95)]
    [InlineData(0.95, 0.90)]     // 0.9025 truncated
    [InlineData(2.00, 1.90)]
    [InlineData(3.50, 3.32)]     // 3.325 truncated, not rounded up
    [InlineData(0.52, 0.50)]     // 0.494 falls under the floor
    [InlineData(0.50, 0.50)]
    public void A_claim_free_year_lowers_the_coefficient_by_five_percent_down_to_the_floor(double before, double expected) {
        // Act
        BonusMalus after = new BonusMalus(before).AfterYear(responsibleClaims: 0, partialClaims: 0);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Theory]
    [InlineData(1.00, 1, 0, 1.25)]
    [InlineData(1.00, 2, 0, 1.56)]     // 1.5625 truncated
    [InlineData(2.00, 2, 0, 3.12)]     // 3.125 truncated
    [InlineData(1.00, 0, 1, 1.12)]     // 1.125 truncated
    [InlineData(1.00, 0, 2, 1.26)]     // 1.265625 truncated
    [InlineData(1.00, 1, 1, 1.40)]     // 1.40625 truncated
    [InlineData(0.50, 1, 0, 0.62)]     // 0.625 truncated
    public void Claims_raise_the_coefficient(double before, int responsible, int partial, double expected) {
        // Act
        BonusMalus after = new BonusMalus(before).AfterYear(responsible, partial);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Fact]
    public void A_year_with_only_a_partial_claim_is_a_malus_not_a_bonus() {
        // Act
        BonusMalus after = new BonusMalus(1.00).AfterYear(responsibleClaims: 0, partialClaims: 1);

        // Assert
        Assert.True(after.Coefficient > 1.00);
    }

    [Theory]
    [InlineData(3.00, 1, 0)]
    [InlineData(1.00, 10, 0)]
    [InlineData(3.50, 0, 1)]
    public void The_coefficient_never_goes_past_the_ceiling(double before, int responsible, int partial) {
        // Act
        BonusMalus after = new BonusMalus(before).AfterYear(responsible, partial);

        // Assert
        Assert.Equal(BonusMalus.Ceiling, after.Coefficient);
    }

    [Fact]
    public void A_negative_count_of_responsible_claims_is_refused() {
        // Arrange
        BonusMalus anyBonusMalus = new AnyBonusMalus().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("responsibleClaims", () => anyBonusMalus.AfterYear(-1, 0));
    }

    [Fact]
    public void A_negative_count_of_partial_claims_is_refused() {
        // Arrange
        BonusMalus anyBonusMalus = new AnyBonusMalus().Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("partialClaims", () => anyBonusMalus.AfterYear(0, -1));
    }

    [Fact]
    public void Advancing_a_year_leaves_the_original_untouched() {
        // Arrange
        BonusMalus before = new BonusMalus(1.50);

        // Act
        before.AfterYear(1, 1);

        // Assert
        Assert.Equal(1.50, before.Coefficient);
    }

}
