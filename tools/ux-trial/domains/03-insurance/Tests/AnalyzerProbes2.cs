using Insurance.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Insurance.Tests;

[Reproducible]
public sealed class AnalyzerProbeLibraryDraws {

    private static readonly int SharedYears = Any.Int32().Between(0, 80).Generate();          // JD009 with a library generator?
    private static readonly Driver SharedDriver = new AnyDriver().Generate();                 // JD009 with new AnyX()?

    private readonly int _years;

    public AnalyzerProbeLibraryDraws() {
        _years = Any.Int32().Between(0, 80).Generate();                                       // JD007 with a library generator?
    }

    [Fact]
    public void Uses_the_values_drawn_outside_the_scope() {
        List<Claim> distinct = Any.ListOf(new AnyClaim()).WithCount(3).Distinct().Generate(); // JD028 with new AnyX()?
        Assert.True(_years >= 0 && SharedYears >= 0 && SharedDriver is not null && distinct.Count == 3);
    }

}
