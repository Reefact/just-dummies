using Clinical.Domain;

using JustDummies;

namespace Clinical.Tests;

/// <summary>The same chains as AnalyzerProbes, written as ordinary arrange lines (never run) so the analyzers do not stand down.</summary>
internal static class AnalyzerProbeChains {

    internal static void ArrangeLines() {
        int                         anyIntakes   = Any.Int32().Between(1, 6).GreaterThan(10).Generate();
        DateOnly                    anyBirthDate = Any.DateOnly().After(new DateOnly(2026, 1, 1)).Before(new DateOnly(2025, 12, 31)).Generate();
        decimal                     anyDose      = Any.Decimal().Between(0.0001m, 0.0009m).WithScale(3).Generate();
        HashSet<RouteOfAdministration> anyRoutes = Any.SetOf(Any.Enum<RouteOfAdministration>()).WithCount(9).Generate();
        List<string>                anyDigits    = Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(11).Distinct().Generate();
        string                      anyNirBody   = Any.StringMatching(@"[12][0-9]{4}(?!00)[0-9]{8}").Generate();
        TimeOnly                    anyNightTime = Any.TimeOnly().Between(new TimeOnly(20, 0), new TimeOnly(8, 0)).Generate();
        decimal                     anyRate      = Any.Decimal().Positive().WithScale(29).Generate();
        _ = (anyIntakes, anyBirthDate, anyDose, anyRoutes, anyDigits, anyNirBody, anyNightTime, anyRate);
    }

}
