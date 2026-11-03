namespace Manufacturing.Domain.Tests.Expert;

public sealed class CadenceTests {

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(10_000)]
    public void A_cadence_from_one_to_ten_thousand_units_per_hour_is_accepted(int unitsPerHour) {
        // Act
        Cadence cadence = new Cadence(unitsPerHour);

        // Assert
        Assert.Equal(unitsPerHour, cadence.UnitsPerHour);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(10_001)]
    [InlineData(int.MaxValue)]
    public void A_cadence_outside_one_to_ten_thousand_units_per_hour_is_refused(int unitsPerHour) {
        // Act
        Action create = () => new Cadence(unitsPerHour);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(create);
        Assert.Equal("unitsPerHour", error.ParamName);
    }

    [Fact]
    public void The_maximum_is_ten_thousand_units_per_hour() {
        Assert.Equal(10_000, Cadence.MaximumUnitsPerHour);
    }

    [Theory]
    [InlineData(1, 1, 60)]
    [InlineData(60, 60, 60)]
    [InlineData(100, 250, 150)]
    [InlineData(10_000, 10_000, 60)]
    [InlineData(1, 100_000, 6_000_000)]
    public void Producing_a_whole_number_of_hours_of_output_takes_exactly_that_time(int unitsPerHour, int quantity, int expectedMinutes) {
        // Arrange
        Cadence cadence = new Cadence(unitsPerHour);

        // Act
        TimeSpan duration = cadence.TimeToProduce(quantity);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
    }

    [Theory]
    [InlineData(7, 1, 9)]        // 8.57 minutes
    [InlineData(7, 7, 60)]       // exactly one hour: no rounding
    [InlineData(7, 8, 69)]       // 68.57 minutes
    [InlineData(60, 1, 1)]       // exactly one minute
    [InlineData(10_000, 1, 1)]   // 0.006 minutes: rounded up, never down to zero
    [InlineData(61, 1, 1)]       // 0.98 minutes
    [InlineData(59, 1, 2)]       // 1.02 minutes
    public void The_time_to_produce_is_rounded_up_to_the_whole_minute(int unitsPerHour, int quantity, int expectedMinutes) {
        // Arrange
        Cadence cadence = new Cadence(unitsPerHour);

        // Act
        TimeSpan duration = cadence.TimeToProduce(quantity);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
    }

    [Fact]
    public void Producing_nothing_takes_no_time() {
        // Arrange
        Cadence anyCadence = Any.Cadence().Generate();

        // Act
        TimeSpan duration = anyCadence.TimeToProduce(0);

        // Assert
        Assert.Equal(TimeSpan.Zero, duration);
    }

    [Fact]
    public void The_largest_quantity_does_not_overflow_the_computation() {
        // Arrange
        Cadence cadence = new Cadence(10_000);

        // Act
        TimeSpan duration = cadence.TimeToProduce(int.MaxValue);

        // Assert: 2 147 483 647 x 60 / 10 000 = 12 884 901.88, rounded up
        Assert.Equal(TimeSpan.FromMinutes(12_884_902), duration);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_negative_quantity_is_refused(int quantity) {
        // Arrange
        Cadence anyCadence = Any.Cadence().Generate();

        // Act
        Action produce = () => anyCadence.TimeToProduce(quantity);

        // Assert
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(produce);
        Assert.Equal("quantity", error.ParamName);
    }

    [Fact]
    public void Two_cadences_of_the_same_rate_are_equal() {
        // Arrange
        Cadence first  = new Cadence(120);
        Cadence second = new Cadence(120);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_cadences_of_different_rates_are_different() {
        Assert.NotEqual(new Cadence(120), new Cadence(121));
    }

    [Fact]
    public void A_cadence_is_not_equal_to_nothing() {
        // Arrange
        Cadence anyCadence = Any.Cadence().Generate();

        // Assert
        Assert.False(anyCadence.Equals((Cadence?)null));
        Assert.False(anyCadence.Equals((object?)null));
    }

}
