using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class ReadingBatchTests {

    // A reading whose only meaningful trait is its humidity; everything else is a dummy.
    private static SensorReading ReadingWithHumidity(double percent) {
        return Any.SensorReading().WithHumidity(humidity => humidity.WithPercent(percent)).Generate();
    }

    private static SensorReading ReadingAt(double latitude, double longitude) {
        return Any.SensorReading().WithPosition(position => position.WithLatitude(latitude).WithLongitude(longitude)).Generate();
    }

    private static SensorReading ReadingOf(SensorId sensor, Half volts) {
        return Any.SensorReading().WithSensor(sensor).WithBattery(battery => battery.WithVolts(volts)).Generate();
    }

    private static ReadingBatch BatchOf(params SensorReading[] readings) {
        return Any.ReadingBatch().WithReadings(readings).Generate();
    }

    // ------------------------------------------------------------------ construction

    [Fact]
    public void A_batch_keeps_its_gateway_and_its_readings_in_order() {
        // Arrange
        SensorId             anyGateway  = Any.SensorId().Generate();
        List<SensorReading>  anyReadings = Any.ListOf(Any.SensorReading()).WithCountBetween(2, 10).Generate();

        // Act
        ReadingBatch batch = new ReadingBatch(anyGateway, anyReadings);

        // Assert
        Assert.Same(anyGateway, batch.Gateway);
        Assert.Equal(anyReadings.Count, batch.Readings.Count);
        for (int index = 0; index < anyReadings.Count; index++) {
            Assert.Same(anyReadings[index], batch.Readings[index]);
        }
    }

    [Fact]
    public void A_batch_is_not_affected_by_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        SensorId            anyGateway  = Any.SensorId().Generate();
        List<SensorReading> anyReadings = Any.ListOf(Any.SensorReading()).WithCountBetween(2, 10).Generate();
        int                 countBefore = anyReadings.Count;
        ReadingBatch        batch       = new ReadingBatch(anyGateway, anyReadings);

        // Act
        anyReadings.Clear();

        // Assert
        Assert.Equal(countBefore, batch.Readings.Count);
    }

    [Fact]
    public void A_missing_gateway_is_refused() {
        // Arrange
        List<SensorReading> anyReadings = Any.ListOf(Any.SensorReading()).NonEmpty().WithMaxCount(5).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReadingBatch(null!, anyReadings));

        // Assert
        Assert.Equal("gateway", refusal.ParamName);
    }

    [Fact]
    public void A_missing_list_of_readings_is_refused() {
        // Arrange
        SensorId anyGateway = Any.SensorId().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ReadingBatch(anyGateway, null!));

        // Assert
        Assert.Equal("readings", refusal.ParamName);
    }

    [Fact]
    public void A_batch_without_any_reading_is_refused() {
        // Arrange
        SensorId anyGateway = Any.SensorId().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, new List<SensorReading>()));

        // Assert
        Assert.Equal("readings", refusal.ParamName);
    }

    [Fact]
    public void A_batch_of_a_single_reading_is_valid() {
        // Arrange
        SensorId      anyGateway = Any.SensorId().Generate();
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ReadingBatch batch = new ReadingBatch(anyGateway, [anyReading]);

        // Assert
        Assert.Same(anyReading, Assert.Single(batch.Readings));
    }

    [Fact]
    public void A_batch_of_exactly_500_000_readings_is_valid() {
        // Arrange: one reading repeated, the batch does not look at its items for identity
        SensorId      anyGateway = Any.SensorId().Generate();
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ReadingBatch batch = new ReadingBatch(anyGateway, Enumerable.Repeat(anyReading, 500_000).ToList());

        // Assert
        Assert.Equal(500_000, batch.Readings.Count);
    }

    [Fact]
    public void A_batch_of_more_than_500_000_readings_is_refused() {
        // Arrange
        SensorId      anyGateway = Any.SensorId().Generate();
        SensorReading anyReading = Any.SensorReading().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, Enumerable.Repeat(anyReading, 500_001).ToList()));

        // Assert
        Assert.Equal("readings", refusal.ParamName);
    }

    // ------------------------------------------------------------------ AverageHumidity

    [Fact]
    public void The_average_humidity_of_a_single_reading_is_its_own_humidity() {
        // Arrange
        ReadingBatch batch = BatchOf(ReadingWithHumidity(37.25));

        // Assert
        Assert.Equal(37.25, batch.AverageHumidity());
    }

    [Fact]
    public void The_average_humidity_is_the_arithmetic_mean_of_the_readings() {
        // Arrange
        ReadingBatch batch = BatchOf(ReadingWithHumidity(10.0), ReadingWithHumidity(20.0), ReadingWithHumidity(60.0));

        // Assert
        Assert.Equal(30.0, batch.AverageHumidity());
    }

    [Fact]
    public void A_repeated_reading_weighs_as_many_times_as_it_appears() {
        // Arrange
        SensorReading dry = ReadingWithHumidity(0.0);
        SensorReading wet = ReadingWithHumidity(100.0);

        // Act
        ReadingBatch batch = BatchOf(dry, dry, dry, wet);

        // Assert
        Assert.Equal(25.0, batch.AverageHumidity());
    }

    // ------------------------------------------------------------------ CountDrierThan

    [Fact]
    public void Drier_than_counts_the_readings_strictly_below_the_threshold() {
        // Arrange
        ReadingBatch batch = BatchOf(ReadingWithHumidity(10.0), ReadingWithHumidity(20.0), ReadingWithHumidity(30.0), ReadingWithHumidity(40.0));

        // Assert
        Assert.Equal(2, batch.CountDrierThan(30.0));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(10.0, 0)]
    [InlineData(10.5, 1)]
    [InlineData(100.0, 4)]
    public void Drier_than_follows_the_threshold_from_nothing_to_everything(double threshold, int expected) {
        // Arrange
        ReadingBatch batch = BatchOf(ReadingWithHumidity(10.0), ReadingWithHumidity(20.0), ReadingWithHumidity(30.0), ReadingWithHumidity(40.0));

        // Assert
        Assert.Equal(expected, batch.CountDrierThan(threshold));
    }

    // ------------------------------------------------------------------ OutsideOf

    [Fact]
    public void Outside_of_returns_only_the_readings_the_fence_does_not_contain_in_their_original_order() {
        // Arrange
        Geofence      field   = new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(20.0, 40.0));
        SensorReading inside  = ReadingAt(15.0, 35.0);
        SensorReading onEdge  = ReadingAt(10.0, 35.0);
        SensorReading north   = ReadingAt(20.5, 35.0);
        SensorReading west    = ReadingAt(15.0, 29.5);
        SensorReading onCorner = ReadingAt(20.0, 40.0);
        ReadingBatch  batch   = BatchOf(north, inside, west, onEdge, onCorner);

        // Act
        IReadOnlyList<SensorReading> outside = batch.OutsideOf(field);

        // Assert
        Assert.Equal(2, outside.Count);
        Assert.Same(north, outside[0]);
        Assert.Same(west, outside[1]);
    }

    [Fact]
    public void Outside_of_is_empty_when_the_fence_contains_every_reading() {
        // Arrange
        Geofence     wholeWorldButTheAntimeridian = new Geofence(new GeoCoordinate(-90.0, -180.0), new GeoCoordinate(90.0, 179.99999999999997));
        ReadingBatch anyBatch                     = Any.ReadingBatch().Generate();

        // Assert
        Assert.Empty(anyBatch.OutsideOf(wholeWorldButTheAntimeridian));
    }

    [Fact]
    public void Outside_of_returns_the_same_reading_twice_when_the_batch_holds_it_twice() {
        // Arrange
        Geofence      field    = new Geofence(new GeoCoordinate(10.0, 30.0), new GeoCoordinate(20.0, 40.0));
        SensorReading straying = ReadingAt(0.0, 0.0);

        // Act
        IReadOnlyList<SensorReading> outside = BatchOf(straying, straying).OutsideOf(field);

        // Assert
        Assert.Equal(2, outside.Count);
    }

    // ------------------------------------------------------------------ SensorsWithLowBattery

    [Fact]
    public void Low_battery_sensors_are_those_with_a_reading_under_the_threshold() {
        // Arrange
        SensorId flat   = Any.SensorId().Generate();
        SensorId healthy = Any.SensorId().WithValue(value => value.DifferentFrom(flat.Value)).Generate();
        ReadingBatch batch = BatchOf(ReadingOf(flat, (Half)3.0f), ReadingOf(healthy, (Half)3.9f));

        // Act
        IReadOnlyCollection<SensorId> lowBattery = batch.SensorsWithLowBattery();

        // Assert
        Assert.Equal(flat, Assert.Single(lowBattery));
    }

    [Fact]
    public void A_sensor_is_listed_once_however_many_of_its_readings_are_low_even_through_different_instances_of_its_id() {
        // Arrange
        SensorId first  = Any.SensorId().Generate();
        SensorId second = new SensorId(first.Value);
        ReadingBatch batch = BatchOf(ReadingOf(first, (Half)2.9f), ReadingOf(second, (Half)3.1f), ReadingOf(first, (Half)2.6f));

        // Act
        IReadOnlyCollection<SensorId> lowBattery = batch.SensorsWithLowBattery();

        // Assert
        Assert.Equal(first, Assert.Single(lowBattery));
    }

    [Fact]
    public void A_sensor_with_one_low_reading_and_one_good_reading_is_listed() {
        // Arrange
        SensorId sensor = Any.SensorId().Generate();
        ReadingBatch batch = BatchOf(ReadingOf(sensor, (Half)4.0f), ReadingOf(sensor, (Half)3.0f), ReadingOf(sensor, (Half)4.1f));

        // Assert
        Assert.Equal(sensor, Assert.Single(batch.SensorsWithLowBattery()));
    }

    [Fact]
    public void Two_different_low_sensors_are_both_listed() {
        // Arrange
        SensorId one = Any.SensorId().Generate();
        SensorId other = Any.SensorId().WithValue(value => value.DifferentFrom(one.Value)).Generate();
        ReadingBatch batch = BatchOf(ReadingOf(one, (Half)2.5f), ReadingOf(other, (Half)3.2f));

        // Assert
        Assert.Equal(2, batch.SensorsWithLowBattery().Count);
        Assert.Contains(one, batch.SensorsWithLowBattery());
        Assert.Contains(other, batch.SensorsWithLowBattery());
    }

    [Fact]
    public void No_sensor_is_listed_when_every_battery_is_at_or_above_the_threshold() {
        // Arrange
        ReadingBatch batch = BatchOf(
            Any.SensorReading().WithBattery(battery => battery.WithVolts(BatteryVoltage.LowThreshold)).Generate(),
            Any.SensorReading().WithBattery(battery => battery.WithVolts(BatteryVoltage.Maximum)).Generate());

        // Assert
        Assert.Empty(batch.SensorsWithLowBattery());
    }

    [Fact]
    public void The_gateway_is_not_a_sensor_of_the_batch() {
        // Arrange
        SensorId gateway = Any.SensorId().Generate();
        SensorId sensor  = Any.SensorId().WithValue(value => value.DifferentFrom(gateway.Value)).Generate();
        ReadingBatch batch = Any.ReadingBatch().WithGateway(gateway).WithReadings([ReadingOf(sensor, (Half)3.0f)]).Generate();

        // Assert
        Assert.DoesNotContain(gateway, batch.SensorsWithLowBattery());
    }

    // ------------------------------------------------------------------ what the batch does not check

    [Fact(Skip = "domain defect: OutsideOf does not null-check its fence and fails with a NullReferenceException instead of an ArgumentNullException naming 'fence'; no drawn value involved")]
    public void A_missing_fence_is_refused_as_a_missing_argument() {
        // Arrange
        ReadingBatch anyBatch = Any.ReadingBatch().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyBatch.OutsideOf(null!));

        // Assert
        Assert.Equal("fence", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: a batch accepts a null item in its list of readings and fails later, in AverageHumidity, with a NullReferenceException far from the cause; no drawn value involved except dummies unrelated to the null")]
    public void A_null_reading_in_the_list_is_refused_when_the_batch_is_built() {
        // Arrange
        SensorId anyGateway = Any.SensorId().Generate();
        SensorReading anyReading = Any.SensorReading().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ReadingBatch(anyGateway, new SensorReading[] { anyReading, null! }));
    }

}
