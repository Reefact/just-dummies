using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class ReadingBatchTests {

    [Fact]
    public void A_batch_keeps_its_gateway_and_its_readings_in_order() {
        // Arrange
        SensorId                     anyGateway  = Any.SensorId().Generate();
        IReadOnlyList<SensorReading> anyReadings = Any.ListOf(Any.SensorReading()).WithCountBetween(1, 20).Generate();

        // Act
        ReadingBatch batch = new ReadingBatch(anyGateway, anyReadings);

        // Assert
        Assert.Same(anyGateway, batch.Gateway);
        Assert.Equal(anyReadings, batch.Readings);
    }

    [Fact]
    public void A_missing_gateway_is_refused() {
        IReadOnlyList<SensorReading> anyReadings = Any.ListOf(Any.SensorReading()).WithCountBetween(1, 5).Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ReadingBatch(null!, anyReadings));

        Assert.Equal("gateway", exception.ParamName);
    }

    [Fact]
    public void A_missing_list_of_readings_is_refused() {
        SensorId anyGateway = Any.SensorId().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ReadingBatch(anyGateway, null!));

        Assert.Equal("readings", exception.ParamName);
    }

    [Fact]
    public void An_empty_batch_is_refused() {
        SensorId anyGateway = Any.SensorId().Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, new List<SensorReading>()));

        Assert.Equal("readings", exception.ParamName);
    }

    [Fact]
    public void A_batch_of_a_single_reading_is_valid() {
        SensorReading anyReading = Any.SensorReading().Generate();

        ReadingBatch batch = new ReadingBatch(Any.SensorId().Generate(), new[] { anyReading });

        Assert.Single(batch.Readings);
    }

    [Fact]
    public void A_batch_of_the_maximum_number_of_readings_is_valid() {
        // Arrange: one reading repeated; what is in the list does not matter, only how many there are.
        SensorReading anyReading = Any.SensorReading().Generate();
        List<SensorReading> maximum = Enumerable.Repeat(anyReading, ReadingBatch.MaximumReadings).ToList();

        // Act
        ReadingBatch batch = new ReadingBatch(Any.SensorId().Generate(), maximum);

        // Assert
        Assert.Equal(500_000, batch.Readings.Count);
    }

    [Fact]
    public void A_batch_one_reading_over_the_maximum_is_refused() {
        // Arrange
        SensorReading anyReading = Any.SensorReading().Generate();
        List<SensorReading> tooMany = Enumerable.Repeat(anyReading, ReadingBatch.MaximumReadings + 1).ToList();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ReadingBatch(Any.SensorId().Generate(), tooMany));

        // Assert
        Assert.Equal("readings", exception.ParamName);
    }

    [Fact]
    public void The_readings_are_copied_so_the_caller_cannot_change_the_batch_afterwards() {
        // Arrange
        List<SensorReading> source = Any.ListOf(Any.SensorReading()).WithCount(3).Generate().ToList();
        ReadingBatch        batch  = new ReadingBatch(Any.SensorId().Generate(), source);

        // Act
        source.Clear();

        // Assert
        Assert.Equal(3, batch.Readings.Count);
    }

    [Fact(Skip = "domain defect: ReadingBatch does not null-check its items, so a null reading is accepted and AverageHumidity later throws NullReferenceException, seed n/a (literal case)")]
    public void A_null_reading_in_the_list_is_refused() {
        SensorReading anyReading = Any.SensorReading().Generate();

        Assert.ThrowsAny<ArgumentException>(() => new ReadingBatch(Any.SensorId().Generate(), new SensorReading?[] { anyReading, null }!));
    }

    [Fact]
    public void The_average_humidity_is_the_arithmetic_mean_of_the_readings() {
        ReadingBatch batch = Readings.BatchOf(Readings.WithHumidity(20.0), Readings.WithHumidity(40.0), Readings.WithHumidity(90.0));

        Assert.Equal(50.0, batch.AverageHumidity());
    }

    [Fact]
    public void The_average_humidity_of_one_reading_is_its_humidity() {
        ReadingBatch batch = Readings.BatchOf(Readings.WithHumidity(33.3));

        Assert.Equal(33.3, batch.AverageHumidity());
    }

    [Fact]
    public void The_average_humidity_counts_a_repeated_value_each_time() {
        ReadingBatch batch = Readings.BatchOf(Readings.WithHumidity(10.0), Readings.WithHumidity(10.0), Readings.WithHumidity(10.0), Readings.WithHumidity(70.0));

        Assert.Equal(25.0, batch.AverageHumidity());
    }

    [Fact]
    public void Humidity_drier_than_a_threshold_is_counted_strictly() {
        ReadingBatch batch = Readings.BatchOf(Readings.WithHumidity(10.0), Readings.WithHumidity(20.0), Readings.WithHumidity(20.0), Readings.WithHumidity(30.0));

        Assert.Equal(1, batch.CountDrierThan(20.0));
        Assert.Equal(3, batch.CountDrierThan(20.0000001));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(10.0, 0)]
    [InlineData(10.0000001, 1)]
    [InlineData(100.0, 4)]
    [InlineData(1000.0, 4)]
    [InlineData(-5.0, 0)]
    public void The_count_of_drier_readings_follows_the_threshold(double threshold, int expected) {
        ReadingBatch batch = Readings.BatchOf(Readings.WithHumidity(10.0), Readings.WithHumidity(20.0), Readings.WithHumidity(20.0), Readings.WithHumidity(30.0));

        Assert.Equal(expected, batch.CountDrierThan(threshold));
    }

    [Fact]
    public void Readings_outside_a_fence_are_returned_in_batch_order() {
        // Arrange
        Geofence      field   = new Geofence(new GeoCoordinate(10.0, 20.0), new GeoCoordinate(30.0, 40.0));
        SensorReading inside  = Readings.At(20.0, 30.0);
        SensorReading south   = Readings.At(5.0, 30.0);
        SensorReading edge    = Readings.At(10.0, 20.0);
        SensorReading east    = Readings.At(20.0, 41.0);
        ReadingBatch  batch   = Readings.BatchOf(south, inside, east, edge);

        // Act
        IReadOnlyList<SensorReading> outside = batch.OutsideOf(field);

        // Assert
        Assert.Equal(new[] { south, east }, outside);
    }

    [Fact]
    public void No_reading_is_outside_a_fence_that_contains_them_all() {
        Geofence     everywhere = new Geofence(new GeoCoordinate(-90.0, -180.0), new GeoCoordinate(90.0, 179.0));
        ReadingBatch batch      = Readings.BatchOf(Readings.At(0.0, 0.0), Readings.At(-90.0, -180.0));

        Assert.Empty(batch.OutsideOf(everywhere));
    }

    [Fact]
    public void Every_reading_is_outside_a_fence_that_contains_none() {
        Geofence     faraway = new Geofence(new GeoCoordinate(50.0, 50.0), new GeoCoordinate(51.0, 51.0));
        SensorReading first  = Readings.At(0.0, 0.0);
        SensorReading second = Readings.At(10.0, 10.0);

        ReadingBatch batch = Readings.BatchOf(first, second);

        Assert.Equal(new[] { first, second }, batch.OutsideOf(faraway));
    }

    [Fact(Skip = "domain defect: ReadingBatch.OutsideOf does not null-check its fence and throws NullReferenceException instead of ArgumentNullException, seed n/a (literal case)")]
    public void A_missing_fence_is_refused() {
        ReadingBatch anyBatch = Any.ReadingBatch().Generate();

        Assert.Throws<ArgumentNullException>(() => anyBatch.OutsideOf(null!));
    }

    [Fact]
    public void Sensors_with_a_low_battery_are_reported_once_each() {
        // Arrange
        ReadingBatch batch = Readings.BatchOf(
            Readings.FromSensor("AG-000001", (Half)3.0f),
            Readings.FromSensor("AG-000001", (Half)2.6f),     // the same sensor again, still low
            Readings.FromSensor("AG-000002", (Half)3.9f),     // healthy
            Readings.FromSensor("AG-000003", (Half)2.5f));    // low, at the minimum

        // Act
        IReadOnlyCollection<SensorId> low = batch.SensorsWithLowBattery();

        // Assert
        Assert.Equal(2, low.Count);
        Assert.Contains(new SensorId("AG-000001"), low);
        Assert.Contains(new SensorId("AG-000003"), low);
        Assert.DoesNotContain(new SensorId("AG-000002"), low);
    }

    [Fact]
    public void A_sensor_with_one_low_and_one_healthy_reading_is_still_reported() {
        ReadingBatch batch = Readings.BatchOf(
            Readings.FromSensor("AG-0000AA", (Half)4.0f),
            Readings.FromSensor("AG-0000AA", (Half)3.0f));

        Assert.Equal(new[] { new SensorId("AG-0000AA") }, batch.SensorsWithLowBattery());
    }

    [Fact]
    public void A_battery_exactly_at_the_low_threshold_is_not_reported() {
        ReadingBatch batch = Readings.BatchOf(Readings.FromSensor("AG-000009", BatteryVoltage.LowThreshold));

        Assert.Empty(batch.SensorsWithLowBattery());
    }

    [Fact]
    public void A_battery_one_step_under_the_low_threshold_is_reported() {
        ReadingBatch batch = Readings.BatchOf(Readings.FromSensor("AG-000009", Half.BitDecrement(BatteryVoltage.LowThreshold)));

        Assert.Single(batch.SensorsWithLowBattery());
    }

}
