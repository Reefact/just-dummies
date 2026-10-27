using JustDummies;
using JustDummies.Xunit;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

[Reproducible]
public sealed class MeterTests {

    private static readonly DateOnly Commissioned = new DateOnly(2024, 3, 1);

    private static Meter MeterCommissionedAt(decimal kwh) {
        return Any.Meter()
                  .WithCommissioningReading(reading => reading.WithIndex(index => index.WithKwh(kwh)).WithReadingDate(Commissioned))
                  .Generate();
    }

    private static AnyMeterReading ReadingOf(Meter meter) {
        return Arbitrary.MeterReading().WithDeliveryPoint(meter.DeliveryPoint);
    }

    [Fact]
    public void Recording_a_later_reading_measures_the_energy_since_the_last_one() {
        // Arrange
        Meter        anyMeter   = MeterCommissionedAt(12_000m);
        MeterReading anyReading = ReadingOf(anyMeter).WithIndex(index => index.WithKwh(12_850m))
                                                     .WithReadingDate(date => date.After(Commissioned))
                                                     .Generate();

        // Act
        ConsumptionPeriod period = anyMeter.Record(anyReading);

        // Assert
        Assert.Equal(850m, period.ConsumedKwh);
    }

    [Fact]
    public void A_reading_below_the_last_index_is_refused() {
        // Arrange
        Meter        anyMeter          = MeterCommissionedAt(12_000m);
        MeterReading anyRegressingRead = ReadingOf(anyMeter).WithIndex(index => index.WithKwh(kwh => kwh.LessThan(12_000m)))
                                                            .WithReadingDate(date => date.After(Commissioned))
                                                            .Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyMeter.Record(anyRegressingRead));
    }

    [Fact]
    public void A_reading_that_does_not_move_forward_in_time_is_refused() {
        // Arrange
        Meter        anyMeter       = MeterCommissionedAt(12_000m);
        MeterReading anyBackdated   = ReadingOf(anyMeter).WithReadingDate(date => date.BeforeOrEqualTo(Commissioned))
                                                         .WithIndex(index => index.WithKwh(kwh => kwh.GreaterThanOrEqualTo(12_000m)))
                                                         .Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyMeter.Record(anyBackdated));
    }

    [Fact]
    public void An_actual_reading_replaces_an_estimate_of_the_same_day() {
        // Arrange
        DateOnly     readingDay = new DateOnly(2024, 4, 1);
        Meter        anyMeter   = MeterCommissionedAt(12_000m);
        anyMeter.Record(ReadingOf(anyMeter).WithReadingDate(readingDay).WithSource(ReadingSource.Estimated)
                                           .WithIndex(index => index.WithKwh(12_400m)).Generate());
        MeterReading anyActual = ReadingOf(anyMeter).WithReadingDate(readingDay).WithSource(ReadingSource.Actual)
                                                    .WithIndex(index => index.WithKwh(12_310m)).Generate();

        // Act
        ConsumptionPeriod period = anyMeter.Record(anyActual);

        // Assert
        Assert.Equal(310m, period.ConsumedKwh);
        Assert.Equal(2, anyMeter.Readings.Count);
    }

    [Fact]
    public void A_reading_of_another_delivery_point_is_refused() {
        // Arrange
        Meter        anyMeter            = Any.Meter().Generate();
        MeterReading anyForeignReading   = Arbitrary.MeterReading()
                                                    .WithDeliveryPoint(id => id.WithValue(value => value.DifferentFrom(anyMeter.DeliveryPoint.Value)))
                                                    .Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyMeter.Record(anyForeignReading));
    }

}
