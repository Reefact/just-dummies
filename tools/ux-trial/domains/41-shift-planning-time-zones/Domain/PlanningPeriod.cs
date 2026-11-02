namespace ShiftPlanning.Domain;

/// <summary>The consecutive ISO weeks a roster covers: from 1 to 13 weeks, starting on the Monday of the first one.</summary>
public sealed class PlanningPeriod : IEquatable<PlanningPeriod>
{
    public const int MaxWeeks = 13;

    public PlanningPeriod(IsoWeek firstWeek, int weekCount)
    {
        ArgumentNullException.ThrowIfNull(firstWeek);
        ArgumentOutOfRangeException.ThrowIfLessThan(weekCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(weekCount, MaxWeeks);
        if (firstWeek.Monday.DayNumber + (7 * weekCount) - 1 > DateOnly.MaxValue.DayNumber)
        {
            throw new ArgumentOutOfRangeException(nameof(weekCount), weekCount, "The period would run past the last representable date.");
        }

        FirstWeek = firstWeek;
        WeekCount = weekCount;
    }

    public IsoWeek FirstWeek { get; }

    public int WeekCount { get; }

    public DateOnly FirstDay => FirstWeek.Monday;

    public DateOnly LastDay => FirstDay.AddDays((7 * WeekCount) - 1);

    public bool Contains(DateOnly date) => date >= FirstDay && date <= LastDay;

    public IReadOnlyList<IsoWeek> Weeks() =>
        Enumerable.Range(0, WeekCount).Select(i => IsoWeek.Of(FirstDay.AddDays(7 * i))).ToList();

    public bool Equals(PlanningPeriod? other) => other is not null && other.FirstWeek.Equals(FirstWeek) && other.WeekCount == WeekCount;

    public override bool Equals(object? obj) => Equals(obj as PlanningPeriod);

    public override int GetHashCode() => HashCode.Combine(FirstWeek, WeekCount);

    public override string ToString() => $"{FirstWeek} +{WeekCount}w";
}
