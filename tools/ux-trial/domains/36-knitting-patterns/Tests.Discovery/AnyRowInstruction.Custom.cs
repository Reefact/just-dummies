using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

// Written by hand, beside the generated file: `any generate RowInstruction --force` leaves it alone.
public sealed partial class AnyRowInstruction {

    /// <summary>
    ///     Elements that consume and leave exactly <paramref name="stitches" /> stitches: runs of knit, purl or slip,
    ///     so the row can be worked on a fabric holding that many without changing the count.
    /// </summary>
    public AnyRowInstruction WithStitchNeutralElements(int stitches) {
        List<IRowElement> elements  = new List<IRowElement>();
        int               remaining = stitches;
        while (remaining > 0) {
            int        take = remaining > StitchRun.MaxCount ? Any.Int32().Between(1, StitchRun.MaxCount).Generate() : Any.Int32().Between(1, remaining).Generate();
            StitchKind kind = Any.OneOf(StitchKind.Knit, StitchKind.Purl, StitchKind.Slip).Generate();

            elements.Add(new StitchRun(kind, take));
            remaining -= take;
        }

        return WithElements(elements);
    }

}
