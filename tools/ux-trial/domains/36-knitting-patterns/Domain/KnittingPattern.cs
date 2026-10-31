namespace KnittingPatterns.Domain;

/// <summary>A flat knitting pattern: a cast-on, a gauge and its sections, rows numbered 1..n.</summary>
public sealed class KnittingPattern
{
    public const int MaxCastOn = 2000;

    private readonly IReadOnlyList<PatternSection> _sections;

    public KnittingPattern(string title, Gauge gauge, int castOnCount, IReadOnlyList<PatternSection> sections)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(gauge);
        if (castOnCount < 1 || castOnCount > MaxCastOn)
        {
            throw new ArgumentOutOfRangeException(nameof(castOnCount), castOnCount, $"A cast-on counts between 1 and {MaxCastOn} stitches.");
        }

        ArgumentNullException.ThrowIfNull(sections);
        if (sections.Count == 0)
        {
            throw new ArgumentException("A pattern holds at least one section.", nameof(sections));
        }

        if (sections.Any(s => s is null))
        {
            throw new ArgumentException("A pattern cannot hold a null section.", nameof(sections));
        }

        if (sections[0].FirstRow != 1)
        {
            throw new ArgumentException($"The first section starts at row {sections[0].FirstRow}, not row 1.", nameof(sections));
        }

        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < sections.Count; i++)
        {
            if (!names.Add(sections[i].Name))
            {
                throw new ArgumentException($"Two sections are named '{sections[i].Name}'.", nameof(sections));
            }

            if (i > 0 && sections[i].FirstRow != sections[i - 1].LastRow + 1)
            {
                throw new ArgumentException(
                    $"Section '{sections[i].Name}' starts at row {sections[i].FirstRow}; row {sections[i - 1].LastRow + 1} was expected.",
                    nameof(sections));
            }
        }

        Title = title.Trim();
        Gauge = gauge;
        CastOnCount = castOnCount;
        _sections = sections.ToArray();
    }

    public string Title { get; }

    public Gauge Gauge { get; }

    public int CastOnCount { get; }

    public IReadOnlyList<PatternSection> Sections => _sections;

    public int RowCount => _sections[^1].LastRow;

    /// <summary>Works every row on a fresh fabric and returns the expanded rows.</summary>
    /// <exception cref="StitchCountMismatchException">A row does not fit the stitches left by the previous one.</exception>
    public IReadOnlyList<ExpandedRow> Expand()
    {
        Fabric fabric = new(CastOnCount);
        List<ExpandedRow> expanded = new(RowCount);
        foreach (PatternSection section in _sections)
        {
            foreach (RowInstruction row in section.Rows)
            {
                expanded.Add(fabric.Work(section.Name, row));
            }
        }

        return expanded;
    }

    /// <summary>Finished width at the last row, in centimetres.</summary>
    public decimal FinishedWidthCm() => Gauge.WidthOf(Expand()[^1].StitchesAfter);
}
