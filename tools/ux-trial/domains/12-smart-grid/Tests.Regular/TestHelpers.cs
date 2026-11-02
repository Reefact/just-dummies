using SmartGrid.Domain;

namespace SmartGrid.Tests.Regular;

internal static class TestHelpers {

    public static MeterReading Reading(DeliveryPointId deliveryPoint, decimal kwh, DateOnly date, ReadingSource source = ReadingSource.Actual) {
        return new MeterReading(deliveryPoint, new MeterIndex(kwh), date, source);
    }

    /// <summary>A day of 48 points whose power in slot i is <paramref name="powerOfSlot" />(i).</summary>
    public static List<LoadCurvePoint> Points(Func<int, decimal> powerOfSlot) {
        List<LoadCurvePoint> points = new List<LoadCurvePoint>();
        for (int slot = 0; slot < DailyLoadCurve.SlotsPerDay; slot++) {
            points.Add(new LoadCurvePoint(SlotStart(slot), powerOfSlot(slot)));
        }

        return points;
    }

    public static TimeOnly SlotStart(int slot) {
        return TimeOnly.MinValue.AddMinutes(30 * slot);
    }

    public static DailyLoadCurve Curve(Func<int, decimal> powerOfSlot) {
        return new AnyDailyLoadCurve().WithPoints(Points(powerOfSlot)).Generate();
    }

}
