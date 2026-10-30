using JustDummies;
using JustDummies.Xunit;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

/// <summary>
/// Compile-time probes: never run as tests. Each method holds one mistake an analyzer could catch, with the
/// rule expected in a comment; the observed outcome is in NOTES.md.
/// </summary>
public static class AnalyzerProbes {

    // A drawn value flowing into an assertion: the documentation calls this "not a dummy" (no rule is listed).
    public static void DrawnValueAsserted() {
        double anyPotassium = Any.Double().Between(3.5, 5.1).Generate();
        ReferenceRange range = new(3.5, 5.1, new MeasurementUnit("mmol/L"));

        Assert.Equal(ResultFlag.Normal, range.Classify(anyPotassium));
    }

    // Crossed constant bounds on a double: JD023 "ScalarChainAdmitsNoValue"?
    public static double CrossedDoubleBounds() => Any.Double().GreaterThan(5.1).LessThan(3.5).Generate();

    // Crossed constant bounds on an int, for comparison: JD023.
    public static int CrossedIntBounds() => Any.Int32().GreaterThan(100).LessThan(10).Generate();

    // Crossed constant instants: JD023?
    public static DateTimeOffset CrossedInstants() => Any.DateTimeOffset()
                                                         .After(new DateTimeOffset(2025, 3, 2, 0, 0, 0, TimeSpan.Zero))
                                                         .Before(new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero))
                                                         .Generate();

    // A decimal interval with no value at the declared scale: JD023?
    public static decimal NoValueAtScale() => Any.Decimal().Between(0.001m, 0.004m).WithScale(2).Generate();

    // A NaN bound: JD014 "RejectedConstantArgument"?
    public static double NaNBound() => Any.Double().LessThan(double.NaN).Generate();

    // A transposed double Between: JD014 ("checked on every numeric overload").
    public static double TransposedDoubleRange() => Any.Double().Between(5.1, 3.5).Generate();

    // NaN in a double pool: JD014 (listed on its page).
    public static double NaNInPool() => Any.Double().OneOf(1.0, double.NaN).Generate();

    // A transposed DateTimeOffset Between: JD014?
    public static DateTimeOffset TransposedInstants() => Any.DateTimeOffset()
                                                            .Between(new DateTimeOffset(2025, 3, 2, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero))
                                                            .Generate();

    // A crossed TimeOnly window: JD014 or JD023?
    public static TimeOnly OvernightWindow() => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0)).Generate();

    // A discarded constraint: JD006.
    public static double DiscardedConstraint() {
        AnyDouble anyConcentration = Any.Double().GreaterThanOrEqualTo(0d);
        anyConcentration.LessThan(1_000d);

        return anyConcentration.Generate();
    }

    // An unsupported regex construct in a constant pattern: an analyzer?
    public static string LookaheadPattern() => Any.StringMatching(@"S(?=\d)\d{10}").Generate();

    // Reproducible on a non-test method: JD010.
    [Reproducible]
    public static void NotATest() { }

}
