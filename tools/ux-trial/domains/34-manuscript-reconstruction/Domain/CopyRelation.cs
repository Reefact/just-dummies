namespace ManuscriptReconstruction.Domain;

/// <summary>The claim that one witness was copied from another, with the editor's confidence in it.</summary>
public sealed class CopyRelation
{
    public CopyRelation(WitnessSiglum exemplar, WitnessSiglum copy, double confidence, int sharedErrorCount)
    {
        ArgumentNullException.ThrowIfNull(exemplar);
        ArgumentNullException.ThrowIfNull(copy);
        if (exemplar.Equals(copy))
        {
            throw new ArgumentException($"Witness {copy} cannot be a copy of itself.", nameof(copy));
        }

        if (double.IsNaN(confidence) || confidence <= 0.0 || confidence > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), confidence, "Confidence lies in (0, 1].");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(sharedErrorCount);
        Exemplar = exemplar;
        Copy = copy;
        Confidence = confidence;
        SharedErrorCount = sharedErrorCount;
    }

    public WitnessSiglum Exemplar { get; }

    public WitnessSiglum Copy { get; }

    public double Confidence { get; }

    /// <summary>The number of conjunctive errors the editor cites in support of the relation.</summary>
    public int SharedErrorCount { get; }
}
