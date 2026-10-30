using JustDummies;
using JustDummies.Xunit;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests;

[Reproducible]
public sealed class LoadCurveTests {

    private static IReadOnlyList<LoadCurvePoint> Curve(Func<TimeOnly, decimal> powerAt) {
        return Enumerable.Range(0, DailyLoadCurve.SlotsPerDay)
                         .Select(slot => TimeOnly.MinValue.AddMinutes(30 * slot))
                         .Select(start => new LoadCurvePoint(start, powerAt(start)))
                         .ToList();
    }

    [Fact]
    public void A_flat_2_kW_day_draws_48_kWh() {
        // Arrange
        DailyLoadCurve anyFlatCurve = Arbitrary.DailyLoadCurve().WithPoints(Curve(_ => 2m)).Generate();

        // Act
        decimal total = anyFlatCurve.TotalEnergyKwh;

        // Assert
        Assert.Equal(48m, total);
    }

    [Fact]
    public void The_peak_is_the_slot_drawing_the_most_power() {
        // Arrange
        TimeOnly    eveningSlot   = new TimeOnly(18, 30);
        decimal     spikeKw       = 9m;
        AnyDecimal  anyBackground = Any.Decimal().GreaterThanOrEqualTo(0m).LessThan(spikeKw).WithScale(3);
        DailyLoadCurve anyCurve = Arbitrary.DailyLoadCurve()
                                           .WithPoints(Curve(start => start == eveningSlot ? spikeKw : anyBackground.Generate()))
                                           .Generate();

        // Act
        LoadCurvePoint peak = anyCurve.Peak();

        // Assert
        Assert.Equal(eveningSlot, peak.SlotStart);
    }

    [Fact]
    public void Off_peak_energy_counts_the_slots_of_a_window_wrapping_midnight() {
        // Arrange
        OffPeakWindow nightHours = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));
        DailyLoadCurve anyCurveAtOneKw = Arbitrary.DailyLoadCurve().WithPoints(Curve(_ => 1m)).Generate();

        // Act
        decimal offPeak = anyCurveAtOneKw.EnergyWithin(nightHours);

        // Assert: 16 half-hour slots at 1 kW.
        Assert.Equal(8m, offPeak);
    }

    [Fact]
    public void A_curve_missing_a_slot_is_refused() {
        // Arrange
        DeliveryPointId anyDeliveryPoint = Arbitrary.DeliveryPointId().Generate();
        DateOnly        anyDay           = Any.DateOnly().Generate();
        List<LoadCurvePoint> anyIncompletePoints = Curve(_ => Any.Decimal().Between(0m, LoadCurvePoint.MaxPowerKw).Generate()).Skip(1).ToList();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new DailyLoadCurve(anyDeliveryPoint, anyDay, anyIncompletePoints));

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void A_wrapping_window_holds_both_sides_of_midnight_and_excludes_its_end() {
        // Arrange
        OffPeakWindow nightHours = new OffPeakWindow(new TimeOnly(22, 0), new TimeOnly(6, 0));

        // Act & Assert
        Assert.True(nightHours.Contains(new TimeOnly(23, 30)));
        Assert.True(nightHours.Contains(new TimeOnly(5, 30)));
        Assert.False(nightHours.Contains(new TimeOnly(6, 0)));
    }

    [Fact]
    public void An_empty_off_peak_window_is_refused() {
        // Arrange
        TimeOnly anyBoundary = Any.TimeOnly().WithGranularity(TimeSpan.FromMinutes(30)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new OffPeakWindow(anyBoundary, anyBoundary));
    }

}
