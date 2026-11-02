using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Regular;

public sealed class MeterReadingTests {

    [Fact]
    public void A_reading_keeps_what_it_was_given() {
        // Arrange
        DeliveryPointId point = new DeliveryPointId("12345678901234");
        MeterIndex      index = new MeterIndex(4321.5m);
        DateOnly        date  = new DateOnly(2024, 3, 15);

        // Act
        MeterReading reading = new MeterReading(point, index, date, ReadingSource.CustomerSupplied);

        // Assert
        Assert.Same(point, reading.DeliveryPoint);
        Assert.Same(index, reading.Index);
        Assert.Equal(new DateOnly(2024, 3, 15), reading.ReadingDate);
        Assert.Equal(ReadingSource.CustomerSupplied, reading.Source);
    }

    [Fact]
    public void A_null_delivery_point_is_refused() {
        // Arrange
        MeterIndex anyIndex = new AnyMeterIndex().Generate();
        DateOnly   anyDate  = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MeterReading(null!, anyIndex, anyDate, ReadingSource.Actual));
    }

    [Fact]
    public void A_null_index_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        anyDate  = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MeterReading(anyPoint, null!, anyDate, ReadingSource.Actual));
    }

    [Fact]
    public void A_reading_on_the_first_day_of_smart_metering_is_accepted() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        MeterIndex      anyIndex = new AnyMeterIndex().Generate();

        // Act
        MeterReading reading = new MeterReading(anyPoint, anyIndex, new DateOnly(2010, 1, 1), ReadingSource.Actual);

        // Assert
        Assert.Equal(new DateOnly(2010, 1, 1), reading.ReadingDate);
        Assert.Equal(MeterReading.SmartMeteringEpoch, reading.ReadingDate);
    }

    [Fact]
    public void A_reading_the_day_before_smart_metering_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        MeterIndex      anyIndex = new AnyMeterIndex().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyPoint, anyIndex, new DateOnly(2009, 12, 31), ReadingSource.Actual));
    }

    [Fact]
    public void A_reading_dated_before_smart_metering_is_refused_whatever_the_date() {
        // Arrange
        DeliveryPointId anyPoint      = new AnyDeliveryPointId().Generate();
        MeterIndex      anyIndex      = new AnyMeterIndex().Generate();
        DateOnly        anyEarlyDate  = Any.DateOnly().Before(MeterReading.SmartMeteringEpoch).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyPoint, anyIndex, anyEarlyDate, ReadingSource.Actual));
    }

    [Fact]
    public void A_reading_dated_after_smart_metering_is_accepted_whatever_the_date() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        MeterIndex      anyIndex = new AnyMeterIndex().Generate();
        DateOnly        anyDate  = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();

        // Act
        MeterReading reading = new MeterReading(anyPoint, anyIndex, anyDate, ReadingSource.Actual);

        // Assert
        Assert.NotNull(reading);
    }

    [Theory]
    [InlineData(ReadingSource.Actual)]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void Every_defined_source_is_accepted(ReadingSource source) {
        // Arrange
        MeterReading anyBase = new AnyMeterReading().Generate();

        // Act
        MeterReading reading = new MeterReading(anyBase.DeliveryPoint, anyBase.Index, anyBase.ReadingDate, source);

        // Assert
        Assert.Equal(source, reading.Source);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(255)]
    [InlineData(int.MaxValue)]
    public void An_undefined_source_is_refused(int value) {
        // Arrange
        MeterReading anyBase = new AnyMeterReading().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyBase.DeliveryPoint, anyBase.Index, anyBase.ReadingDate, (ReadingSource)value));
    }

}
