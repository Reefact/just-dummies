namespace KnittingPatterns.Domain;

/// <summary>A run of identical stitches, such as "k12" or "k2tog 3 times".</summary>
public sealed class StitchRun : IRowElement
{
    /// <summary>Longest run a single instruction may express.</summary>
    public const int MaxCount = 500;

    public StitchRun(StitchKind kind, int count)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown stitch kind.");
        }

        if (count < 1 || count > MaxCount)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, $"A stitch run counts between 1 and {MaxCount} stitches.");
        }

        Kind = kind;
        Count = count;
    }

    public StitchKind Kind { get; }

    public int Count { get; }

    public int Consumes => Kind.Consumes() * Count;

    public int Produces => Kind.Produces() * Count;

    public int OperationCount => Count;

    public IEnumerable<StitchKind> Expand() => Enumerable.Repeat(Kind, Count);

    public override string ToString() => Count == 1 ? Kind.ToString() : $"{Kind} x{Count}";
}
