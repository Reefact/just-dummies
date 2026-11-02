using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class ConsumptionPeriodTests {

    [Fact]
    public void A_period_counts_the_days_between_its_two_readings() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 1, 31))
                                  .WithIndex(i => i.WithKwh(k => k.GreaterThanOrEqualTo(opening.Index.Kwh))).Generate();

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(30, period.Days);
    }

    [Fact]
    public void A_period_spans_a_leap_day() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 2, 28)).WithIndex(new MeterIndex(0m)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 3, 1)).Generate();

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(2, period.Days);
    }

    [Fact]
    public void The_consumption_is_the_difference_between_the_two_indexes() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(100.250m)).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(350.755m)).WithReadingDate(new DateOnly(2024, 1, 11)).Generate();

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(250.505m, period.ConsumedKwh);
        Assert.Same(opening, period.Opening);
        Assert.Same(closing, period.Closing);
    }

    [Fact]
    public void An_index_that_did_not_move_is_a_period_without_consumption() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(42m)).WithReadingDate(new DateOnly(2024, 5, 1)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(42.000m)).WithReadingDate(new DateOnly(2024, 5, 2)).Generate();

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(0m, period.ConsumedKwh);
        Assert.Equal(0m, period.AverageDailyKwh);
    }

    [Theory]
    [InlineData("10.000", 4, "2.500")]
    [InlineData("10.000", 3, "3.333")]
    [InlineData("20.000", 3, "6.667")]
    [InlineData("1.000", 7, "0.143")]
    public void The_daily_average_is_rounded_to_three_decimals(string consumed, int days, string expected) {
        ConsumptionPeriod period = PeriodOf(decimal.Parse(consumed, System.Globalization.CultureInfo.InvariantCulture), days);

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), period.AverageDailyKwh);
    }

    [Theory]
    [InlineData("0.001", 2, "0.000")]
    [InlineData("0.003", 2, "0.002")]
    [InlineData("0.005", 2, "0.002")]
    [InlineData("0.007", 2, "0.004")]
    [InlineData("0.009", 2, "0.004")]
    public void A_daily_average_exactly_between_two_roundings_goes_to_the_even_one(string consumed, int days, string expected) {
        ConsumptionPeriod period = PeriodOf(decimal.Parse(consumed, System.Globalization.CultureInfo.InvariantCulture), days);

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), period.AverageDailyKwh);
    }

    [Fact]
    public void A_closing_reading_below_the_opening_one_is_refused() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(100m)).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(99.999m)).WithReadingDate(new DateOnly(2024, 2, 1)).Generate();

        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));
    }

    [Fact]
    public void A_closing_reading_on_the_opening_day_is_refused() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(100m)).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(200m)).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();

        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));
    }

    [Fact]
    public void A_closing_reading_before_the_opening_day_is_refused() {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(100m)).WithReadingDate(new DateOnly(2024, 1, 10)).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(200m)).WithReadingDate(d => d.Before(new DateOnly(2024, 1, 10))).Generate();

        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));
    }

    [Fact]
    public void Readings_of_two_delivery_points_are_refused() {
        DeliveryPointId anyPoint      = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOtherPoint = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(anyPoint.Value)).Generate();
        MeterReading    opening       = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(100m)).WithReadingDate(new DateOnly(2024, 1, 1)).Generate();
        MeterReading    closing       = Any.MeterReading().WithDeliveryPoint(anyOtherPoint).WithIndex(new MeterIndex(200m)).WithReadingDate(new DateOnly(2024, 2, 1)).Generate();

        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));
    }

    [Fact]
    public void A_missing_opening_reading_is_refused() {
        MeterReading anyClosing = Any.MeterReading().Generate();

        Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(null!, anyClosing));
    }

    [Fact]
    public void A_missing_closing_reading_is_refused() {
        MeterReading anyOpening = Any.MeterReading().Generate();

        Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(anyOpening, null!));
    }

    [Theory]
    [InlineData(ReadingSource.Actual, ReadingSource.Actual, false)]
    [InlineData(ReadingSource.Actual, ReadingSource.CustomerSupplied, false)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Actual, false)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.CustomerSupplied, false)]
    [InlineData(ReadingSource.Estimated, ReadingSource.Actual, true)]
    [InlineData(ReadingSource.Actual, ReadingSource.Estimated, true)]
    [InlineData(ReadingSource.Estimated, ReadingSource.CustomerSupplied, true)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Estimated, true)]
    [InlineData(ReadingSource.Estimated, ReadingSource.Estimated, true)]
    public void A_period_is_estimated_when_either_end_is_estimated(ReadingSource opening, ReadingSource closing, bool expected) {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        MeterReading openingReading = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(0m)).WithReadingDate(new DateOnly(2024, 1, 1)).WithSource(opening).Generate();
        MeterReading closingReading = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(1m)).WithReadingDate(new DateOnly(2024, 1, 2)).WithSource(closing).Generate();

        ConsumptionPeriod period = new ConsumptionPeriod(openingReading, closingReading);

        Assert.Equal(expected, period.IsEstimated);
    }

    private static ConsumptionPeriod PeriodOf(decimal consumedKwh, int days) {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();
        DateOnly        opening  = new DateOnly(2024, 1, 1);
        MeterReading    first    = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(10m)).WithReadingDate(opening).Generate();
        MeterReading    second   = Any.MeterReading().WithDeliveryPoint(anyPoint).WithIndex(new MeterIndex(10m + consumedKwh)).WithReadingDate(opening.AddDays(days)).Generate();

        return new ConsumptionPeriod(first, second);
    }

}
