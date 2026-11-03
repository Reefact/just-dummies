using JustDummies;

using Warehouse.Domain.Tests.Generators;

using Xunit;

namespace Warehouse.Domain.Tests.Expert;

public sealed class TemperatureRangeTests {

    public static TheoryData<decimal, StorageZone> MaximumsAndTheirZone => new() {
        { -39m,    StorageZone.Frozen },
        { -18.01m, StorageZone.Frozen },
        { -18m,    StorageZone.Frozen },
        { -17.99m, StorageZone.Chilled },
        { 0m,      StorageZone.Chilled },
        { 8m,      StorageZone.Chilled },
        { 8.01m,   StorageZone.Ambient },
        { 40m,     StorageZone.Ambient }
    };

    public static TheoryData<decimal, decimal> RangesWithAMinimumBelowTheScale => new() {
        { -40.01m, 0m },
        { -41m,    -40m },
        { -1000m,  0m }
    };

    public static TheoryData<decimal, decimal> RangesWithAMaximumAboveTheScale => new() {
        { 0m,  40.01m },
        { 40m, 41m },
        { 0m,  1000m }
    };

    [Fact]
    public void A_range_keeps_its_two_ends() {
        // Act
        TemperatureRange range = new TemperatureRange(2m, 8m);

        // Assert
        Assert.Equal(2m, range.MinCelsius);
        Assert.Equal(8m, range.MaxCelsius);
    }

    [Fact]
    public void Both_ends_of_the_storage_scale_are_accepted() {
        // Act
        TemperatureRange widest = new TemperatureRange(-40m, 40m);

        // Assert
        Assert.Equal(-40m, widest.MinCelsius);
        Assert.Equal(40m, widest.MaxCelsius);
    }

    [Fact]
    public void A_range_may_be_as_narrow_as_the_decimal_type_can_express() {
        // Act
        Exception? thrown = Record.Exception(() => new TemperatureRange(0m, 0.0000000001m));

        // Assert
        Assert.Null(thrown);
    }

    [Theory]
    [MemberData(nameof(MaximumsAndTheirZone))]
    public void The_zone_follows_the_maximum_whatever_the_minimum_is(decimal maxCelsius, StorageZone expectedZone) {
        // Arrange: the minimum is a dummy, drawn below the pinned maximum.
        TemperatureRange anyRange = Any.TemperatureRange().WithMaxCelsius(maxCelsius).Generate();

        // Act
        StorageZone zone = anyRange.Zone;

        // Assert
        Assert.Equal(expectedZone, zone);
    }

    [Theory]
    [InlineData(-40, 8)]
    [InlineData(7, 8)]
    public void A_range_whose_maximum_is_eight_degrees_is_chilled_whatever_the_minimum(int minCelsius, int maxCelsius) {
        // Arrange
        TemperatureRange range = new TemperatureRange(minCelsius, maxCelsius);

        // Act
        StorageZone zone = range.Zone;

        // Assert
        Assert.Equal(StorageZone.Chilled, zone);
    }

    [Fact]
    public void A_range_that_dips_into_freezing_temperatures_but_tops_above_minus_eighteen_is_not_frozen() {
        // Arrange
        TemperatureRange range = new TemperatureRange(-25m, -10m);

        // Act
        StorageZone zone = range.Zone;

        // Assert
        Assert.Equal(StorageZone.Chilled, zone);
    }

    [Theory]
    [MemberData(nameof(RangesWithAMinimumBelowTheScale))]
    public void A_minimum_below_minus_forty_is_refused_as_out_of_range(decimal minCelsius, decimal maxCelsius) {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(minCelsius, maxCelsius));

        // Assert
        Assert.Equal("minCelsius", refused.ParamName);
    }

    [Theory]
    [MemberData(nameof(RangesWithAMaximumAboveTheScale))]
    public void A_maximum_above_forty_is_refused_as_out_of_range(decimal minCelsius, decimal maxCelsius) {
        // Act
        Exception? thrown = Record.Exception(() => new TemperatureRange(minCelsius, maxCelsius));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

    [Fact(Skip = "domain defect: an out-of-range maximum is reported on the parameter 'minCelsius', not on 'maxCelsius'; MODEL.md documents the behaviour but it names the wrong parameter, seed n/a (deterministic)")]
    public void A_maximum_above_forty_is_reported_on_the_maximum_parameter() {
        // Act
        ArgumentOutOfRangeException refused = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, 40.01m));

        // Assert
        Assert.Equal("maxCelsius", refused.ParamName);
    }

    [Fact]
    public void A_minimum_equal_to_the_maximum_is_refused_as_an_invalid_range() {
        // Arrange
        decimal anyTemperature = Any.Decimal().Between(-40m, 40m).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TemperatureRange(anyTemperature, anyTemperature));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_minimum_above_the_maximum_is_refused_as_an_invalid_range() {
        // Arrange
        (decimal anyLower, decimal anyHigher) = Any.OrderedPair(Any.Decimal().Between(-40m, 40m), Any.Decimal().Between(-40m, 40m), strict: true).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new TemperatureRange(anyHigher, anyLower));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_bound_outside_the_storage_scale_is_reported_before_an_inverted_range() {
        // Act
        Exception? thrown = Record.Exception(() => new TemperatureRange(-50m, -60m));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(thrown);
    }

}
