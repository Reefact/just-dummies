using JustDummies;
using JustDummies.Xunit;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

[Reproducible]
public sealed class ConsumptionPeriodTests {

    [Fact]
    public void The_average_daily_consumption_spreads_the_energy_over_the_days_between_readings() {
        // Arrange
        DateOnly opened = new DateOnly(2025, 1, 1);
        ConsumptionPeriod anyThirtyDays = Arbitrary.ConsumptionPeriod()
                                                   .WithOpening(reading => reading.WithReadingDate(opened).WithIndex(index => index.WithKwh(1_000m)))
                                                   .WithClosing(reading => reading.WithReadingDate(opened.AddDays(30)).WithIndex(index => index.WithKwh(1_310m)))
                                                   .Generate();

        // Act
        decimal average = anyThirtyDays.AverageDailyKwh;

        // Assert
        Assert.Equal(10.333m, average);
    }

    [Fact]
    public void A_period_with_an_estimated_closing_reading_is_estimated() {
        // Arrange
        ConsumptionPeriod anyPeriod = Arbitrary.ConsumptionPeriod()
                                               .WithClosing(reading => reading.WithSource(ReadingSource.Estimated))
                                               .Generate();

        // Act
        bool estimated = anyPeriod.IsEstimated;

        // Assert
        Assert.True(estimated);
    }

    [Fact]
    public void A_period_between_two_actual_readings_is_not_estimated() {
        // Arrange
        ConsumptionPeriod anyActualPeriod = Arbitrary.ConsumptionPeriod()
                                                     .WithOpening(reading => reading.WithSource(source => source.Except(ReadingSource.Estimated)))
                                                     .WithClosing(reading => reading.WithSource(source => source.Except(ReadingSource.Estimated)))
                                                     .Generate();

        // Act
        bool estimated = anyActualPeriod.IsEstimated;

        // Assert
        Assert.False(estimated);
    }

    [Fact]
    public void Readings_of_two_delivery_points_do_not_make_a_period() {
        // Arrange
        HashSet<DeliveryPointId> anyTwoPoints = Any.SetOf(Arbitrary.DeliveryPointId()).WithCount(2).Generate();
        DeliveryPointId[]        points       = anyTwoPoints.ToArray();
        MeterReading anyOpening = Arbitrary.MeterReading().WithDeliveryPoint(points[0]).WithReadingDate(new DateOnly(2025, 1, 1)).Generate();
        MeterReading anyClosing = Arbitrary.MeterReading().WithDeliveryPoint(points[1]).WithReadingDate(new DateOnly(2025, 2, 1)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(anyOpening, anyClosing));
    }

    [Fact]
    public void A_delivery_point_id_of_thirteen_digits_is_refused() {
        // Arrange
        string anyThirteenDigits = Any.StringMatching(@"[0-9]{13}").Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new DeliveryPointId(anyThirteenDigits));
    }

    [Fact]
    public void An_index_with_a_fourth_decimal_place_is_refused() {
        // Arrange
        decimal anyTooPrecise = Any.Decimal().Between(0m, 1_000m).WithScale(4).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MeterIndex(anyTooPrecise));
    }

}
