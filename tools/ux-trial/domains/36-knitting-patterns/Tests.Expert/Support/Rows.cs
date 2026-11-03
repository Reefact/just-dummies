using JustDummies;
using KnittingPatterns.Domain;
using KnittingPatterns.Tests.Expert.Generators;

namespace KnittingPatterns.Tests.Expert.Support;

/// <summary>Row-building helpers shared by the generators and the tests.</summary>
public static class Rows {

    /// <summary>Flat rows start on the right side: odd rows are right side, even rows wrong side.</summary>
    public static FabricSide SideOf(int rowNumber) {
        return rowNumber % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;
    }

    /// <summary>Runs of one kind covering <paramref name="count" /> stitches, split where a single run tops out.</summary>
    public static List<IRowElement> Chunks(StitchKind kind, int count) {
        List<IRowElement> runs      = new List<IRowElement>();
        int               remaining = count;
        while (remaining > 0) {
            int chunk = Math.Min(remaining, StitchRun.MaxCount);
            runs.Add(new StitchRun(kind, chunk));
            remaining -= chunk;
        }

        return runs;
    }

    /// <summary>A row that knits (or purls, or slips) its way across exactly <paramref name="stitches" /> stitches, whatever they are.</summary>
    public static RowInstruction PlainRow(int rowNumber, int stitches) {
        return new RowInstruction(rowNumber, SideOf(rowNumber), Chunks(StitchKind.Knit, stitches), expectedStitchCount: stitches);
    }

    /// <summary>
    ///     An arbitrary row, with decreases, increases, yarn overs and sometimes a rib repeat, that consumes
    ///     exactly <paramref name="live" /> stitches. Everything about it replays from the ambient seed.
    /// </summary>
    public static RowInstruction ArbitraryRowConsuming(int rowNumber, int live) {
        int centralDecreases = Any.Int32().Between(0, live / 3).Generate();
        int pairDecreases    = Any.Int32().Between(0, (live - 3 * centralDecreases) / 2).Generate();
        int left             = live - 3 * centralDecreases - 2 * pairDecreases;
        int frontAndBacks    = Any.Int32().Between(0, Math.Min(left, 3)).Generate();
        int plain            = left - frontAndBacks;
        int yarnOvers        = Any.Int32().Between(0, 2).Generate();

        List<IRowElement> parts = new List<IRowElement>();
        parts.AddRange(Chunks(StitchKind.CentralDoubleDecrease, centralDecreases));
        parts.AddRange(Chunks(Any.OneOf(StitchKind.KnitTwoTogether, StitchKind.SlipSlipKnit).Generate(), pairDecreases));
        parts.AddRange(Chunks(StitchKind.KnitFrontAndBack, frontAndBacks));
        parts.AddRange(Chunks(StitchKind.YarnOver, yarnOvers));
        if (plain >= 4 && Any.Boolean().Generate()) {
            parts.Add(new StitchRepeat([new StitchRun(StitchKind.Knit, 1), new StitchRun(StitchKind.Purl, 1)], plain / 2));
            parts.AddRange(Chunks(StitchKind.Knit, plain % 2));
        } else {
            parts.AddRange(Chunks(Any.OneOf(StitchKind.Knit, StitchKind.Purl, StitchKind.Slip).Generate(), plain));
        }

        return Any.RowInstruction()
                  .WithRowNumber(rowNumber)
                  .WithSide(SideOf(rowNumber))
                  .WithElements(Any.PermutationOf(parts).Generate())
                  .Generate();
    }

}
