namespace ManuscriptReconstruction.Domain;

/// <summary>A surviving manuscript that attests the text, dated to a span of years (negative years are BCE).</summary>
public sealed class Witness
{
    public const int EarliestYear = -800;
    public const int LatestYear = 1600;

    public Witness(WitnessSiglum siglum, string repository, int datedFrom, int datedTo, int folioCount)
    {
        ArgumentNullException.ThrowIfNull(siglum);
        ArgumentException.ThrowIfNullOrWhiteSpace(repository);
        ArgumentOutOfRangeException.ThrowIfLessThan(datedFrom, EarliestYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(datedTo, LatestYear);
        if (datedFrom > datedTo)
        {
            throw new ArgumentException($"The dating span starts in {datedFrom} after it ends in {datedTo}.", nameof(datedFrom));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(folioCount, 1);
        Siglum = siglum;
        Repository = repository;
        DatedFrom = datedFrom;
        DatedTo = datedTo;
        FolioCount = folioCount;
    }

    public WitnessSiglum Siglum { get; }

    public string Repository { get; }

    public int DatedFrom { get; }

    public int DatedTo { get; }

    public int FolioCount { get; }

    /// <summary>True when this witness was certainly written before <paramref name="other"/> could have been.</summary>
    public bool IsCertainlyOlderThan(Witness other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return DatedTo < other.DatedFrom;
    }
}
