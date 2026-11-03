using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class StitchKindTests {

    [Theory]
    [InlineData(StitchKind.Knit, 1, 1)]
    [InlineData(StitchKind.Purl, 1, 1)]
    [InlineData(StitchKind.Slip, 1, 1)]
    [InlineData(StitchKind.YarnOver, 0, 1)]
    [InlineData(StitchKind.KnitFrontAndBack, 1, 2)]
    [InlineData(StitchKind.KnitTwoTogether, 2, 1)]
    [InlineData(StitchKind.SlipSlipKnit, 2, 1)]
    [InlineData(StitchKind.CentralDoubleDecrease, 3, 1)]
    public void Each_stitch_consumes_and_produces_the_stitches_the_pattern_books_give_it(StitchKind kind, int consumes, int produces) {
        Assert.Equal(consumes, kind.Consumes());
        Assert.Equal(produces, kind.Produces());
    }

}
