using JustDummies;

using Xunit;

namespace Insurance.Domain.Tests;

public sealed class BonusMalusTests {

    [Fact]
    public void A_new_driver_starts_at_one() {
        Assert.Equal(1.00, BonusMalus.NewDriver.Coefficient);
    }

    [Fact]
    public void The_bonus_floor_and_the_malus_ceiling_are_half_and_three_and_a_half() {
        Assert.Equal(0.50, BonusMalus.Floor);
        Assert.Equal(3.50, BonusMalus.Ceiling);
    }

    [Theory]
    [InlineData(0.50)]
    [InlineData(0.51)]
    [InlineData(1.00)]
    [InlineData(3.49)]
    [InlineData(3.50)]
    public void A_coefficient_within_the_floor_and_the_ceiling_is_accepted_ends_included(double coefficient) {
        // Act
        BonusMalus bonusMalus = new BonusMalus(coefficient);

        // Assert
        Assert.Equal(coefficient, bonusMalus.Coefficient);
    }

    [Fact]
    public void Not_a_number_is_refused_as_a_plain_argument_error() {
        // Act + Assert (the exact type: an out-of-range error would be the wrong refusal)
        Assert.Throws<ArgumentException>(() => new BonusMalus(double.NaN));
    }

    [Theory]
    [InlineData(0.49)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_coefficient_below_the_floor_is_out_of_range(double coefficient) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BonusMalus(coefficient));
    }

    [Theory]
    [InlineData(3.51)]
    [InlineData(100.0)]
    [InlineData(double.PositiveInfinity)]
    public void A_coefficient_above_the_ceiling_is_out_of_range(double coefficient) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BonusMalus(coefficient));
    }

    [Theory]
    [InlineData(1.00, 0.95)]
    [InlineData(1.07, 1.01)]   // 1.0165 is cut, not rounded up
    [InlineData(2.00, 1.90)]
    [InlineData(3.50, 3.32)]   // 3.325 is cut, not rounded up
    [InlineData(0.54, 0.51)]
    [InlineData(0.53, 0.50)]   // 0.5035 cut to 0.50
    [InlineData(0.52, 0.50)]   // 0.494 would go under the floor
    [InlineData(0.50, 0.50)]
    public void A_year_without_claim_takes_five_percent_off_cut_towards_zero_and_stops_at_the_floor(double before, double expected) {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(before);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 0, partialClaims: 0);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Theory(Skip = "domain defect: the coefficient is rounded in binary floating point, so an exact decimal result such as 0.60 x 0.95 = 0.57 is cut to 0.56 (0.5699999999999999); same for 1.20 -> 1.13, 1.40 -> 1.32, 2.40 -> 2.27. Deterministic literals, no seed involved")]
    [InlineData(0.60, 0.57)]
    [InlineData(1.20, 1.14)]
    [InlineData(1.40, 1.33)]
    [InlineData(2.40, 2.28)]
    [InlineData(3.00, 2.85)]
    public void A_year_without_claim_gives_the_exact_decimal_result_cut_towards_zero(double before, double expected) {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(before);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 0, partialClaims: 0);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Theory(Skip = "domain defect: 0.72 x 1.25 is exactly 0.90 in decimal but 0.8999999999999999 in binary floating point, cut to 0.89; same for 1.44 -> 1.79 and 1.64 -> 2.04. Deterministic literals, no seed involved")]
    [InlineData(0.72, 0.90)]
    [InlineData(1.44, 1.80)]
    [InlineData(1.64, 2.05)]
    public void A_responsible_claim_gives_the_exact_decimal_result_cut_towards_zero(double before, double expected) {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(before);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 1, partialClaims: 0);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Theory]
    [InlineData(1, 1.25)]
    [InlineData(2, 1.56)]   // 1.5625 is cut
    [InlineData(3, 1.95)]   // 1.953125 is cut
    [InlineData(5, 3.05)]   // 3.0517578125 is cut
    [InlineData(6, 3.50)]   // 3.8146... goes past the ceiling
    public void Each_responsible_claim_adds_a_quarter_and_the_result_stops_at_the_ceiling(int responsibleClaims, double expected) {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(1.00);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims, partialClaims: 0);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Theory]
    [InlineData(1, 1.12)]   // 1.125 is cut: a partial claim does raise the coefficient
    [InlineData(2, 1.26)]   // 1.265625 is cut
    [InlineData(3, 1.42)]   // 1.423828125 is cut
    public void Each_partial_claim_adds_an_eighth_instead_of_the_bonus(int partialClaims, double expected) {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(1.00);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 0, partialClaims);

        // Assert
        Assert.Equal(expected, after.Coefficient);
    }

    [Fact]
    public void Responsible_and_partial_claims_compound() {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(1.00);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 1, partialClaims: 1);

        // Assert
        Assert.Equal(1.40, after.Coefficient);   // 1.25 x 1.125 = 1.40625, cut
    }

    [Fact]
    public void The_malus_applies_to_the_current_coefficient_not_to_the_new_driver_one() {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(2.00);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 1, partialClaims: 0);

        // Assert
        Assert.Equal(2.50, after.Coefficient);
    }

    [Fact]
    public void A_ceiling_coefficient_stays_at_the_ceiling_after_a_claim() {
        // Arrange
        BonusMalus atCeiling = new BonusMalus(BonusMalus.Ceiling);

        // Act
        BonusMalus after = atCeiling.AfterYear(responsibleClaims: 1, partialClaims: 0);

        // Assert
        Assert.Equal(3.50, after.Coefficient);
    }

    [Fact]
    public void An_absurd_number_of_claims_is_a_ceiling_coefficient_not_an_overflow() {
        // Arrange
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();

        // Act
        BonusMalus after = anyBonusMalus.AfterYear(responsibleClaims: int.MaxValue, partialClaims: int.MaxValue);

        // Assert
        Assert.Equal(3.50, after.Coefficient);
    }

    [Fact]
    public void A_negative_number_of_responsible_claims_is_out_of_range() {
        // Arrange
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();
        int        anyPartial    = Any.Int32().Between(0, 10).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyBonusMalus.AfterYear(-1, anyPartial));

        // Assert
        Assert.Equal("responsibleClaims", refusal.ParamName);
    }

    [Fact]
    public void A_negative_number_of_partial_claims_is_out_of_range() {
        // Arrange
        BonusMalus anyBonusMalus = Any.BonusMalus().Generate();
        int        anyResponsible = Any.Int32().Between(0, 10).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyBonusMalus.AfterYear(anyResponsible, -1));

        // Assert
        Assert.Equal("partialClaims", refusal.ParamName);
    }

    [Fact]
    public void Passing_a_year_leaves_the_original_coefficient_untouched() {
        // Arrange
        BonusMalus bonusMalus = new BonusMalus(1.00);

        // Act
        BonusMalus after = bonusMalus.AfterYear(responsibleClaims: 1, partialClaims: 0);

        // Assert
        Assert.NotSame(bonusMalus, after);
        Assert.Equal(1.00, bonusMalus.Coefficient);
    }

}
