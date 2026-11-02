using System.Globalization;

using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

public sealed class ConsumptionPeriodTests {

    [Fact]
    public void Keeps_the_opening_and_the_closing_reading_it_was_built_from() {
        // Arrange
        (MeterReading opening, MeterReading closing) = ReadingsOfOnePoint(new DateOnly(2024, 1, 1), 100m, new DateOnly(2024, 2, 1), 150m);

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        // Assert
        Assert.Same(opening, period.Opening);
        Assert.Same(closing, period.Closing);
    }

    [Theory]
    [InlineData("2024-01-01", "2024-01-02", 1)]
    [InlineData("2024-01-01", "2024-01-31", 30)]
    [InlineData("2023-02-28", "2023-03-01", 1)]   // no leap day in 2023
    [InlineData("2024-02-28", "2024-03-01", 2)]   // 2024 has a 29 February
    [InlineData("2023-12-31", "2024-01-01", 1)]   // across the new year
    [InlineData("2023-01-01", "2024-01-01", 365)]
    [InlineData("2024-01-01", "2025-01-01", 366)] // a whole leap year
    public void Counts_the_days_between_the_two_readings(string opening, string closing, int expectedDays) {
        (MeterReading first, MeterReading last) = ReadingsOfOnePoint(DateOnly.Parse(opening), 0m, DateOnly.Parse(closing), 0m);

        ConsumptionPeriod period = new ConsumptionPeriod(first, last);

        Assert.Equal(expectedDays, period.Days);
    }

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("100", "100", "0")]                 // equal indexes: nothing consumed, allowed
    [InlineData("1000.250", "1234.750", "234.500")]
    [InlineData("0.001", "0.002", "0.001")]
    [InlineData("99999998.999", "99999999", "0.001")]
    public void Consumed_energy_is_the_difference_of_the_two_indexes(string opening, string closing, string expected) {
        (MeterReading first, MeterReading last) = ReadingsOfOnePoint(new DateOnly(2024, 1, 1), Kwh(opening), new DateOnly(2024, 1, 2), Kwh(closing));

        ConsumptionPeriod period = new ConsumptionPeriod(first, last);

        Assert.Equal(Kwh(expected), period.ConsumedKwh);
    }

    [Theory]
    [InlineData("10", 3, "3.333")]
    [InlineData("20", 3, "6.667")]
    [InlineData("31", 31, "1")]
    [InlineData("0", 17, "0")]
    [InlineData("365.250", 365, "1.001")]
    public void Average_daily_energy_is_the_consumption_over_the_days_to_three_decimals(string consumed, int days, string expected) {
        DateOnly openingDay = new DateOnly(2024, 1, 1);
        (MeterReading opening, MeterReading closing) = ReadingsOfOnePoint(openingDay, 100m, openingDay.AddDays(days), 100m + Kwh(consumed));

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(Kwh(expected), period.AverageDailyKwh);
    }

    [Theory]
    [InlineData("0.005", "0.000")] // 0.0005 sits exactly between 0.000 and 0.001: the even neighbour is 0.000
    [InlineData("0.015", "0.002")] // 0.0015: the even neighbour is 0.002
    [InlineData("0.025", "0.002")] // 0.0025: the even neighbour is 0.002, not 0.003
    [InlineData("0.035", "0.004")] // 0.0035: the even neighbour is 0.004
    [InlineData("0.006", "0.001")] // 0.0006 is not a tie: it rounds up
    public void Average_daily_energy_rounds_a_tie_to_the_even_neighbour(string consumedOverTenDays, string expected) {
        DateOnly openingDay = new DateOnly(2024, 1, 1);
        (MeterReading opening, MeterReading closing) = ReadingsOfOnePoint(openingDay, 100m, openingDay.AddDays(10), 100m + Kwh(consumedOverTenDays));

        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        Assert.Equal(Kwh(expected), period.AverageDailyKwh);
    }

    [Theory]
    [InlineData(ReadingSource.Actual, ReadingSource.Actual, false)]
    [InlineData(ReadingSource.Actual, ReadingSource.CustomerSupplied, false)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Actual, false)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.CustomerSupplied, false)]
    [InlineData(ReadingSource.Estimated, ReadingSource.Actual, true)]
    [InlineData(ReadingSource.Actual, ReadingSource.Estimated, true)]
    [InlineData(ReadingSource.Estimated, ReadingSource.Estimated, true)]
    [InlineData(ReadingSource.Estimated, ReadingSource.CustomerSupplied, true)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Estimated, true)]
    public void Is_estimated_as_soon_as_either_end_is(ReadingSource opening, ReadingSource closing, bool expected) {
        // Arrange: the rest of the period (point, days, indexes) is whatever the generator makes of it
        ConsumptionPeriod period = Any.ConsumptionPeriod()
                                      .WithOpening(r => r.WithSource(opening))
                                      .WithClosing(r => r.WithSource(closing))
                                      .Generate();

        // Assert
        Assert.Equal(expected, period.IsEstimated);
    }

    [Fact]
    public void Refuses_two_readings_of_different_delivery_points() {
        // Arrange
        DeliveryPointId anyPoint      = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOtherPoint = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(anyPoint.Value)).Generate();
        MeterReading    opening       = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 1, 1)).WithIndexOf(100m).Generate();
        MeterReading    closing       = Any.MeterReading().WithDeliveryPoint(anyOtherPoint).WithReadingDate(new DateOnly(2024, 2, 1)).WithIndexOf(150m).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));

        // Assert
        Assert.Equal("closing", refusal.ParamName);
        Assert.Contains("same delivery point", refusal.Message);
    }

    [Fact]
    public void Accepts_two_readings_of_equal_delivery_points_that_are_not_the_same_object() {
        // Arrange: equality is by value
        MeterReading opening = Any.MeterReading().WithDeliveryPoint(new DeliveryPointId("12345678901234")).WithReadingDate(new DateOnly(2024, 1, 1)).WithIndexOf(1m).Generate();
        MeterReading closing = Any.MeterReading().WithDeliveryPoint(new DeliveryPointId("12345678901234")).WithReadingDate(new DateOnly(2024, 1, 2)).WithIndexOf(2m).Generate();

        // Assert
        Assert.Equal(1m, new ConsumptionPeriod(opening, closing).ConsumedKwh);
    }

    [Theory]
    [InlineData("2024-03-01")] // the same day
    [InlineData("2024-02-29")] // the day before
    [InlineData("2010-01-01")] // far before
    public void Refuses_a_closing_reading_that_is_not_after_the_opening_one(string closingDay) {
        (MeterReading opening, MeterReading closing) = ReadingsOfOnePoint(new DateOnly(2024, 3, 1), 100m, DateOnly.Parse(closingDay), 200m);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));

        Assert.Equal("closing", refusal.ParamName);
        Assert.Contains("after", refusal.Message);
    }

    [Fact]
    public void Refuses_an_index_that_went_down_by_a_single_thousandth() {
        (MeterReading opening, MeterReading closing) = ReadingsOfOnePoint(new DateOnly(2024, 3, 1), 500.000m, new DateOnly(2024, 3, 2), 499.999m);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));

        Assert.Equal("closing", refusal.ParamName);
        Assert.Contains("never goes down", refusal.Message);
    }

    [Fact]
    public void Refuses_a_closing_reading_whatever_the_drop_in_the_index() {
        // Arrange: how far the index fell does not matter, only that it did
        MeterIndex      anyOpeningIndex = Any.MeterIndex().WithKwh(k => k.GreaterThan(0m)).Generate();
        MeterIndex      anyLowerIndex   = Any.MeterIndex().WithKwh(k => k.LessThan(anyOpeningIndex.Kwh)).Generate();
        DeliveryPointId anyPoint        = Any.DeliveryPointId().Generate();
        MeterReading    opening         = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 1, 1)).WithIndex(anyOpeningIndex).Generate();
        MeterReading    closing         = Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(new DateOnly(2024, 1, 2)).WithIndex(anyLowerIndex).Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(opening, closing));
    }

    [Fact]
    public void Refuses_a_missing_opening_reading() {
        MeterReading anyClosing = Any.MeterReading().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(null!, anyClosing));

        Assert.Equal("opening", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_closing_reading() {
        MeterReading anyOpening = Any.MeterReading().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(anyOpening, null!));

        Assert.Equal("closing", refusal.ParamName);
    }

    // Both ends must share a delivery point; no test here is about which one it is, so it is drawn.
    private static (MeterReading Opening, MeterReading Closing) ReadingsOfOnePoint(DateOnly openingDay, decimal openingKwh, DateOnly closingDay, decimal closingKwh) {
        DeliveryPointId anyPoint = Any.DeliveryPointId().Generate();

        return (Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(openingDay).WithIndexOf(openingKwh).Generate(),
                Any.MeterReading().WithDeliveryPoint(anyPoint).WithReadingDate(closingDay).WithIndexOf(closingKwh).Generate());
    }

    private static decimal Kwh(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

}
