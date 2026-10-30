using JustDummies;

using Trading.Domain;

namespace Trading.Tests;

/// <summary>Never called: chains written outside any Assert.Throws, to see which rules fire.</summary>
internal static class AnalyzerTargets {

    public static void Contradictions() {
        int     crossed     = Any.Int32().GreaterThan(100).LessThan(10).Generate();
        int     noMultiple  = Any.Int32().Between(1, 10).MultipleOf(50).Generate();
        decimal crossedDec  = Any.Decimal().Between(10m, 20m).GreaterThan(30m).Generate();
        decimal noScalePt   = Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate();
        List<Side> tooMany  = Any.ListOf(Any.Enum<Side>()).WithCount(3).Distinct().Generate();
        List<Ticker> inert  = Any.ListOf(Any.Int32().Between(1, 2).As(shares => new Quantity(shares))).Distinct().WithCount(6).Generate().Select(_ => new Ticker("A")).ToList();
        string  ticker      = Any.String().NotBlank().Generate();
        _ = (crossed, noMultiple, crossedDec, noScalePt, tooMany, inert, ticker);
    }

}
