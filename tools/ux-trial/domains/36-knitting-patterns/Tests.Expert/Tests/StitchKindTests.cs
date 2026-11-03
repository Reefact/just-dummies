namespace KnittingPatterns.Tests.Expert.Tests;

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
    public void Each_stitch_takes_from_the_left_needle_and_puts_on_the_right_needle_what_its_symbol_says(StitchKind kind, int consumed, int produced) {
        Assert.Equal(consumed, kind.Consumes());
        Assert.Equal(produced, kind.Produces());
    }

}
