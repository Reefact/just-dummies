using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular;

/// <summary>Hand-made calibration curves whose shape the tests rely on: they are significant, never drawn.</summary>
internal static class Curves {

    /// <summary>An ideal atmosphere: an age of N BP reads as calendar year 1950 - N, over [-3000, 1950].</summary>
    public static CalibrationCurve Linear() {
        return new CalibrationCurve("ideal", [
            new CalibrationPoint(-3000, 4950, 10),
            new CalibrationPoint(1950, 0, 10)
        ]);
    }

    /// <summary>A wiggle around year 100: an age of 1950 BP is reached twice, near years 50 and 150.</summary>
    public static CalibrationCurve Wiggle() {
        return new CalibrationCurve("wiggle", [
            new CalibrationPoint(-500, 2600, 10),
            new CalibrationPoint(0, 2000, 10),
            new CalibrationPoint(100, 1900, 10),
            new CalibrationPoint(200, 2000, 10)
        ]);
    }

}
