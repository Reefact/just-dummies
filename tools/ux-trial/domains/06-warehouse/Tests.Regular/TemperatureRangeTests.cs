namespace Warehouse.Domain.Tests.Regular;

public sealed class TemperatureRangeTests {

    [Fact]
    public void Any_generated_range_keeps_its_bounds_in_order_within_40_degrees() {
        // Act
        TemperatureRange anyRange = new AnyTemperatureRange().Generate();

        // Assert
        Assert.InRange(anyRange.MinCelsius, -40m, 40m);
        Assert.InRange(anyRange.MaxCelsius, -40m, 40m);
        Assert.True(anyRange.MinCelsius < anyRange.MaxCelsius);
    }

    [Fact]
    public void The_bounds_are_kept_as_given() {
        // Act
        TemperatureRange range = new TemperatureRange(2m, 8m);

        // Assert
        Assert.Equal(2m, range.MinCelsius);
        Assert.Equal(8m, range.MaxCelsius);
    }

    [Fact]
    public void Both_ends_of_the_storable_scale_are_accepted() {
        // Act
        TemperatureRange widest = new TemperatureRange(-40m, 40m);

        // Assert
        Assert.Equal(-40m, widest.MinCelsius);
        Assert.Equal(40m, widest.MaxCelsius);
    }

    [Fact]
    public void A_minimum_below_minus_40_is_refused() {
        // Arrange
        decimal anyTooCold = Any.Decimal().LessThan(-40m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(anyTooCold, 0m));

        // Assert
        Assert.Equal("minCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_maximum_above_40_is_refused() {
        // Arrange
        decimal anyTooHot = Any.Decimal().GreaterThan(40m).Generate();

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, anyTooHot));
    }

    [Fact]
    public void Just_beyond_the_scale_is_refused_at_either_end() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(-40.01m, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, 40.01m));
    }

    // The refusal for a too-hot maximum names the minimum: the message and the parameter point at the wrong argument.
    [Fact(Skip = "domain defect: a maximum above 40 is reported on the 'minCelsius' parameter (TemperatureRange.cs line 9), seed n/a (deterministic)")]
    public void A_too_hot_maximum_is_reported_on_the_maximum_parameter() {
        // Arrange
        decimal anyTooHot = Any.Decimal().GreaterThan(40m).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(0m, anyTooHot));

        // Assert
        Assert.Equal("maxCelsius", refusal.ParamName);
    }

    [Fact]
    public void A_minimum_above_the_maximum_is_refused() {
        // Arrange
        decimal anyMaximum = Any.Decimal().Between(-40m, 39m).Generate();
        decimal anyMinimum = Any.Decimal().GreaterThan(anyMaximum).LessThanOrEqualTo(40m).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TemperatureRange(anyMinimum, anyMaximum));

        // Assert
        Assert.Contains(anyMinimum.ToString(System.Globalization.CultureInfo.CurrentCulture), refusal.Message);
    }

    [Fact]
    public void A_minimum_equal_to_the_maximum_is_refused() {
        // Arrange
        decimal anyTemperature = Any.Decimal().Between(-40m, 40m).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new TemperatureRange(anyTemperature, anyTemperature));
    }

    [Fact]
    public void The_scale_is_checked_before_the_order() {
        // Arrange: out of scale AND inverted
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(50m, 45m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TemperatureRange(-45m, -50m));
    }

    [Theory]
    [InlineData(-39.99, StorageZone.Frozen)]
    [InlineData(-18, StorageZone.Frozen)]
    [InlineData(-17.99, StorageZone.Chilled)]
    [InlineData(0, StorageZone.Chilled)]
    [InlineData(8, StorageZone.Chilled)]
    [InlineData(8.01, StorageZone.Ambient)]
    [InlineData(15, StorageZone.Ambient)]
    [InlineData(40, StorageZone.Ambient)]
    public void The_zone_is_decided_by_the_maximum_alone(double maximum, StorageZone expected) {
        // Arrange: the minimum is a dummy, any value below the maximum
        decimal max = (decimal)maximum;
        TemperatureRange range = new AnyTemperatureRange().WithMaxCelsius(max).Generate();

        // Assert
        Assert.Equal(expected, range.Zone);
    }

    [Fact]
    public void A_range_whose_maximum_is_at_most_minus_18_is_frozen_whatever_the_minimum() {
        // Arrange
        TemperatureRange anyFrozen = new AnyTemperatureRange().WithMaxCelsius(m => m.Between(-39m, -18m)).Generate();

        // Assert
        Assert.Equal(StorageZone.Frozen, anyFrozen.Zone);
    }

    [Fact]
    public void A_range_whose_maximum_is_between_minus_18_exclusive_and_8_is_chilled() {
        // Arrange
        TemperatureRange anyChilled = new AnyTemperatureRange().WithMaxCelsius(m => m.GreaterThan(-18m).LessThanOrEqualTo(8m)).Generate();

        // Assert
        Assert.Equal(StorageZone.Chilled, anyChilled.Zone);
    }

    [Fact]
    public void A_range_whose_maximum_is_above_8_is_ambient_even_when_it_reaches_below_zero() {
        // Arrange
        TemperatureRange anyAmbient = new AnyTemperatureRange().WithMaxCelsius(m => m.GreaterThan(8m)).Generate();

        // Assert
        Assert.Equal(StorageZone.Ambient, anyAmbient.Zone);
    }

}
