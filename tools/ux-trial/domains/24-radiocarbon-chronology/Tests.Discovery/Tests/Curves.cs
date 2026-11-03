namespace RadiocarbonChronology.Tests.Discovery;

/// <summary>Calibration curves the tests are about: their shape is what the expected dates are computed from.</summary>
internal static class Curves {

    /// <summary>A radiocarbon age equal to 1950 minus the calendar year, between -3000 and 1950: a measured age maps to one year.</summary>
    public static CalibrationCurve Linear() {
        return new CalibrationCurve("Linear", [new CalibrationPoint(-3000, 4950, 20), new CalibrationPoint(1950, 0, 20)]);
    }

    /// <summary>A curve that rises from 2000 BP to 2200 BP and falls back: an age of 2100 BP is reached twice, around 50 and 150.</summary>
    public static CalibrationCurve Wiggle() {
        return new CalibrationCurve("Wiggle", [new CalibrationPoint(0, 2000, 10), new CalibrationPoint(100, 2200, 10), new CalibrationPoint(200, 2000, 10)]);
    }

}
