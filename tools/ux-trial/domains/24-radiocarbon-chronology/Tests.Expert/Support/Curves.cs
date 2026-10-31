using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

/// <summary>
/// Calibration curves whose shape the tests rely on, so their points are literals. Only their names are
/// arbitrary: no calibration reads the name of its curve.
/// </summary>
public static class Curves {

    /// <summary>
    /// A curve without wiggles from 5000 BCE to 1950: a radiocarbon age of N BP reads as calendar year 1950 - N.
    /// </summary>
    public static CalibrationCurve Straight() {
        List<CalibrationPoint> points = new();
        for (int year = -5_000; year <= CalendarInterval.LatestYear; year += 50) {
            points.Add(new CalibrationPoint(year, CalendarInterval.LatestYear - year, 10));
        }

        return Any.CalibrationCurve().WithPoints(points).Generate();
    }

    /// <summary>
    /// A curve whose atmosphere reverses between 900 and 700 BCE (astronomical -900 and -700): 2800 BP is
    /// recorded in two separate centuries, the textbook origin of a multimodal date.
    /// </summary>
    public static CalibrationCurve ReversedAroundTheEighthCenturyBce() {
        List<CalibrationPoint> points = [
            new CalibrationPoint(-1_100, 3_100, 10),
            new CalibrationPoint(-900,   2_800, 10),
            new CalibrationPoint(-800,   2_500, 10),
            new CalibrationPoint(-700,   2_800, 10),
            new CalibrationPoint(-500,   3_100, 10),
        ];

        return Any.CalibrationCurve().WithPoints(points).Generate();
    }

}
