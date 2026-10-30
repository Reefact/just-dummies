namespace Insurance.Domain;

/// <summary>The days a policy covers, both inclusive. A car policy never runs longer than one year.</summary>
public sealed class CoveragePeriod {

    public CoveragePeriod(DateOnly start, DateOnly end) {
        if (end <= start) {
            throw new ArgumentException($"A coverage period must end after it starts ({start} .. {end}).", nameof(end));
        }
        if (end > start.AddYears(1)) {
            throw new ArgumentException($"A coverage period cannot exceed one year ({start} .. {end}).", nameof(end));
        }

        Start = start;
        End   = end;
    }

    public DateOnly Start { get; }
    public DateOnly End   { get; }

    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>The period of the same length that starts the day after this one ends.</summary>
    public CoveragePeriod Next() => new(End.AddDays(1), End.AddDays(Days));

}
