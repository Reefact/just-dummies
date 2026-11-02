using System.Globalization;

namespace ShiftPlanning.Domain;

/// <summary>An ISO 8601 week: Monday to Sunday, week 1 holds the year's first Thursday; a year has 52 or 53 weeks.</summary>
public sealed class IsoWeek : IEquatable<IsoWeek>, IComparable<IsoWeek>
{
    public IsoWeek(int year, int week)
    {
        if (year < 1 || year > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "ISO year must be between 1 and 9999.");
        }

        int weeksInYear = ISOWeek.GetWeeksInYear(year);
        if (week < 1 || week > weeksInYear)
        {
            throw new ArgumentOutOfRangeException(nameof(week), week, $"ISO year {year} has weeks 1 to {weeksInYear}.");
        }

        Year = year;
        Week = week;
    }

    public int Year { get; }

    public int Week { get; }

    public DateOnly Monday => DateOnly.FromDateTime(ISOWeek.ToDateTime(Year, Week, DayOfWeek.Monday));

    public DateOnly Sunday => Monday.AddDays(6);

    public static IsoWeek Of(DateOnly date)
    {
        DateTime dateTime = date.ToDateTime(TimeOnly.MinValue);
        return new IsoWeek(ISOWeek.GetYear(dateTime), ISOWeek.GetWeekOfYear(dateTime));
    }

    public bool Contains(DateOnly date) => date >= Monday && date <= Sunday;

    public int CompareTo(IsoWeek? other) =>
        other is null ? 1 : Year != other.Year ? Year.CompareTo(other.Year) : Week.CompareTo(other.Week);

    public bool Equals(IsoWeek? other) => other is not null && other.Year == Year && other.Week == Week;

    public override bool Equals(object? obj) => Equals(obj as IsoWeek);

    public override int GetHashCode() => HashCode.Combine(Year, Week);

    public override string ToString() => $"{Year:D4}-W{Week:D2}";
}
