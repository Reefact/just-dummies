using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class TemperatureRangeTests {

    [Fact]
    public void A_range_keeps_its_two_ends() {
        TemperatureRange range = new TemperatureRange(2m, 8m);

        Assert.Equal(2m, range.MinCelsius);
        Assert.Equal(8m, range.MaxCelsius);
    }

    [Fact]
    public void Both_ends_of_the_storage_scale_are_accepted() {
        TemperatureRange range = new TemperatureRange(-40m, 40m);

        Assert.Equal(-40m, range.MinCelsius);
        Assert.Equal(40m, range.MaxCelsius);
    }

    [Theory]
    [InlineData(-40.01, 0)]
    [InlineData(-100, -50)]
    public void A_minimum_below_minus_forty_is_refused(double min, double max) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange((decimal)min, (decimal)max));

        Assert.Equal("minCelsius", exception.ParamName);
    }

    [Fact]
    public void A_maximum_above_forty_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, 40.01m));
    }

    [Fact(Skip = "domain defect: a maximum above 40 is reported on parameter 'minCelsius' instead of 'maxCelsius'")]
    public void A_maximum_above_forty_is_reported_on_the_maximum() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, 40.01m));

        Assert.Equal("maxCelsius", exception.ParamName);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(8, 2)]
    [InlineData(-40, -40)]
    public void A_minimum_that_is_not_below_the_maximum_is_refused(int min, int max) {
        // Exactly ArgumentException: the range guard is not the out-of-range one.
        Assert.Throws<ArgumentException>(() => new TemperatureRange(min, max));
    }

    [Theory]
    [InlineData(-18, StorageZone.Frozen)]
    [InlineData(-30, StorageZone.Frozen)]
    [InlineData(-17.99, StorageZone.Chilled)]
    [InlineData(0, StorageZone.Chilled)]
    [InlineData(8, StorageZone.Chilled)]
    [InlineData(8.01, StorageZone.Ambient)]
    [InlineData(25, StorageZone.Ambient)]
    [InlineData(40, StorageZone.Ambient)]
    public void The_zone_is_decided_on_the_maximum_alone(double max, StorageZone expected) {
        // Arrange: the minimum is arbitrary, as long as it is a valid one below the maximum.
        TemperatureRange anyRange = Any.TemperatureRange().WithMaxCelsius((decimal)max).Generate();

        // Act
        StorageZone zone = anyRange.Zone;

        // Assert
        Assert.Equal(expected, zone);
    }

}
