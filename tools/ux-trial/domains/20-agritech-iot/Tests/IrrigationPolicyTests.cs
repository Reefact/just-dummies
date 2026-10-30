using AgriTech.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace AgriTech.Tests;

[Reproducible]
public sealed class IrrigationPolicyTests {

    [Fact]
    public void Irrigates_when_the_average_humidity_is_below_the_dry_threshold() {
        // Arrange
        IrrigationPolicy policy = new IrrigationPolicy(dryThresholdPercent: 35.0, minimumReadings: 2);
        ReadingBatch batch = new AnyReadingBatch().WithReadings([
            new AnySensorReading().WithHumidity(new RelativeHumidity(20.0)).Generate(),
            new AnySensorReading().WithHumidity(new RelativeHumidity(30.0)).Generate()
        ]).Generate();

        // Act / Assert
        Assert.True(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void Does_not_irrigate_when_the_average_humidity_reaches_the_threshold() {
        IrrigationPolicy policy = new IrrigationPolicy(dryThresholdPercent: 35.0, minimumReadings: 2);
        ReadingBatch batch = new AnyReadingBatch().WithReadings([
            new AnySensorReading().WithHumidity(new RelativeHumidity(30.0)).Generate(),
            new AnySensorReading().WithHumidity(new RelativeHumidity(40.0)).Generate()
        ]).Generate();

        Assert.False(policy.ShouldIrrigate(batch));
    }

    [Fact]
    public void Never_irrigates_on_too_few_readings_whatever_their_humidity() {
        // Arrange: with fewer readings than the minimum, humidity cannot change the outcome — a dummy.
        IrrigationPolicy anyPolicy = new AnyIrrigationPolicy().WithMinimumReadings(5).Generate();
        ReadingBatch batch = new AnyReadingBatch().WithReadings(r => r.WithCountBetween(1, 4)).Generate();

        // Act / Assert
        Assert.False(anyPolicy.ShouldIrrigate(batch));
    }

}
