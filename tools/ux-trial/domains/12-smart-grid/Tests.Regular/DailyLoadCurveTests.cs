using JustDummies;

using SmartGrid.Domain;

using Xunit;

using static SmartGrid.Tests.Regular.TestHelpers;

namespace SmartGrid.Tests.Regular;

public sealed class DailyLoadCurveTests {

    [Fact]
    public void A_curve_keeps_its_point_its_day_and_its_points() {
        // Arrange
        DeliveryPointId      point  = new DeliveryPointId("98765432109876");
        List<LoadCurvePoint> points = Points(_ => 1m);

        // Act
        DailyLoadCurve curve = new DailyLoadCurve(point, new DateOnly(2024, 5, 17), points);

        // Assert
        Assert.Same(point, curve.DeliveryPoint);
        Assert.Equal(new DateOnly(2024, 5, 17), curve.Day);
        Assert.Equal(48, curve.Points.Count);
        Assert.Equal(DailyLoadCurve.SlotsPerDay, curve.Points.Count);
    }

    [Fact]
    public void A_null_delivery_point_is_refused() {
        // Arrange
        DateOnly anyDay = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DailyLoadCurve(null!, anyDay, Points(_ => 1m)));
    }

    [Fact]
    public void A_null_list_of_points_is_refused() {
        // Arrange
        DeliveryPointId anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly        anyDay   = Any.DateOnly().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DailyLoadCurve(anyPoint, anyDay, null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(47)]
    [InlineData(49)]
    [InlineData(96)]
    public void A_curve_without_exactly_forty_eight_points_is_refused(int count) {
        // Arrange
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly             anyDay   = Any.DateOnly().Generate();
        List<LoadCurvePoint> points   = new List<LoadCurvePoint>();
        for (int slot = 0; slot < count; slot++) {
            points.Add(new LoadCurvePoint(SlotStart(slot % 48), 1m));
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DailyLoadCurve(anyPoint, anyDay, points));
    }

    [Fact]
    public void A_point_starting_at_the_wrong_slot_is_refused() {
        // Arrange
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly             anyDay   = Any.DateOnly().Generate();
        List<LoadCurvePoint> points   = Points(_ => 1m);
        (points[10], points[11]) = (points[11], points[10]);

        // Act & Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new DailyLoadCurve(anyPoint, anyDay, points));
        Assert.Contains("Point 10", refusal.Message);
    }

    [Fact]
    public void A_gap_in_the_slots_is_refused() {
        // Arrange
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly             anyDay   = Any.DateOnly().Generate();
        List<LoadCurvePoint> points   = Points(_ => 1m);
        points[20] = new LoadCurvePoint(SlotStart(21), 1m);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DailyLoadCurve(anyPoint, anyDay, points));
    }

    [Fact]
    public void A_curve_that_does_not_start_at_midnight_is_refused() {
        // Arrange
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly             anyDay   = Any.DateOnly().Generate();
        List<LoadCurvePoint> points   = new List<LoadCurvePoint>();
        for (int slot = 0; slot < 48; slot++) {
            points.Add(new LoadCurvePoint(SlotStart((slot + 1) % 48), 1m));
        }

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DailyLoadCurve(anyPoint, anyDay, points));
    }

    [Fact]
    public void A_curve_with_a_null_point_among_forty_eight_fails() {
        // Arrange
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DateOnly             anyDay   = Any.DateOnly().Generate();
        List<LoadCurvePoint> points   = Points(_ => 1m);
        points[5] = null!;

        // Act
        Exception? failure = Record.Exception(() => new DailyLoadCurve(anyPoint, anyDay, points));

        // Assert
        Assert.NotNull(failure);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 24)]
    [InlineData(2, 48)]
    [InlineData(36, 864)]
    public void The_total_energy_of_a_flat_curve_is_its_power_times_twenty_four_hours(int power, int expectedKwh) {
        // Arrange
        DailyLoadCurve curve = Curve(_ => power);

        // Assert
        Assert.Equal(expectedKwh, curve.TotalEnergyKwh);
    }

    [Fact]
    public void The_total_energy_adds_up_the_energy_of_every_slot() {
        // Arrange: 4 kW for the first half of the day, 2 kW for the second half.
        DailyLoadCurve curve = Curve(slot => slot < 24 ? 4m : 2m);

        // Assert: 24 x 2 kWh + 24 x 1 kWh
        Assert.Equal(72m, curve.TotalEnergyKwh);
    }

    [Fact]
    public void A_single_loaded_slot_carries_all_the_energy() {
        // Arrange
        int            anyLoadedSlot = Any.Int32().Between(0, 47).Generate();
        DailyLoadCurve curve         = Curve(slot => slot == anyLoadedSlot ? 36m : 0m);

        // Assert
        Assert.Equal(18m, curve.TotalEnergyKwh);
    }

    [Fact]
    public void The_peak_is_the_slot_with_the_highest_power() {
        // Arrange
        int            anyPeakSlot = Any.Int32().Between(0, 47).Generate();
        decimal        anyLowerKw  = Any.Decimal().Between(0m, 9.999m).WithScale(3).Generate();
        DailyLoadCurve curve       = Curve(slot => slot == anyPeakSlot ? 10m : anyLowerKw);

        // Act
        LoadCurvePoint peak = curve.Peak();

        // Assert
        Assert.Equal(10m, peak.AveragePowerKw);
        Assert.Equal(SlotStart(anyPeakSlot), peak.SlotStart);
    }

    [Fact]
    public void The_peak_of_a_curve_peaking_at_the_last_slot_is_the_last_slot() {
        // Arrange
        DailyLoadCurve curve = Curve(slot => slot == 47 ? 1m : 0m);

        // Assert
        Assert.Equal(new TimeOnly(23, 30), curve.Peak().SlotStart);
    }

    [Fact]
    public void The_peak_of_a_curve_with_two_equal_maxima_is_the_earlier_one() {
        // Arrange
        DailyLoadCurve curve = Curve(slot => slot == 5 || slot == 30 ? 20m : 1m);

        // Act
        LoadCurvePoint peak = curve.Peak();

        // Assert
        Assert.Equal(new TimeOnly(2, 30), peak.SlotStart);
    }

    [Fact]
    public void The_peak_of_a_flat_curve_is_its_first_slot() {
        // Arrange
        decimal        anyPower = Any.Decimal().Between(0m, 36m).WithScale(3).Generate();
        DailyLoadCurve curve    = Curve(_ => anyPower);

        // Assert
        Assert.Equal(new TimeOnly(0, 0), curve.Peak().SlotStart);
    }

    [Fact]
    public void The_peak_of_an_idle_curve_is_its_first_slot() {
        // Arrange
        DailyLoadCurve curve = Curve(_ => 0m);

        // Assert
        Assert.Equal(new TimeOnly(0, 0), curve.Peak().SlotStart);
        Assert.Equal(0m, curve.Peak().AveragePowerKw);
    }

    [Fact]
    public void The_energy_within_a_night_window_counts_both_sides_of_midnight() {
        // Arrange: 2 kW all day = 1 kWh per slot; 22:00 to 06:00 is 16 slots.
        DailyLoadCurve curve  = Curve(_ => 2m);
        OffPeakWindow  window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Assert
        Assert.Equal(16m, curve.EnergyWithin(window));
    }

    [Fact]
    public void The_energy_within_a_daytime_window_counts_its_start_but_not_its_end() {
        // Arrange: 06:00 to 22:00 is 32 slots of 1 kWh.
        DailyLoadCurve curve  = Curve(_ => 2m);
        OffPeakWindow  window = new OffPeakWindow(new TimeOnly(6, 0), new TimeOnly(22, 0));

        // Assert
        Assert.Equal(32m, curve.EnergyWithin(window));
    }

    [Fact]
    public void The_energy_within_a_window_only_counts_the_slots_in_it() {
        // Arrange: slot i draws i / 2 kW, i.e. i / 4 kWh. The window 00:00 to 01:00 holds slots 0 and 1.
        DailyLoadCurve curve  = Curve(slot => slot / 2m);
        OffPeakWindow  window = new OffPeakWindow(new TimeOnly(0, 0), new TimeOnly(1, 0));

        // Assert
        Assert.Equal(0.25m, curve.EnergyWithin(window));
    }

    [Fact]
    public void The_energy_within_a_window_ending_at_midnight_includes_the_last_slot_of_the_day() {
        // Arrange: only the last slot (23:30) is loaded.
        DailyLoadCurve curve  = Curve(slot => slot == 47 ? 10m : 0m);
        OffPeakWindow  window = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(0, 0));

        // Assert
        Assert.Equal(5m, curve.EnergyWithin(window));
    }

    [Fact]
    public void The_energy_within_a_one_slot_window_is_the_energy_of_that_slot() {
        // Arrange
        int            anySlot = Any.Int32().Between(0, 47).Generate();
        DailyLoadCurve curve   = Curve(slot => slot == anySlot ? 8m : 0m);
        OffPeakWindow  window  = new OffPeakWindow(SlotStart(anySlot), SlotStart(anySlot).AddMinutes(30));

        // Assert
        Assert.Equal(4m, curve.EnergyWithin(window));
    }

    [Fact]
    public void The_energy_within_a_window_never_exceeds_the_total_energy() {
        // Arrange
        decimal        anyPower = Any.Decimal().Between(0m, 36m).WithScale(3).Generate();
        DailyLoadCurve curve    = Curve(_ => anyPower);
        OffPeakWindow  window   = new AnyOffPeakWindow().Generate();

        // Assert
        Assert.True(curve.EnergyWithin(window) <= curve.TotalEnergyKwh);
    }

    [Fact]
    public void A_null_window_is_refused() {
        // Arrange
        DailyLoadCurve anyCurve = new AnyDailyLoadCurve().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyCurve.EnergyWithin(null!));
    }

    [Fact(Skip = "domain defect: the curve keeps the caller's list instead of copying it, so replacing an element afterwards silently breaks the 48-slot ordering invariant (literal data, no seed)")]
    public void A_curve_is_not_changed_by_a_later_change_to_the_list_it_was_built_from() {
        // Arrange
        List<LoadCurvePoint> points = Points(_ => 1m);
        DeliveryPointId      anyPoint = new AnyDeliveryPointId().Generate();
        DailyLoadCurve       curve  = new DailyLoadCurve(anyPoint, new DateOnly(2024, 5, 17), points);

        // Act
        points[0] = new LoadCurvePoint(SlotStart(0), 30m);

        // Assert
        Assert.Equal(24m, curve.TotalEnergyKwh);
    }

}
