namespace KnittingPatterns.Domain;

/// <summary>A row's instructions do not match the stitches the fabric holds.</summary>
public sealed class StitchCountMismatchException : InvalidOperationException
{
    public StitchCountMismatchException(int rowNumber, int liveStitches, int requiredStitches)
        : base($"Row {rowNumber} needs {requiredStitches} stitches but the needle holds {liveStitches}.")
    {
        RowNumber = rowNumber;
        LiveStitches = liveStitches;
        RequiredStitches = requiredStitches;
    }

    public int RowNumber { get; }

    public int LiveStitches { get; }

    public int RequiredStitches { get; }
}
