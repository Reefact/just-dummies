namespace SmartGrid.Domain;

/// <summary>Builders for the values a test pins on purpose; everything else is drawn through the generators.</summary>
internal static class TestData {

    public static MeterReading Reading(DeliveryPointId point, decimal kwh, DateOnly date, ReadingSource source = ReadingSource.Actual) {
        return new MeterReading(point, new MeterIndex(kwh), date, source);
    }

    public static List<LoadCurvePoint> Slots(Func<int, decimal> powerOfSlot) {
        List<LoadCurvePoint> points = new List<LoadCurvePoint>();
        for (int slot = 0; slot < DailyLoadCurve.SlotsPerDay; slot++) {
            points.Add(new LoadCurvePoint(TimeOnly.MinValue.AddMinutes(30 * slot), powerOfSlot(slot)));
        }

        return points;
    }

}
