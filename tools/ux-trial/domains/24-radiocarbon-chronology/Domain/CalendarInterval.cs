namespace RadiocarbonChronology.Domain;

/// <summary>A closed interval of astronomical calendar years (1 BCE is 0, 2 BCE is -1).</summary>
public sealed class CalendarInterval : IEquatable<CalendarInterval>
{
    public const int EarliestYear = -55_000;
    public const int LatestYear = 1950;

    public CalendarInterval(int startYear, int endYear)
    {
        if (startYear < EarliestYear || startYear > LatestYear)
        {
            throw new ArgumentOutOfRangeException(nameof(startYear), startYear, $"A calendar year must lie between {EarliestYear} and {LatestYear}.");
        }

        if (endYear < EarliestYear || endYear > LatestYear)
        {
            throw new ArgumentOutOfRangeException(nameof(endYear), endYear, $"A calendar year must lie between {EarliestYear} and {LatestYear}.");
        }

        if (startYear > endYear)
        {
            throw new ArgumentException($"The interval starts in {startYear}, after its end in {endYear}.", nameof(endYear));
        }

        StartYear = startYear;
        EndYear = endYear;
    }

    public int StartYear { get; }

    public int EndYear { get; }

    public int LengthInYears => EndYear - StartYear + 1;

    public bool Contains(int year) => year >= StartYear && year <= EndYear;

    public bool Overlaps(CalendarInterval other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartYear <= other.EndYear && other.StartYear <= EndYear;
    }

    public bool Equals(CalendarInterval? other) => other is not null && StartYear == other.StartYear && EndYear == other.EndYear;

    public override bool Equals(object? obj) => Equals(obj as CalendarInterval);

    public override int GetHashCode() => HashCode.Combine(StartYear, EndYear);

    public override string ToString() => $"[{StartYear}, {EndYear}]";
}
