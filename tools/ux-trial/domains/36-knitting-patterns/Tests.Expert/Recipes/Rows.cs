using JustDummies;

using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Recipes;

/// <summary>Recipes for written rows.</summary>
public static class Rows {

    /// <summary>The side a flat piece presents on a row: the right side on odd rows, cast-on facing.</summary>
    public static FabricSide SideOf(int rowNumber) {
        return rowNumber % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;
    }

    /// <summary>
    ///     A row that neither increases nor decreases over <paramref name="width" /> stitches, written
    ///     any way at all, with or without its count printed at the end.
    /// </summary>
    public static IAny<RowInstruction> Plain(int rowNumber, int width) {
        return Any.Combine(
            Stitches.NeutralRunsOver(width),
            Any.Boolean(),
            (elements, printsCount) => new RowInstruction(rowNumber, SideOf(rowNumber), elements, printsCount ? width : null));
    }

    /// <summary>A row written from the given elements, worked on the side its number calls for.</summary>
    public static RowInstruction Written(int rowNumber, params IRowElement[] elements) {
        return new RowInstruction(rowNumber, SideOf(rowNumber), elements, expectedStitchCount: null);
    }

    public static StitchRun K(int count) => new(StitchKind.Knit, count);

    public static StitchRun P(int count) => new(StitchKind.Purl, count);

    public static StitchRun Kfb(int count = 1) => new(StitchKind.KnitFrontAndBack, count);

    public static StitchRun K2Tog(int count = 1) => new(StitchKind.KnitTwoTogether, count);

    public static StitchRun Ssk(int count = 1) => new(StitchKind.SlipSlipKnit, count);

    public static StitchRun Yo(int count = 1) => new(StitchKind.YarnOver, count);

    public static StitchRun Cdd(int count = 1) => new(StitchKind.CentralDoubleDecrease, count);

    public static StitchRepeat Repeat(int times, params StitchRun[] runs) => new(runs, times);

}
