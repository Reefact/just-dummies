using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

/// <summary>
///     Calibration curves the tests are ABOUT, not dummies: their shape decides where a measured age lands, so each
///     one is written out. A test that does not care which curve it has draws one with <c>Any.CalibrationCurve()</c>.
/// </summary>
internal static class Curves {

    /// <summary>
    ///     A curve on which a measured age of N years BP calibrates onto calendar year 1950 - N, whatever N, with a
    ///     curve uncertainty of 10 years. It covers calendar years -5000 to 1950, that is ages 0 to 6950 BP.
    /// </summary>
    public static CalibrationCurve Identity() {
        List<CalibrationPoint> points = new();
        for (int year = -5000; year <= 1950; year += 50) {
            points.Add(new CalibrationPoint(year, 1950 - year, 10));
        }

        return new CalibrationCurve("Identity", points);
    }

    /// <summary>
    ///     A curve with a reversal: radiocarbon age falls to 2800 BP in year -800 then rises again, so an age of
    ///     2900 BP is met twice, around year -900 and around year -700. Covers calendar years -1500 to -100.
    /// </summary>
    public static CalibrationCurve WithAReversalAtYearMinus800() {
        return new CalibrationCurve("Reversal", [
            new CalibrationPoint(-1500, 3500, 10),
            new CalibrationPoint(-800, 2800, 10),
            new CalibrationPoint(-100, 3500, 10),
        ]);
    }

}
