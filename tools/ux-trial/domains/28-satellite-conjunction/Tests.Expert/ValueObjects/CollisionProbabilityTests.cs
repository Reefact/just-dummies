using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class CollisionProbabilityTests {

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(0.5)]
    public void A_value_from_zero_to_one_inclusive_is_kept_as_it_is(double value) {
        // Act
        CollisionProbability probability = new(value);

        // Assert
        Assert.Equal(value, probability.Value);
    }

    [Fact]
    public void A_value_just_above_one_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CollisionProbability(Math.BitIncrement(1)));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_value_just_below_zero_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollisionProbability(Math.BitDecrement(0)));
    }

    [Fact]
    public void Not_a_number_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollisionProbability(double.NaN));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void An_infinity_is_refused(double infinity) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new CollisionProbability(infinity));
    }

    [Fact]
    public void The_thresholds_are_one_in_ten_thousand_for_high_and_one_in_a_hundred_thousand_for_elevated() {
        // Assert
        Assert.Equal(1e-4, CollisionProbability.HighThreshold);
        Assert.Equal(1e-5, CollisionProbability.ElevatedThreshold);
    }

    [Fact]
    public void A_probability_at_the_high_threshold_is_high() {
        // Act & Assert
        Assert.Equal(RiskLevel.High, new CollisionProbability(1e-4).Level);
    }

    [Fact]
    public void A_probability_just_under_the_high_threshold_is_only_elevated() {
        // Act & Assert
        Assert.Equal(RiskLevel.Elevated, new CollisionProbability(Math.BitDecrement(1e-4)).Level);
    }

    [Fact]
    public void A_probability_at_the_elevated_threshold_is_elevated() {
        // Act & Assert
        Assert.Equal(RiskLevel.Elevated, new CollisionProbability(1e-5).Level);
    }

    [Fact]
    public void A_probability_just_under_the_elevated_threshold_is_low() {
        // Act & Assert
        Assert.Equal(RiskLevel.Low, new CollisionProbability(Math.BitDecrement(1e-5)).Level);
    }

    [Fact]
    public void A_zero_probability_is_low() {
        // Act & Assert
        Assert.Equal(RiskLevel.Low, new CollisionProbability(0).Level);
    }

    [Fact]
    public void A_certain_collision_is_high() {
        // Act & Assert
        Assert.Equal(RiskLevel.High, new CollisionProbability(1).Level);
    }

    [Fact]
    public void A_smaller_probability_is_ordered_before_a_larger_one() {
        // Arrange
        CollisionProbability small = new(0.2);
        CollisionProbability large = new(0.7);

        // Act & Assert
        Assert.True(small.CompareTo(large) < 0);
        Assert.True(large.CompareTo(small) > 0);
    }

    [Fact]
    public void Two_probabilities_of_the_same_value_compare_as_equal() {
        // Arrange
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();
        CollisionProbability sameValue      = new(anyProbability.Value);

        // Act & Assert
        Assert.Equal(0, anyProbability.CompareTo(sameValue));
    }

    [Fact]
    public void Any_probability_is_ordered_after_nothing() {
        // Arrange
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();

        // Act & Assert
        Assert.True(anyProbability.CompareTo(null) > 0);
    }

    [Fact]
    public void A_probability_is_written_in_scientific_notation_with_two_decimals() {
        // Act & Assert
        Assert.Equal("1.23E-004", new CollisionProbability(0.000123).ToString());
    }

}
