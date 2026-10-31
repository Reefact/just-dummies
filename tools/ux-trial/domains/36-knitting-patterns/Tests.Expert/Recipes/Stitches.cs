using JustDummies;

using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Recipes;

/// <summary>Recipes for the stitches a test needs to be there without caring which they are.</summary>
public static class Stitches {

    /// <summary>A stitch that takes one live stitch and leaves one: knit, purl or slip.</summary>
    public static readonly AnyEnum<StitchKind> Neutral =
        Any.Enum<StitchKind>().OneOf(StitchKind.Knit, StitchKind.Purl, StitchKind.Slip);

    /// <summary>
    ///     How a row that keeps <paramref name="width" /> stitches is written: k40, or k12, p16, k12, or
    ///     sl1, k38, p1... The width is the test's, the split and the stitches are arbitrary.
    /// </summary>
    public static IAny<IReadOnlyList<IRowElement>> NeutralRunsOver(int width) {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);

        // Cut points split the width into runs; a run holds at most StitchRun.MaxCount stitches,
        // so the multiples of that maximum are always cut as well.
        IAny<HashSet<int>> anyCuts = width == 1
            ? Any.SetOf(Any.Int32()).Empty()
            : Any.SetOf(Any.Int32().Between(1, width - 1)).WithMaxCount(Math.Min(width - 1, 5));
        int[] mandatoryCuts = Enumerable.Range(1, (width - 1) / StitchRun.MaxCount).Select(i => i * StitchRun.MaxCount).ToArray();

        return Any.Combine(
            anyCuts,
            Any.ListOf(Neutral).WithCount(width),
            (cuts, kinds) => {
                int[] bounds = cuts.Concat(mandatoryCuts).Append(0).Append(width).Distinct().Order().ToArray();
                return (IReadOnlyList<IRowElement>)bounds.Zip(bounds.Skip(1), (from, to) => (IRowElement)new StitchRun(kinds[from], to - from)).ToArray();
            });
    }

}
