using Insurance.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Insurance.Tests;

// Deliberate misuses, compiled to see which JDxxx analyzers fire. Nothing here runs as a test except
// the one in the [Reproducible] class, which exists only for its constructor.
internal static class AnalyzerProbes {

    // JD009 expected: a static initializer draws once for the whole suite.
    internal static readonly Driver SharedDriver = Any.Driver().Generate();

    internal static void ConstantContradictions() {
        int    years       = Any.Int32().Between(80, 3).Generate();                                   // JD023 expected
        double coefficient = Any.Double().Between(BonusMalus.Ceiling, BonusMalus.Floor).Generate();   // floating point: any rule?
        decimal deductible = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();           // scale lattice empty: any rule?
        DateOnly day       = Any.DateOnly().Between(new DateOnly(2027, 1, 1), new DateOnly(2026, 1, 1)).Generate(); // dates: any rule?
        HashSet<Responsibility> all = Any.SetOf(Any.Enum<Responsibility>()).WithCount(4).Generate(); // JD016 expected
        List<Claim> distinct = Any.ListOf(Any.Claim()).WithCount(3).Distinct().Generate();          // JD028 expected
        string number = Any.String().StartingWith("POL-").WithLength(3).Generate();                   // JD015 expected
        _ = (years, coefficient, deductible, day, all, distinct, number);
    }

    internal static void DrawnValueInTheAssertion() {
        // The fundamentals say this is a property, not a dummy: does anything flag it?
        string anyName   = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        Driver anyDriver = Any.Driver().WithFullName($"  {anyName} ").Generate();

        Assert.Equal(anyName, anyDriver.FullName);
    }

}

[Reproducible]
public sealed class AnalyzerProbeConstructorDraw {

    private readonly Claim _claim;

    public AnalyzerProbeConstructorDraw() {
        _claim = Any.Claim().Generate(); // JD007 expected
    }

    [Fact]
    public void Uses_the_claim_drawn_in_the_constructor() {
        Assert.NotNull(_claim);
    }

}
