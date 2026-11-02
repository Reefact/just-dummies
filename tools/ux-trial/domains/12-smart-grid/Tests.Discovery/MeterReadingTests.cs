using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class MeterReadingTests {

    [Fact]
    public void A_reading_keeps_what_it_was_given() {
        DeliveryPointId point = new DeliveryPointId("12345678901234");
        MeterIndex      index = new MeterIndex(42.5m);

        MeterReading reading = new MeterReading(point, index, new DateOnly(2024, 3, 15), ReadingSource.CustomerSupplied);

        Assert.Same(point, reading.DeliveryPoint);
        Assert.Same(index, reading.Index);
        Assert.Equal(new DateOnly(2024, 3, 15), reading.ReadingDate);
        Assert.Equal(ReadingSource.CustomerSupplied, reading.Source);
    }

    [Fact]
    public void The_smart_metering_epoch_is_the_first_of_january_2010() {
        Assert.Equal(new DateOnly(2010, 1, 1), MeterReading.SmartMeteringEpoch);
    }

    [Fact]
    public void A_reading_on_the_epoch_itself_is_accepted() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterIndex      anyIndex = Any.MeterIndex().Generate();
        ReadingSource   anySource = Any.Enum<ReadingSource>().Generate();

        MeterReading reading = new MeterReading(anyPoint, anyIndex, MeterReading.SmartMeteringEpoch, anySource);

        Assert.Equal(MeterReading.SmartMeteringEpoch, reading.ReadingDate);
    }

    [Fact]
    public void A_reading_the_day_before_the_epoch_is_refused() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        MeterIndex      anyIndex  = Any.MeterIndex().Generate();
        ReadingSource   anySource = Any.Enum<ReadingSource>().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyPoint, anyIndex, new DateOnly(2009, 12, 31), anySource));
    }

    [Fact]
    public void A_reading_long_before_the_epoch_is_refused() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        MeterIndex      anyIndex  = Any.MeterIndex().Generate();
        ReadingSource   anySource = Any.Enum<ReadingSource>().Generate();
        DateOnly        anyTooEarly = Any.DateOnly().Before(MeterReading.SmartMeteringEpoch).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyPoint, anyIndex, anyTooEarly, anySource));
    }

    [Fact]
    public void A_missing_delivery_point_is_refused() {
        MeterIndex    anyIndex  = Any.MeterIndex().Generate();
        DateOnly      anyDate   = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();
        ReadingSource anySource = Any.Enum<ReadingSource>().Generate();

        Assert.Throws<ArgumentNullException>(() => new MeterReading(null!, anyIndex, anyDate, anySource));
    }

    [Fact]
    public void A_missing_index_is_refused() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        DateOnly        anyDate   = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();
        ReadingSource   anySource = Any.Enum<ReadingSource>().Generate();

        Assert.Throws<ArgumentNullException>(() => new MeterReading(anyPoint, null!, anyDate, anySource));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(99)]
    [InlineData(-1)]
    public void An_undefined_source_is_refused(int undefinedSource) {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterIndex      anyIndex = Any.MeterIndex().Generate();
        DateOnly        anyDate  = Any.DateOnly().AfterOrEqualTo(MeterReading.SmartMeteringEpoch).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new MeterReading(anyPoint, anyIndex, anyDate, (ReadingSource)undefinedSource));
    }

    [Theory]
    [InlineData(ReadingSource.Actual)]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void Every_declared_source_is_accepted(ReadingSource source) {
        MeterReading reading = Any.MeterReading().WithSource(source).Generate();

        Assert.Equal(source, reading.Source);
    }

}
