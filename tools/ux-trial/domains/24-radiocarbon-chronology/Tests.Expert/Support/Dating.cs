using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert.Support;

/// <summary>Reads the shape of a calibrated date the way a reviewer would, from its public probabilities.</summary>
internal static class Dating {

    public static int ModeYear(CalibratedDate date) {
        int modeYear = date.FirstYear;
        for (int year = date.FirstYear; year <= date.LastYear; year++) {
            if (date.ProbabilityOf(year) > date.ProbabilityOf(modeYear)) { modeYear = year; }
        }

        return modeYear;
    }

    public static double StandardDeviation(CalibratedDate date) {
        double mean     = date.Mean;
        double variance = 0;
        for (int year = date.FirstYear; year <= date.LastYear; year++) {
            variance += date.ProbabilityOf(year) * (year - mean) * (year - mean);
        }

        return Math.Sqrt(variance);
    }

    public static double Total(CalibratedDate date) {
        return date.Probabilities.Sum();
    }

    /// <summary>The probability held by the years of <paramref name="interval" />.</summary>
    public static double MassWithin(CalibratedDate date, CalendarInterval interval) {
        double mass = 0;
        for (int year = interval.StartYear; year <= interval.EndYear; year++) {
            mass += date.ProbabilityOf(year);
        }

        return mass;
    }

}
