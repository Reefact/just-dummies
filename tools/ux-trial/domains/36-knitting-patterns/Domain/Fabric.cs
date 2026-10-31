namespace KnittingPatterns.Domain;

/// <summary>The structural state of the fabric on the needles while a pattern is worked.</summary>
public sealed class Fabric
{
    public Fabric(int castOnCount)
    {
        if (castOnCount < 1 || castOnCount > KnittingPattern.MaxCastOn)
        {
            throw new ArgumentOutOfRangeException(nameof(castOnCount), castOnCount, $"A cast-on counts between 1 and {KnittingPattern.MaxCastOn} stitches.");
        }

        LiveStitches = castOnCount;
    }

    /// <summary>Stitches currently on the needle.</summary>
    public int LiveStitches { get; private set; }

    /// <summary>Last row worked, 0 right after the cast-on.</summary>
    public int RowsWorked { get; private set; }

    /// <summary>Side the next row is worked on: right side right after the cast-on, then alternating.</summary>
    public FabricSide NextSide => RowsWorked % 2 == 0 ? FabricSide.RightSide : FabricSide.WrongSide;

    /// <summary>Works one row and moves the fabric to the stitch count it leaves.</summary>
    /// <exception cref="InvalidOperationException">The row is not the next one, or is worked on the wrong side.</exception>
    /// <exception cref="StitchCountMismatchException">The row consumes a different number of stitches than are live.</exception>
    public ExpandedRow Work(string sectionName, RowInstruction row)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        ArgumentNullException.ThrowIfNull(row);
        if (row.RowNumber != RowsWorked + 1)
        {
            throw new InvalidOperationException($"Row {row.RowNumber} cannot be worked: row {RowsWorked + 1} is next.");
        }

        if (row.Side != NextSide)
        {
            throw new InvalidOperationException($"Row {row.RowNumber} is written {row.Side} but the fabric presents its {NextSide}.");
        }

        if (row.Consumes != LiveStitches)
        {
            throw new StitchCountMismatchException(row.RowNumber, LiveStitches, row.Consumes);
        }

        ExpandedRow expanded = new(sectionName, row.RowNumber, row.Side, LiveStitches, row.Expand().ToArray());
        LiveStitches = expanded.StitchesAfter;
        RowsWorked = row.RowNumber;
        return expanded;
    }
}
