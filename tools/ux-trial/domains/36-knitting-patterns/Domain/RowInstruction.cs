namespace KnittingPatterns.Domain;

/// <summary>Which face of the fabric faces the knitter while working a row.</summary>
public enum FabricSide
{
    RightSide,
    WrongSide,
}

/// <summary>One written row: "Row 5 (RS): k2, *yo, k2tog; rep from * 8 times, k2 (20 sts)".</summary>
public sealed class RowInstruction
{
    private readonly IReadOnlyList<IRowElement> _elements;

    public RowInstruction(int rowNumber, FabricSide side, IReadOnlyList<IRowElement> elements, int? expectedStitchCount)
    {
        if (rowNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rowNumber), rowNumber, "Rows are numbered from 1.");
        }

        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown fabric side.");
        }

        ArgumentNullException.ThrowIfNull(elements);
        if (elements.Count == 0)
        {
            throw new ArgumentException("A row holds at least one element.", nameof(elements));
        }

        if (elements.Any(e => e is null))
        {
            throw new ArgumentException("A row cannot hold a null element.", nameof(elements));
        }

        int produces = elements.Sum(e => e.Produces);
        if (expectedStitchCount is { } expected)
        {
            if (expected < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedStitchCount), expected, "A row ends with at least one stitch.");
            }

            if (expected != produces)
            {
                throw new ArgumentException(
                    $"Row {rowNumber} states {expected} stitches but its instructions produce {produces}.",
                    nameof(expectedStitchCount));
            }
        }

        RowNumber = rowNumber;
        Side = side;
        _elements = elements.ToArray();
        ExpectedStitchCount = expectedStitchCount;
    }

    public int RowNumber { get; }

    public FabricSide Side { get; }

    public IReadOnlyList<IRowElement> Elements => _elements;

    /// <summary>The count printed at the end of the row, when the pattern gives one.</summary>
    public int? ExpectedStitchCount { get; }

    /// <summary>Live stitches the row must start with.</summary>
    public int Consumes => _elements.Sum(e => e.Consumes);

    /// <summary>Live stitches the row ends with.</summary>
    public int Produces => _elements.Sum(e => e.Produces);

    public IEnumerable<StitchKind> Expand() => _elements.SelectMany(e => e.Expand());
}
