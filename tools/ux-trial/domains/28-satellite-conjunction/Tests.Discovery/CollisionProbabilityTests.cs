using System;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class CollisionProbabilityTests {

    [Theory]
    [InlineData(3e-3, RiskLevel.High)]
    [InlineData(1e-4, RiskLevel.High)]
    [InlineData(4e-5, RiskLevel.Elevated)]
    [InlineData(1e-5, RiskLevel.Elevated)]
    [InlineData(2e-7, RiskLevel.Low)]
    public void A_probability_is_classified_against_the_operational_thresholds(double value, RiskLevel expected) {
        // Arrange
        CollisionProbability probability = new(value);

        // Act
        RiskLevel level = probability.Level;

        // Assert
        Assert.Equal(expected, level);
    }

    [Fact]
    public void A_probability_above_one_is_refused() {
        // Arrange
        double impossible = 1.2;

        // Act
        Action creation = () => _ = new CollisionProbability(impossible);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(creation);
    }

}
