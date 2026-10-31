using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class VoltageLimitsTests {

    [Theory]
    [InlineData(0.95, true)]
    [InlineData(1.05, true)]
    [InlineData(0.9499, false)]
    [InlineData(1.0501, false)]
    public void The_band_is_inclusive_at_both_ends(double magnitudePu, bool expected) {
        // Arrange
        VoltageLimits limits = new(0.95, 1.05);

        // Act
        bool contains = limits.Contains(magnitudePu);

        // Assert
        Assert.Equal(expected, contains);
    }

    [Fact]
    public void A_band_that_does_not_hold_nominal_voltage_is_refused() {
        // Arrange
        double anyMaximumPu = Any.Double().GreaterThan(1.0).LessThanOrEqualTo(VoltageLimits.AbsoluteMaximumPu).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new VoltageLimits(1.02, anyMaximumPu));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

}
