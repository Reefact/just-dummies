using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Regular.Tests;

/// <summary>Spells a written row the way a pattern does, so the stitch counts in a test stay readable.</summary>
internal static class Rows {

    public static StitchRun K(int count)  => new(StitchKind.Knit, count);
    public static StitchRun P(int count)  => new(StitchKind.Purl, count);
    public static StitchRun Yo(int count) => new(StitchKind.YarnOver, count);
    public static StitchRun Kfb(int count) => new(StitchKind.KnitFrontAndBack, count);
    public static StitchRun K2Tog(int count) => new(StitchKind.KnitTwoTogether, count);
    public static StitchRun Ssk(int count) => new(StitchKind.SlipSlipKnit, count);
    public static StitchRun Cdd(int count) => new(StitchKind.CentralDoubleDecrease, count);

    public static StitchRepeat Rep(int times, params StitchRun[] runs) => new(runs, times);

    public static RowInstruction Row(int rowNumber, params IRowElement[] elements) {
        FabricSide side = rowNumber % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;

        return new RowInstruction(rowNumber, side, elements, expectedStitchCount: null);
    }

}
