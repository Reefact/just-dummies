namespace KnittingPatterns.Domain;

/// <summary>A bracketed repeat, such as "*k2, p2; rep from * 10 times".</summary>
public sealed class StitchRepeat : IRowElement
{
    /// <summary>Most repetitions a single repeat may express.</summary>
    public const int MaxTimes = 1000;

    /// <summary>Most single-stitch operations a repeat may expand to.</summary>
    public const int MaxOperations = 10_000;

    private readonly IReadOnlyList<StitchRun> _runs;

    public StitchRepeat(IReadOnlyList<StitchRun> runs, int times)
    {
        ArgumentNullException.ThrowIfNull(runs);
        if (runs.Count == 0)
        {
            throw new ArgumentException("A repeat holds at least one stitch run.", nameof(runs));
        }

        if (runs.Any(r => r is null))
        {
            throw new ArgumentException("A repeat cannot hold a null stitch run.", nameof(runs));
        }

        if (times < 2 || times > MaxTimes)
        {
            throw new ArgumentOutOfRangeException(nameof(times), times, $"A repeat is worked between 2 and {MaxTimes} times.");
        }

        long operations = (long)runs.Sum(r => r.Count) * times;
        if (operations > MaxOperations)
        {
            throw new ArgumentException($"A repeat expands to {operations} operations, above the {MaxOperations} limit.", nameof(times));
        }

        _runs = runs.ToArray();
        Times = times;
    }

    public IReadOnlyList<StitchRun> Runs => _runs;

    public int Times { get; }

    public int Consumes => _runs.Sum(r => r.Consumes) * Times;

    public int Produces => _runs.Sum(r => r.Produces) * Times;

    public int OperationCount => _runs.Sum(r => r.Count) * Times;

    public IEnumerable<StitchKind> Expand()
    {
        for (int i = 0; i < Times; i++)
        {
            foreach (StitchRun run in _runs)
            {
                foreach (StitchKind stitch in run.Expand())
                {
                    yield return stitch;
                }
            }
        }
    }
}
