namespace KnittingPatterns.Domain;

/// <summary>A row once worked: every single-stitch operation, and the stitch counts around it.</summary>
public sealed class ExpandedRow
{
    private readonly IReadOnlyList<StitchKind> _operations;

    public ExpandedRow(string sectionName, int rowNumber, FabricSide side, int stitchesBefore, IReadOnlyList<StitchKind> operations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        if (rowNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rowNumber), rowNumber, "Rows are numbered from 1.");
        }

        if (!Enum.IsDefined(side))
        {
            throw new ArgumentOutOfRangeException(nameof(side), side, "Unknown fabric side.");
        }

        if (stitchesBefore < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(stitchesBefore), stitchesBefore, "A row starts with at least one stitch.");
        }

        ArgumentNullException.ThrowIfNull(operations);
        if (operations.Count == 0)
        {
            throw new ArgumentException("An expanded row holds at least one operation.", nameof(operations));
        }

        int consumed = operations.Sum(o => o.Consumes());
        if (consumed != stitchesBefore)
        {
            throw new ArgumentException(
                $"Row {rowNumber} consumes {consumed} stitches but starts with {stitchesBefore}.", nameof(operations));
        }

        SectionName = sectionName;
        RowNumber = rowNumber;
        Side = side;
        StitchesBefore = stitchesBefore;
        _operations = operations.ToArray();
        StitchesAfter = _operations.Sum(o => o.Produces());
    }

    public string SectionName { get; }

    public int RowNumber { get; }

    public FabricSide Side { get; }

    public int StitchesBefore { get; }

    public int StitchesAfter { get; }

    public IReadOnlyList<StitchKind> Operations => _operations;

    public int Increases => _operations.Count(o => o.Produces() > o.Consumes());

    public int Decreases => _operations.Count(o => o.Consumes() > o.Produces());
}
