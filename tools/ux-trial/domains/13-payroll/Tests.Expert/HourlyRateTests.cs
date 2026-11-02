namespace Payroll.Tests.Expert;

public sealed class HourlyRateTests {

    [Fact]
    public void The_ceiling_of_an_hourly_rate_is_500_euros() {
        Assert.Equal(500m, HourlyRate.Ceiling);
    }

    [Theory]
    [InlineData(0.01)]     // the smallest amount in whole cents
    [InlineData(12.50)]
    [InlineData(499.99)]
    [InlineData(500.00)]   // the ceiling itself
    public void An_hourly_rate_in_whole_cents_up_to_the_ceiling_is_accepted(double euros) {
        // Arrange
        decimal rate = (decimal)euros;

        // Act
        HourlyRate hourlyRate = new HourlyRate(rate);

        // Assert
        Assert.Equal(rate, hourlyRate.Value);
    }

    [Fact]
    public void An_hourly_rate_of_zero_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(0m));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_negative_hourly_rate_is_refused() {
        // Arrange
        decimal anyNegativeRate = Any.Decimal().Negative().WithScale(2).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(anyNegativeRate));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void An_hourly_rate_one_cent_above_the_ceiling_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(500.01m));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void An_hourly_rate_far_above_the_ceiling_is_refused() {
        // Arrange
        decimal anyRateAboveTheCeiling = Any.Decimal().GreaterThan(HourlyRate.Ceiling).WithScale(2).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new HourlyRate(anyRateAboveTheCeiling));
    }

    [Theory]
    [InlineData(12.345)]   // a fraction of a cent
    [InlineData(0.001)]    // positive, but below a cent
    [InlineData(499.999)]  // below the ceiling, with a fraction of a cent
    public void An_hourly_rate_with_a_fraction_of_a_cent_is_refused(double euros) {
        // Arrange
        decimal rate = (decimal)euros;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new HourlyRate(rate));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void An_hourly_rate_written_with_trailing_zeros_is_still_in_whole_cents() {
        // Act
        HourlyRate hourlyRate = new HourlyRate(12.500000m);

        // Assert
        Assert.Equal(12.5m, hourlyRate.Value);
    }

}
