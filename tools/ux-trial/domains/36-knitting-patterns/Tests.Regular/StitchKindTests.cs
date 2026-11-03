using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

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
    public void Each_stitch_takes_and_gives_the_stitches_its_symbol_stands_for(StitchKind kind, int consumes, int produces) {
        Assert.Equal(consumes, kind.Consumes());
        Assert.Equal(produces, kind.Produces());
    }

    [Fact]
    public void A_stitch_that_adds_stitches_produces_more_than_it_consumes() {
        // Arrange
        StitchKind anyIncrease = Any.Enum<StitchKind>().OneOf(StitchKind.YarnOver, StitchKind.KnitFrontAndBack).Generate();

        // Act / Assert
        Assert.True(anyIncrease.Produces() > anyIncrease.Consumes());
    }

    [Fact]
    public void A_stitch_that_removes_stitches_consumes_more_than_it_produces() {
        // Arrange
        StitchKind anyDecrease = Any.Enum<StitchKind>().OneOf(StitchKind.KnitTwoTogether, StitchKind.SlipSlipKnit, StitchKind.CentralDoubleDecrease).Generate();

        // Act / Assert
        Assert.True(anyDecrease.Consumes() > anyDecrease.Produces());
    }

}
