using Drafting.Domain;

using JustDummies;

using Xunit;

namespace Drafting.Tests;

/// <summary>Things an analyzer should catch, written outside a lambda so no exemption applies. Never run.</summary>
public static class AnalyzerProbes {

    public static void ContradictoryConstants() {
        double  crossedDouble  = Any.Double().GreaterThan(10).LessThan(5).Generate();
        int     crossedInt     = Any.Int32().GreaterThan(10).LessThan(5).Generate();
        DateOnly afterTheEnd   = Any.DateOnly().After(DateOnly.MaxValue).Generate();
        HashSet<FitKind> four  = Any.SetOf(Any.Enum<FitKind>()).WithCount(4).Generate();
        string  narrowedPool   = Any.String().OneOf("H", "h").OneOf("H").Generate();
        int     disjointPool   = Any.Int32().Between(1, 9_999).OneOf(10_000).Generate();
        string  emptyPattern   = Any.StringMatching(@"^P\d{6}$").OneOf("X").Generate();
    }

    public static void GeneratedValueInAssertion() {
        double anyNominal = Any.Double().Between(1, 400).Generate();
        Dimension hole    = new(anyNominal, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        Assert.Equal(anyNominal, hole.LowerLimitInMillimetres);
    }

    public static void UnconstrainedString() {
        string anyTitle = Any.String().Generate();
        Dimension[] none = Any.ArrayOf(new AnyDimension()).WithCount(0).NonEmpty().Generate();
        HashSet<DrawingScale> scales = Any.SetOf(new AnyDrawingScale()).WithCount(3).Generate();
        List<DrawingScale> distinctScales = Any.ListOf(new AnyDrawingScale()).WithCount(3).Distinct().Generate();
    }

}
