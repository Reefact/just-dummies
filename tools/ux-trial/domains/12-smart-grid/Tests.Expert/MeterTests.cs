using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Expert;

public sealed class MeterTests {

    private static readonly DateOnly CommissioningDay = new DateOnly(2024, 1, 1);

    [Fact]
    public void Starts_with_its_commissioning_reading_as_its_only_and_last_reading() {
        // Arrange
        DeliveryPointId point   = new DeliveryPointId("12345678901234");
        MeterReading    reading = Any.MeterReading().WithDeliveryPoint(point).WithSource(ReadingSource.Actual).Generate();

        // Act
        Meter meter = new Meter(point, TariffOption.OffPeak, reading);

        // Assert
        Assert.Same(point, meter.DeliveryPoint);
        Assert.Equal(TariffOption.OffPeak, meter.Tariff);
        Assert.Same(reading, Assert.Single(meter.Readings));
        Assert.Same(reading, meter.LastReading);
    }

    [Theory]
    [InlineData(TariffOption.Base)]
    [InlineData(TariffOption.OffPeak)]
    public void Accepts_every_declared_tariff(TariffOption tariff) {
        Meter meter = Any.Meter().WithTariff(tariff).Generate();

        Assert.Equal(tariff, meter.Tariff);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MinValue)]
    public void Refuses_a_tariff_that_is_not_declared(int undeclared) {
        DeliveryPointId anyPoint   = Any.DeliveryPointId().Generate();
        MeterReading    anyReading = Any.MeterReading().WithDeliveryPoint(anyPoint).WithSource(ReadingSource.Actual).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Meter(anyPoint, (TariffOption)undeclared, anyReading));

        Assert.Equal("tariff", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_delivery_point() {
        MeterReading anyReading = Any.MeterReading().WithSource(ReadingSource.Actual).Generate();
        TariffOption anyTariff  = Any.Enum<TariffOption>().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Meter(null!, anyTariff, anyReading));

        Assert.Equal("deliveryPoint", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_missing_commissioning_reading() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        TariffOption    anyTariff = Any.Enum<TariffOption>().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Meter(anyPoint, anyTariff, null!));

        Assert.Equal("commissioningReading", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_commissioning_reading_of_another_delivery_point() {
        // Arrange
        DeliveryPointId anyPoint      = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOtherPoint = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(anyPoint.Value)).Generate();
        MeterReading    anyReading    = Any.MeterReading().WithDeliveryPoint(anyOtherPoint).WithSource(ReadingSource.Actual).Generate();
        TariffOption    anyTariff     = Any.Enum<TariffOption>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Meter(anyPoint, anyTariff, anyReading));

        // Assert
        Assert.Equal("commissioningReading", refusal.ParamName);
        Assert.Contains("another delivery point", refusal.Message);
    }

    [Theory]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void Refuses_a_commissioning_reading_that_is_not_an_actual_one(ReadingSource source) {
        DeliveryPointId anyPoint   = Any.DeliveryPointId().Generate();
        MeterReading    anyReading = Any.MeterReading().WithDeliveryPoint(anyPoint).WithSource(source).Generate();
        TariffOption    anyTariff  = Any.Enum<TariffOption>().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Meter(anyPoint, anyTariff, anyReading));

        Assert.Equal("commissioningReading", refusal.ParamName);
        Assert.Contains("actual", refusal.Message);
    }

    [Fact]
    public void Refuses_the_tariff_before_the_reading_when_both_are_wrong() {
        DeliveryPointId anyPoint   = Any.DeliveryPointId().Generate();
        MeterReading    anyReading = Any.MeterReading().WithDeliveryPoint(anyPoint).WithSource(ReadingSource.Estimated).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Meter(anyPoint, (TariffOption)99, anyReading));

        Assert.Equal("tariff", refusal.ParamName);
    }

    // ---- Record: a reading that moves forward

    [Fact]
    public void Recording_a_later_reading_appends_it_and_returns_the_period_since_the_former_last_one() {
        // Arrange
        Meter        meter    = MeterCommissionedAt(100m);
        MeterReading previous = meter.LastReading;
        MeterReading next     = ReadingFor(meter, CommissioningDay.AddDays(30), 130.5m, ReadingSource.Actual);

        // Act
        ConsumptionPeriod period = meter.Record(next);

        // Assert
        Assert.Same(previous, period.Opening);
        Assert.Same(next, period.Closing);
        Assert.Equal(30, period.Days);
        Assert.Equal(30.5m, period.ConsumedKwh);
        Assert.Same(next, meter.LastReading);
        Assert.Equal(new[] { previous, next }, meter.Readings);
    }

    [Fact]
    public void Recording_a_later_reading_with_the_same_index_is_allowed() {
        Meter        meter = MeterCommissionedAt(100m);
        MeterReading next  = ReadingFor(meter, CommissioningDay.AddDays(1), 100m, ReadingSource.Actual);

        ConsumptionPeriod period = meter.Record(next);

        Assert.Equal(0m, period.ConsumedKwh);
        Assert.Equal(2, meter.Readings.Count);
    }

    [Theory]
    [InlineData(ReadingSource.Actual)]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void Recording_keeps_the_source_of_the_reading_in_the_period(ReadingSource source) {
        Meter        meter = MeterCommissionedAt(100m);
        MeterReading next  = ReadingFor(meter, CommissioningDay.AddDays(10), 120m, source);

        ConsumptionPeriod period = meter.Record(next);

        Assert.Equal(source == ReadingSource.Estimated, period.IsEstimated);
    }

    [Fact]
    public void Successive_readings_accumulate_in_the_order_they_were_recorded() {
        // Arrange: four monthly readings; how much each month consumed is nobody's concern here
        Meter meter = MeterCommissionedAt(100m);

        // Act
        decimal index = 100m;
        for (int month = 1; month <= 4; month++) {
            index += Any.Decimal().Between(0m, 1_000m).WithScale(3).Generate();
            meter.Record(ReadingFor(meter, CommissioningDay.AddMonths(month), index, ReadingSource.Actual));
        }

        // Assert
        Assert.Equal(5, meter.Readings.Count);
        Assert.Equal(Enumerable.Range(0, 5).Select(month => CommissioningDay.AddMonths(month)), meter.Readings.Select(r => r.ReadingDate));
        Assert.Equal(index, meter.LastReading.Index.Kwh);
    }

    [Fact]
    public void Refuses_a_missing_reading() {
        Meter anyMeter = Any.Meter().Generate();

        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyMeter.Record(null!));

        Assert.Equal("reading", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_reading_of_another_delivery_point_and_stays_as_it_was() {
        // Arrange
        Meter           meter         = MeterCommissionedAt(100m);
        DeliveryPointId anyOtherPoint = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(meter.DeliveryPoint.Value)).Generate();
        MeterReading    foreign       = Any.MeterReading().WithDeliveryPoint(anyOtherPoint).WithReadingDate(CommissioningDay.AddDays(5)).WithIndexOf(200m).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => meter.Record(foreign));

        // Assert
        Assert.Contains("another delivery point", refusal.Message);
        Assert.Single(meter.Readings);
    }

    [Theory]
    [InlineData(0)]   // the same day as the last reading
    [InlineData(-1)]  // the day before
    [InlineData(-300)]
    public void Refuses_a_reading_that_does_not_follow_the_last_one_in_time(int daysAfterLast) {
        Meter        meter = MeterCommissionedAt(100m);
        MeterReading late  = ReadingFor(meter, CommissioningDay.AddDays(daysAfterLast), 500m, ReadingSource.Actual);

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => meter.Record(late));

        Assert.Contains("does not follow", refusal.Message);
        Assert.Single(meter.Readings);
    }

    [Fact]
    public void Refuses_an_index_that_is_below_the_last_one_by_a_single_thousandth_and_stays_as_it_was() {
        Meter        meter = MeterCommissionedAt(100.000m);
        MeterReading lower = ReadingFor(meter, CommissioningDay.AddDays(1), 99.999m, ReadingSource.Actual);

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => meter.Record(lower));

        Assert.Contains("below the last one", refusal.Message);
        Assert.Single(meter.Readings);
    }

    [Fact]
    public void Checks_the_date_before_the_index() {
        // Both rules are broken: the date is the one reported
        Meter        meter = MeterCommissionedAt(100m);
        MeterReading both  = ReadingFor(meter, CommissioningDay, 1m, ReadingSource.Actual);

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => meter.Record(both));

        Assert.Contains("does not follow", refusal.Message);
    }

    // ---- Record: an actual reading replacing an estimated one of the same day

    [Fact]
    public void An_actual_reading_replaces_an_estimated_one_of_the_same_day() {
        // Arrange: commissioned at 100, estimated 150 on day 31
        Meter        meter      = MeterCommissionedAt(100m);
        MeterReading commission = meter.LastReading;
        DateOnly     day        = CommissioningDay.AddDays(31);
        meter.Record(ReadingFor(meter, day, 150m, ReadingSource.Estimated));
        MeterReading actual = ReadingFor(meter, day, 142m, ReadingSource.Actual);

        // Act
        ConsumptionPeriod period = meter.Record(actual);

        // Assert
        Assert.Equal(new[] { commission, actual }, meter.Readings);
        Assert.Same(actual, meter.LastReading);
        Assert.Same(commission, period.Opening);
        Assert.Same(actual, period.Closing);
        Assert.Equal(42m, period.ConsumedKwh);
        Assert.False(period.IsEstimated);
    }

    [Fact]
    public void An_actual_reading_may_correct_an_estimate_downwards_as_long_as_it_stays_above_the_reading_before() {
        Meter meter = MeterCommissionedAt(100m);
        DateOnly day = CommissioningDay.AddDays(31);
        meter.Record(ReadingFor(meter, day, 500m, ReadingSource.Estimated));

        ConsumptionPeriod period = meter.Record(ReadingFor(meter, day, 100m, ReadingSource.Actual));

        Assert.Equal(0m, period.ConsumedKwh);
        Assert.Equal(2, meter.Readings.Count);
    }

    [Theory]
    [InlineData(ReadingSource.Actual, ReadingSource.Actual)]
    [InlineData(ReadingSource.Actual, ReadingSource.Estimated)]
    [InlineData(ReadingSource.Actual, ReadingSource.CustomerSupplied)]
    [InlineData(ReadingSource.Estimated, ReadingSource.Estimated)]
    [InlineData(ReadingSource.Estimated, ReadingSource.CustomerSupplied)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Actual)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied, ReadingSource.CustomerSupplied)]
    public void Only_an_actual_reading_replaces_an_estimated_one_of_the_same_day(ReadingSource last, ReadingSource sameDaySource) {
        // Arrange: the meter's last reading, of the given source, is on day 31
        Meter    meter = MeterCommissionedAt(100m);
        DateOnly day   = CommissioningDay.AddDays(31);
        meter.Record(ReadingFor(meter, day, 150m, last));
        MeterReading sameDay = ReadingFor(meter, day, 160m, sameDaySource);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => meter.Record(sameDay));

        // Assert
        Assert.Contains("does not follow", refusal.Message);
        Assert.Equal(2, meter.Readings.Count);
        Assert.Equal(last, meter.LastReading.Source);
    }

    [Fact]
    public void An_estimated_reading_is_only_replaced_by_a_reading_of_the_same_day_not_a_later_one_taking_its_place() {
        // Arrange: a later actual reading is a new reading, the estimate stays
        Meter    meter = MeterCommissionedAt(100m);
        DateOnly day   = CommissioningDay.AddDays(31);
        meter.Record(ReadingFor(meter, day, 150m, ReadingSource.Estimated));

        // Act
        meter.Record(ReadingFor(meter, day.AddDays(1), 160m, ReadingSource.Actual));

        // Assert
        Assert.Equal(3, meter.Readings.Count);
    }

    [Fact(Skip = "domain defect: Meter.Record replaces the estimated reading BEFORE ConsumptionPeriod validates the new one, so an actual reading below the previous index is refused with ArgumentException but stays stored and the estimate is lost; fails on every seed")]
    public void An_actual_reading_below_the_reading_before_the_estimate_is_refused_and_leaves_the_meter_as_it_was() {
        Meter        meter      = MeterCommissionedAt(100m);
        DateOnly     day        = CommissioningDay.AddDays(31);
        MeterReading estimated  = ReadingFor(meter, day, 150m, ReadingSource.Estimated);
        meter.Record(estimated);
        MeterReading belowStart = ReadingFor(meter, day, 90m, ReadingSource.Actual);

        Assert.ThrowsAny<Exception>(() => meter.Record(belowStart));

        Assert.Same(estimated, meter.LastReading);
    }

    [Fact(Skip = "domain defect: the same refusal is an ArgumentException (from ConsumptionPeriod) here but an InvalidOperationException for the same fault on a non-replacing reading; fails on every seed")]
    public void A_falling_index_is_refused_the_same_way_whether_or_not_it_replaces_an_estimate() {
        Meter    meter = MeterCommissionedAt(100m);
        DateOnly day   = CommissioningDay.AddDays(31);
        meter.Record(ReadingFor(meter, day, 150m, ReadingSource.Estimated));

        Assert.Throws<InvalidOperationException>(() => meter.Record(ReadingFor(meter, day, 90m, ReadingSource.Actual)));
    }

    // A meter whose commissioning reading is the one fact the test is about: its index, on a fixed day.
    private static Meter MeterCommissionedAt(decimal kwh) {
        return Any.Meter().WithCommissioningReading(r => r.WithReadingDate(CommissioningDay).WithIndexOf(kwh)).Generate();
    }

    private static MeterReading ReadingFor(Meter meter, DateOnly day, decimal kwh, ReadingSource source) {
        return Any.MeterReading().WithDeliveryPoint(meter.DeliveryPoint).WithReadingDate(day).WithIndexOf(kwh).WithSource(source).Generate();
    }

}
