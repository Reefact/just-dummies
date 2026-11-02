using JustDummies;

using SmartGrid.Domain;

using Xunit;

using static SmartGrid.Tests.Regular.TestHelpers;

namespace SmartGrid.Tests.Regular;

public sealed class ConsumptionPeriodTests {

    [Fact]
    public void A_period_keeps_its_two_readings() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        MeterReading    opening  = Reading(anyPoint, 100m, new DateOnly(2024, 1, 1));
        MeterReading    closing  = Reading(anyPoint, 160m, new DateOnly(2024, 1, 31));

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(opening, closing);

        // Assert
        Assert.Same(opening, period.Opening);
        Assert.Same(closing, period.Closing);
    }

    [Theory]
    [InlineData(2024, 1, 1, 2024, 1, 2, 1)]
    [InlineData(2024, 1, 1, 2024, 1, 31, 30)]
    [InlineData(2024, 2, 28, 2024, 3, 1, 2)]   // 2024 is a leap year
    [InlineData(2023, 2, 28, 2023, 3, 1, 1)]
    [InlineData(2023, 12, 31, 2024, 1, 1, 1)]
    [InlineData(2023, 1, 1, 2024, 1, 1, 365)]
    [InlineData(2024, 1, 1, 2025, 1, 1, 366)]
    public void Days_is_the_difference_between_the_two_dates(int y1, int m1, int d1, int y2, int m2, int d2, int expectedDays) {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(Reading(anyPoint, 0m, new DateOnly(y1, m1, d1)), Reading(anyPoint, 10m, new DateOnly(y2, m2, d2)));

        // Assert
        Assert.Equal(expectedDays, period.Days);
    }

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("100", "160", "60")]
    [InlineData("1234.567", "1234.568", "0.001")]
    [InlineData("0", "99999999", "99999999")]
    [InlineData("99999999", "99999999", "0")]
    public void ConsumedKwh_is_the_difference_between_the_two_indexes(string openingKwh, string closingKwh, string expected) {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        decimal         opening  = decimal.Parse(openingKwh, System.Globalization.CultureInfo.InvariantCulture);
        decimal         closing  = decimal.Parse(closingKwh, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(Reading(anyPoint, opening, new DateOnly(2024, 1, 1)), Reading(anyPoint, closing, new DateOnly(2024, 1, 11)));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), period.ConsumedKwh);
    }

    [Theory]
    [InlineData("100", 10, "10")]
    [InlineData("10", 3, "3.333")]
    [InlineData("20", 3, "6.667")]
    [InlineData("0", 5, "0")]
    [InlineData("1", 366, "0.003")]
    public void AverageDailyKwh_is_the_consumption_per_day_rounded_to_three_decimals(string consumed, int days, string expected) {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        opening  = new DateOnly(2024, 1, 1);

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(Reading(anyPoint, 50m, opening),
                                                         Reading(anyPoint, 50m + decimal.Parse(consumed, System.Globalization.CultureInfo.InvariantCulture), opening.AddDays(days)));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), period.AverageDailyKwh);
    }

    [Theory]
    [InlineData("0.005", 2, "0.002")]   // 0.0025 -> the even neighbour is 0.002
    [InlineData("0.015", 2, "0.008")]   // 0.0075 -> the even neighbour is 0.008
    [InlineData("0.001", 2, "0")]       // 0.0005 -> 0.000
    [InlineData("0.003", 2, "0.002")]   // 0.0015 -> 0.002
    [InlineData("0.007", 2, "0.004")]   // 0.0035 -> 0.004
    public void AverageDailyKwh_rounds_a_midpoint_to_the_even_neighbour(string consumed, int days, string expected) {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        opening  = new DateOnly(2024, 6, 1);

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(Reading(anyPoint, 10m, opening),
                                                         Reading(anyPoint, 10m + decimal.Parse(consumed, System.Globalization.CultureInfo.InvariantCulture), opening.AddDays(days)));

        // Assert
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), period.AverageDailyKwh);
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
    public void A_period_is_estimated_when_either_end_is_estimated(ReadingSource openingSource, ReadingSource closingSource, bool expected) {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(Reading(anyPoint, 1m, new DateOnly(2024, 1, 1), openingSource),
                                                         Reading(anyPoint, 2m, new DateOnly(2024, 2, 1), closingSource));

        // Assert
        Assert.Equal(expected, period.IsEstimated);
    }

    [Fact]
    public void A_closing_index_equal_to_the_opening_one_is_accepted() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        MeterIndex      anyIndex = new AnyMeterIndex().Generate();
        DateOnly        opening  = new DateOnly(2024, 1, 1);

        // Act
        ConsumptionPeriod period = new ConsumptionPeriod(new MeterReading(anyPoint, anyIndex, opening, ReadingSource.Actual),
                                                         new MeterReading(anyPoint, anyIndex, opening.AddDays(1), ReadingSource.Actual));

        // Assert
        Assert.Equal(0m, period.ConsumedKwh);
        Assert.Equal(0m, period.AverageDailyKwh);
    }

    [Fact]
    public void A_null_opening_is_refused() {
        // Arrange
        MeterReading anyClosing = new AnyMeterReading().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(null!, anyClosing));
    }

    [Fact]
    public void A_null_closing_is_refused() {
        // Arrange
        MeterReading anyOpening = new AnyMeterReading().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ConsumptionPeriod(anyOpening, null!));
    }

    [Fact]
    public void Readings_of_two_delivery_points_are_refused() {
        // Arrange
        DeliveryPointId anyPoint      = new AnyDeliveryPointId().Generate();
        DeliveryPointId anyOtherPoint = new AnyDeliveryPointId().WithValue(v => v.DifferentFrom(anyPoint.Value)).Generate();

        // Act & Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(Reading(anyPoint, 10m, new DateOnly(2024, 1, 1)),
                                                                                                 Reading(anyOtherPoint, 20m, new DateOnly(2024, 2, 1))));
        Assert.Contains("delivery point", refusal.Message);
    }

    [Fact]
    public void A_closing_on_the_opening_day_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        day      = Any.DateOnly().Between(new DateOnly(2010, 1, 1), new DateOnly(2090, 1, 1)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(Reading(anyPoint, 10m, day), Reading(anyPoint, 20m, day)));
    }

    [Fact]
    public void A_closing_the_day_before_the_opening_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        opening  = Any.DateOnly().Between(new DateOnly(2011, 1, 1), new DateOnly(2090, 1, 1)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(Reading(anyPoint, 10m, opening), Reading(anyPoint, 20m, opening.AddDays(-1))));
    }

    [Fact]
    public void A_closing_index_below_the_opening_one_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ConsumptionPeriod(Reading(anyPoint, 10.001m, new DateOnly(2024, 1, 1)),
                                                                     Reading(anyPoint, 10m, new DateOnly(2024, 2, 1))));
    }

    [Fact]
    public void Every_rule_refuses_with_an_ArgumentException_and_not_a_subtype() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();

        // Act
        Exception? sameDay   = Record.Exception(() => new ConsumptionPeriod(Reading(anyPoint, 1m, new DateOnly(2024, 1, 1)), Reading(anyPoint, 2m, new DateOnly(2024, 1, 1))));
        Exception? indexDown = Record.Exception(() => new ConsumptionPeriod(Reading(anyPoint, 2m, new DateOnly(2024, 1, 1)), Reading(anyPoint, 1m, new DateOnly(2024, 1, 2))));

        // Assert
        Assert.IsType<ArgumentException>(sameDay);
        Assert.IsType<ArgumentException>(indexDown);
    }

}
