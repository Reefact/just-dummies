using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Discovery;

/// <summary>Curves whose shape the tests reason about, so they are written out rather than drawn.</summary>
internal static class Curves {

    /// <summary>One radiocarbon year per calendar year: an age of N BP reads as the calendar year 1950 - N.</summary>
    public static IReadOnlyList<CalibrationPoint> Linear() {
        return [
            new CalibrationPoint(-3050, 5000, 10),
            new CalibrationPoint(1950, 0, 10),
        ];
    }

}
