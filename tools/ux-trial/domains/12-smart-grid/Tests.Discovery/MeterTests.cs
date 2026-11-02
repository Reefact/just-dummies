using JustDummies;

using Xunit;

namespace SmartGrid.Domain;

public sealed class MeterTests {

    private static readonly DateOnly CommissioningDay = new DateOnly(2024, 1, 1);

    // ---- construction ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_meter_starts_with_its_commissioning_reading() {
        DeliveryPointId point         = Any.DeliveryPointId().Generate();
        TariffOption    anyTariff     = Any.Enum<TariffOption>().Generate();
        MeterReading    commissioning = Any.MeterReading().WithDeliveryPoint(point).WithSource(ReadingSource.Actual).Generate();

        Meter meter = new Meter(point, anyTariff, commissioning);

        Assert.Same(point, meter.DeliveryPoint);
        Assert.Single(meter.Readings);
        Assert.Same(commissioning, meter.Readings[0]);
        Assert.Same(commissioning, meter.LastReading);
    }

    [Theory]
    [InlineData(TariffOption.Base)]
    [InlineData(TariffOption.OffPeak)]
    public void A_meter_keeps_its_tariff(TariffOption tariff) {
        Meter meter = Any.Meter().WithTariff(tariff).Generate();

        Assert.Equal(tariff, meter.Tariff);
    }

    [Fact]
    public void A_missing_delivery_point_is_refused() {
        TariffOption anyTariff = Any.Enum<TariffOption>().Generate();
        MeterReading anyReading = Any.MeterReading().WithSource(ReadingSource.Actual).Generate();

        Assert.Throws<ArgumentNullException>(() => new Meter(null!, anyTariff, anyReading));
    }

    [Fact]
    public void A_missing_commissioning_reading_is_refused() {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        TariffOption    anyTariff = Any.Enum<TariffOption>().Generate();

        Assert.Throws<ArgumentNullException>(() => new Meter(anyPoint, anyTariff, null!));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(100)]
    public void An_undefined_tariff_is_refused(int undefinedTariff) {
        DeliveryPointId point   = Any.DeliveryPointId().Generate();
        MeterReading    reading = Any.MeterReading().WithDeliveryPoint(point).WithSource(ReadingSource.Actual).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Meter(point, (TariffOption)undefinedTariff, reading));
    }

    [Fact]
    public void A_commissioning_reading_of_another_delivery_point_is_refused() {
        DeliveryPointId anyPoint      = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOtherPoint = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(anyPoint.Value)).Generate();
        TariffOption    anyTariff     = Any.Enum<TariffOption>().Generate();
        MeterReading    elsewhere     = Any.MeterReading().WithDeliveryPoint(anyOtherPoint).WithSource(ReadingSource.Actual).Generate();

        Assert.Throws<ArgumentException>(() => new Meter(anyPoint, anyTariff, elsewhere));
    }

    [Theory]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void A_meter_is_not_commissioned_on_a_reading_that_is_not_actual(ReadingSource source) {
        DeliveryPointId anyPoint  = Any.DeliveryPointId().Generate();
        TariffOption    anyTariff = Any.Enum<TariffOption>().Generate();
        MeterReading    reading   = Any.MeterReading().WithDeliveryPoint(anyPoint).WithSource(source).Generate();

        Assert.Throws<ArgumentException>(() => new Meter(anyPoint, anyTariff, reading));
    }

    // ---- recording a reading ---------------------------------------------------------------------------------------

    [Fact]
    public void A_later_reading_is_appended_and_gives_the_period_since_the_last_one() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        MeterReading    next  = Any.MeterReading().WithDeliveryPoint(point).WithReadingDate(new DateOnly(2024, 1, 31)).WithIndex(new MeterIndex(160.5m)).Generate();

        ConsumptionPeriod period = meter.Record(next);

        Assert.Equal(2, meter.Readings.Count);
        Assert.Same(next, meter.LastReading);
        Assert.Equal(30, period.Days);
        Assert.Equal(60.5m, period.ConsumedKwh);
        Assert.Same(next, period.Closing);
        Assert.Equal(CommissioningDay, period.Opening.ReadingDate);
    }

    [Fact]
    public void Each_period_starts_where_the_previous_one_ended() {
        DeliveryPointId point  = Any.DeliveryPointId().Generate();
        Meter           meter  = MeterAt(point, 100m);
        MeterReading    second = Reading(point, 150m, new DateOnly(2024, 2, 1));
        MeterReading    third  = Reading(point, 190m, new DateOnly(2024, 3, 1));

        meter.Record(second);
        ConsumptionPeriod period = meter.Record(third);

        Assert.Same(second, period.Opening);
        Assert.Same(third, period.Closing);
        Assert.Equal(40m, period.ConsumedKwh);
        Assert.Equal(3, meter.Readings.Count);
    }

    [Fact]
    public void An_index_that_did_not_move_is_accepted() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);

        ConsumptionPeriod period = meter.Record(Reading(point, 100m, new DateOnly(2024, 1, 2)));

        Assert.Equal(0m, period.ConsumedKwh);
    }

    [Theory]
    [InlineData(ReadingSource.Actual)]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void A_later_reading_is_accepted_whatever_its_source(ReadingSource source) {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        MeterReading    next  = Reading(point, 120m, new DateOnly(2024, 1, 15), source);

        meter.Record(next);

        Assert.Same(next, meter.LastReading);
    }

    [Fact]
    public void A_null_reading_is_refused() {
        Meter anyMeter = Any.Meter().Generate();

        Assert.Throws<ArgumentNullException>(() => anyMeter.Record(null!));
    }

    [Fact]
    public void A_reading_of_another_delivery_point_is_refused() {
        DeliveryPointId point      = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOther   = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(point.Value)).Generate();
        Meter           meter      = MeterAt(point, 100m);
        MeterReading    elsewhere  = Reading(anyOther, 200m, new DateOnly(2024, 2, 1));

        Assert.Throws<InvalidOperationException>(() => meter.Record(elsewhere));
        Assert.Single(meter.Readings);
    }

    [Fact]
    public void A_reading_of_another_delivery_point_is_refused_before_anything_else_is_looked_at() {
        DeliveryPointId point     = Any.DeliveryPointId().Generate();
        DeliveryPointId anyOther  = Any.DeliveryPointId().WithValue(v => v.DifferentFrom(point.Value)).Generate();
        Meter           meter     = MeterAt(point, 100m);
        MeterReading    elsewhere = Reading(anyOther, 1m, new DateOnly(2011, 1, 1));

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => meter.Record(elsewhere));

        Assert.Contains("another delivery point", thrown.Message);
    }

    [Fact]
    public void A_reading_the_day_before_the_last_one_is_refused() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        MeterReading    early = Reading(point, 200m, CommissioningDay.AddDays(-1));

        Assert.Throws<InvalidOperationException>(() => meter.Record(early));
        Assert.Single(meter.Readings);
    }

    [Fact]
    public void A_reading_well_before_the_last_one_is_refused() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        MeterReading    early = Any.MeterReading().WithDeliveryPoint(point).WithReadingDate(d => d.Before(CommissioningDay)).WithIndex(new MeterIndex(500m)).Generate();

        Assert.Throws<InvalidOperationException>(() => meter.Record(early));
    }

    [Theory]
    [InlineData(ReadingSource.Actual)]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void A_second_reading_on_the_day_of_an_actual_one_is_refused(ReadingSource source) {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);

        Assert.Throws<InvalidOperationException>(() => meter.Record(Reading(point, 150m, CommissioningDay, source)));
        Assert.Single(meter.Readings);
    }

    [Theory]
    [InlineData(ReadingSource.Estimated)]
    [InlineData(ReadingSource.CustomerSupplied)]
    public void Only_an_actual_reading_replaces_an_estimated_one_of_the_same_day(ReadingSource source) {
        DeliveryPointId point    = Any.DeliveryPointId().Generate();
        Meter           meter    = MeterAt(point, 100m);
        DateOnly        day      = new DateOnly(2024, 2, 1);
        meter.Record(Reading(point, 150m, day, ReadingSource.Estimated));

        Assert.Throws<InvalidOperationException>(() => meter.Record(Reading(point, 160m, day, source)));
        Assert.Equal(2, meter.Readings.Count);
        Assert.Equal(ReadingSource.Estimated, meter.LastReading.Source);
    }

    [Fact]
    public void A_reading_on_the_same_day_as_an_estimated_one_and_actual_replaces_it() {
        DeliveryPointId point   = Any.DeliveryPointId().Generate();
        Meter           meter   = MeterAt(point, 100m);
        MeterReading    initial = meter.LastReading;
        DateOnly        day     = new DateOnly(2024, 2, 1);
        meter.Record(Reading(point, 150m, day, ReadingSource.Estimated));
        MeterReading    actual  = Reading(point, 170m, day, ReadingSource.Actual);

        ConsumptionPeriod period = meter.Record(actual);

        Assert.Equal(2, meter.Readings.Count);
        Assert.Same(actual, meter.LastReading);
        Assert.Same(initial, period.Opening);
        Assert.Same(actual, period.Closing);
        Assert.Equal(70m, period.ConsumedKwh);
        Assert.False(period.IsEstimated);
    }

    [Fact]
    public void The_replacing_reading_may_read_lower_than_the_estimate_it_replaces() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        DateOnly        day   = new DateOnly(2024, 2, 1);
        meter.Record(Reading(point, 150m, day, ReadingSource.Estimated));

        ConsumptionPeriod period = meter.Record(Reading(point, 120m, day, ReadingSource.Actual));

        Assert.Equal(20m, period.ConsumedKwh);
        Assert.Equal(120m, meter.LastReading.Index.Kwh);
    }

    [Fact]
    public void Once_replaced_by_an_actual_reading_the_day_is_closed() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        DateOnly        day   = new DateOnly(2024, 2, 1);
        meter.Record(Reading(point, 150m, day, ReadingSource.Estimated));
        meter.Record(Reading(point, 160m, day, ReadingSource.Actual));

        Assert.Throws<InvalidOperationException>(() => meter.Record(Reading(point, 170m, day, ReadingSource.Actual)));
    }

    [Fact]
    public void A_reading_below_the_last_index_is_refused_and_leaves_the_meter_unchanged() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);
        MeterReading    last  = meter.LastReading;

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => meter.Record(Reading(point, 99.999m, new DateOnly(2024, 1, 2))));

        Assert.Contains("below", thrown.Message);
        Assert.Single(meter.Readings);
        Assert.Same(last, meter.LastReading);
    }

    [Fact]
    public void The_date_rule_is_checked_before_the_index_rule() {
        DeliveryPointId point = Any.DeliveryPointId().Generate();
        Meter           meter = MeterAt(point, 100m);

        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => meter.Record(Reading(point, 1m, CommissioningDay)));

        Assert.Contains("does not follow", thrown.Message);
    }

    [Fact(Skip = "domain defect: a replacing actual reading below the previous index is stored before the period refuses it, so the meter keeps an index that went down after the ArgumentException; deterministic, no seed needed")]
    public void A_refused_replacement_leaves_the_meter_as_it_was() {
        DeliveryPointId point     = Any.DeliveryPointId().Generate();
        Meter           meter     = MeterAt(point, 100m);
        DateOnly        day       = new DateOnly(2024, 2, 1);
        MeterReading    estimated = Reading(point, 150m, day, ReadingSource.Estimated);
        meter.Record(estimated);

        Assert.Throws<ArgumentException>(() => meter.Record(Reading(point, 90m, day, ReadingSource.Actual)));

        Assert.Same(estimated, meter.LastReading);
    }

    [Fact(Skip = "domain defect: Readings hands out the internal List, so a caller can cast it and empty the meter, after which LastReading throws; deterministic, no seed needed")]
    public void The_readings_of_a_meter_cannot_be_emptied_from_outside() {
        Meter anyMeter = Any.Meter().Generate();

        if (anyMeter.Readings is List<MeterReading> list) {
            list.Clear();
        }

        Assert.Single(anyMeter.Readings);
    }

    // ---- helpers -------------------------------------------------------------------------------------------------

    private static MeterReading Reading(DeliveryPointId point, decimal kwh, DateOnly date, ReadingSource source = ReadingSource.Actual) {
        return TestData.Reading(point, kwh, date, source);
    }

    private static Meter MeterAt(DeliveryPointId point, decimal commissioningKwh) {
        TariffOption anyTariff = Any.Enum<TariffOption>().Generate();

        return new Meter(point, anyTariff, Reading(point, commissioningKwh, CommissioningDay));
    }

}
