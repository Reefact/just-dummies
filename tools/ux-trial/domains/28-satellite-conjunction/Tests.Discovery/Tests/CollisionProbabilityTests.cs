using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class CollisionProbabilityTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(1e-300)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void A_probability_accepts_any_value_from_zero_to_one_inclusive(double value) {
        // Act
        CollisionProbability probability = new CollisionProbability(value);

        // Assert
        Assert.Equal(value, probability.Value);
    }

    [Theory]
    [InlineData(-0.0000001)]
    [InlineData(1.0000001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_probability_refuses_a_value_outside_zero_to_one(double value) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CollisionProbability(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData(0.0, RiskLevel.Low)]
    [InlineData(9.999e-6, RiskLevel.Low)]
    [InlineData(1e-5, RiskLevel.Elevated)]
    [InlineData(9.999e-5, RiskLevel.Elevated)]
    [InlineData(1e-4, RiskLevel.High)]
    [InlineData(0.5, RiskLevel.High)]
    [InlineData(1.0, RiskLevel.High)]
    public void A_probability_is_classified_against_the_operational_thresholds(double value, RiskLevel expected) {
        // Act
        CollisionProbability probability = new CollisionProbability(value);

        // Assert
        Assert.Equal(expected, probability.Level);
    }

    [Fact]
    public void Probabilities_are_ordered_by_value() {
        // Arrange
        CollisionProbability smaller = new CollisionProbability(1e-6);
        CollisionProbability larger  = new CollisionProbability(1e-3);

        // Act / Assert
        Assert.True(smaller.CompareTo(larger) < 0);
        Assert.True(larger.CompareTo(smaller) > 0);
        Assert.Equal(0, smaller.CompareTo(new CollisionProbability(1e-6)));
    }

    [Fact]
    public void Any_probability_sorts_after_nothing() {
        // Arrange
        CollisionProbability anyProbability = Any.CollisionProbability().Generate();

        // Act / Assert
        Assert.True(anyProbability.CompareTo(null) > 0);
    }

    [Fact]
    public void A_probability_is_written_in_scientific_notation() {
        // Arrange
        CollisionProbability probability = new CollisionProbability(0.00025);

        // Act / Assert
        Assert.Equal("2.50E-004", probability.ToString());
    }

}
