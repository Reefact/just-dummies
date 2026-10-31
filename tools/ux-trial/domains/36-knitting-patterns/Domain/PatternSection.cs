namespace KnittingPatterns.Domain;

/// <summary>A named block of consecutive rows: "Ribbing", "Body", "Lace panel", "Crown decreases".</summary>
public sealed class PatternSection
{
    public const int MaxNameLength = 60;

    private readonly IReadOnlyList<RowInstruction> _rows;

    public PatternSection(string name, IReadOnlyList<RowInstruction> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"A section name holds at most {MaxNameLength} characters.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("A section holds at least one row.", nameof(rows));
        }

        if (rows.Any(r => r is null))
        {
            throw new ArgumentException("A section cannot hold a null row.", nameof(rows));
        }

        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i].RowNumber != rows[i - 1].RowNumber + 1)
            {
                throw new ArgumentException(
                    $"Section '{name}' jumps from row {rows[i - 1].RowNumber} to row {rows[i].RowNumber}.", nameof(rows));
            }

            if (rows[i].Side == rows[i - 1].Side)
            {
                throw new ArgumentException(
                    $"Section '{name}' works rows {rows[i - 1].RowNumber} and {rows[i].RowNumber} on the same side; flat rows alternate.",
                    nameof(rows));
            }
        }

        Name = name.Trim();
        _rows = rows.ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<RowInstruction> Rows => _rows;

    public int FirstRow => _rows[0].RowNumber;

    public int LastRow => _rows[^1].RowNumber;
}
