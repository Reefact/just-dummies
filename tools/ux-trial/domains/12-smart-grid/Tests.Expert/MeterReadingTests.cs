using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

public sealed class MeterReadingTests {

    [Fact]
    public void Carries_the_point_index_day_and_source_it_was_taken_with() {
        // Arrange: here every value is what the test is about, so every value is a literal
        DeliveryPointId point = new DeliveryPointId("12345678901234");
        MeterIndex      index = new MeterIndex(4521.5m);

        // Act
        MeterReading reading = new MeterReading(point, index, new DateOnly(2024, 3, 1), ReadingSource.CustomerSupplied);

        // Assert
        Assert.Same(point, reading.DeliveryPoint);
        Assert.Same(index, reading.Index);
        Assert.Equal(new DateOnly(2024, 3, 1), reading.ReadingDate);
        Assert.Equal(ReadingSource.CustomerSupplied, reading.Source);
    }

    [Fact]
    public void The_smart_metering_epoch_is_the_first_of_january_2010() {
        Assert.Equal(new DateOnly(2010, 1, 1), MeterReading.SmartMeteringEpoch);
    }

    [Fact]
    public void Accepts_a_reading_taken_on_the_epoch_itself() {
        MeterReading reading = Any.MeterReading().WithReadingDate(new DateOnly(2010, 1, 1)).Generate();

        Assert.Equal(MeterReading.SmartMeteringEpoch, reading.ReadingDate);
    }

    [Fact]
    public void Refuses_a_reading_taken_the_day_before_the_epoch() {
        AnyMeterReading anyReading = Any.MeterReading();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => anyReading.WithReadingDate(new DateOnly(2009, 12, 31)).Generate());

        Assert.Equal("readingDate", refusal.ParamName);
    }

    [Fact]
    public void Refuses_any_reading_taken_before_the_epoch() {
        DateOnly anyEarlyDay = Any.DateOnly().Before(MeterReading.SmartMeteringEpoch).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.MeterReading().WithReadingDate(anyEarlyDay).Generate());
    }

    [Theory]
    [InlineData((int)ReadingSource.Actual)]
    [InlineData((int)ReadingSource.Estimated)]
    [InlineData((int)ReadingSource.CustomerSupplied)]
    public void Accepts_every_declared_source(int source) {
        MeterReading reading = Any.MeterReading().WithSource((ReadingSource)source).Generate();

        Assert.Equal((ReadingSource)source, reading.Source);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public void Refuses_a_source_that_is_not_declared(int undeclared) {
        AnyMeterReading anyReading = Any.MeterReading();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => anyReading.WithSource((ReadingSource)undeclared).Generate());

        Assert.Equal("source", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_delivery_point() {
        MeterIndex    anyIndex  = Any.MeterIndex().Generate();
        DateOnly      anyDay    = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();
        ReadingSource anySource = Any.Enum<ReadingSource>().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new MeterReading(null!, anyIndex, anyDay, anySource));

        Assert.Equal("deliveryPoint", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_index() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        DateOnly        anyDay    = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();
        ReadingSource   anySource = Any.Enum<ReadingSource>().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new MeterReading(anyPoint, null!, anyDay, anySource));

        Assert.Equal("index", refusal.ParamName);
    }

}
