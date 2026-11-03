using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular;

/// <summary>Hand-built calibration curves whose shape the tests can reason about.</summary>
internal static class TestCurves {

    /// <summary>
    ///     A curve where the radiocarbon age is exactly 1950 minus the calendar year: a measurement of N BP peaks in
    ///     the year 1950 - N, with a curve uncertainty of 10 years.
    /// </summary>
    public static CalibrationCurve Linear() {
        List<CalibrationPoint> points = new List<CalibrationPoint>();
        for (int year = -10_000; year <= 1950; year += 10) {
            points.Add(new CalibrationPoint(year, 1950 - year, 10d));
        }

        return new CalibrationCurve("Linear test curve", points);
    }

}
