using SatelliteConjunction.Domain;

namespace SatelliteConjunction.Tests.Expert.Tests;

public sealed class CollisionProbabilityTests {

    [Theory]
    [InlineData(1e-4, RiskLevel.High)]
    [InlineData(3e-2, RiskLevel.High)]
    [InlineData(9.9e-5, RiskLevel.Elevated)]
    [InlineData(1e-5, RiskLevel.Elevated)]
    [InlineData(9.9e-6, RiskLevel.Low)]
    [InlineData(0, RiskLevel.Low)]
    public void A_probability_is_classified_against_the_operational_thresholds(double value, RiskLevel expected) {
        // Arrange
        CollisionProbability probability = new(value);

        // Act
        RiskLevel level = probability.Level;

        // Assert
        Assert.Equal(expected, level);
    }

    [Theory]
    [InlineData(-1e-9)]
    [InlineData(1.000001)]
    [InlineData(double.NaN)]
    public void A_value_outside_zero_to_one_is_not_a_probability(double value) {
        // Arrange

        // Act
        Action creating = () => new CollisionProbability(value);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("value", creating);
    }

}
