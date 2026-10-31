// Written by hand beside the scaffolded AnyPatternSection.cs, so `any generate --force` keeps it.

using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace KnittingPatterns.Domain;

public sealed partial class AnyPatternSection {

    /// <summary>Draws a section whose rows start at <paramref name="firstRow" />, the first worked on the right side.</summary>
    public AnyPatternSection StartingAtRow(int firstRow) {
        return new AnyPatternSection(_name, AnyConsecutiveRowsFrom(new FixedValue<int>(firstRow), new FixedValue<FabricSide>(FabricSide.RightSide)));
    }

    /// <summary>A short run of consecutive rows, alternating sides, the first one numbered from <paramref name="firstRow" />.</summary>
    internal static IAny<IReadOnlyList<RowInstruction>> AnyConsecutiveRowsFrom(IAny<int> firstRow, IAny<FabricSide> firstSide) {
        return Any.Combine(firstRow,
                           Any.Int32().Between(1, 6),
                           firstSide,
                           new AnyRowInstruction(),
                           (first, count, side, template) => (IReadOnlyList<RowInstruction>)Enumerable.Range(0, count)
                               .Select(i => new RowInstruction(first + i,
                                                               i % 2 == 0 ? side : Opposite(side),
                                                               template.Elements,
                                                               template.ExpectedStitchCount))
                               .ToArray());
    }

    private static FabricSide Opposite(FabricSide side) {
        return side == FabricSide.RightSide ? FabricSide.WrongSide : FabricSide.RightSide;
    }

}
