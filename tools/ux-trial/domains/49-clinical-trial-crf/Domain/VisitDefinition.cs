using System.Text.RegularExpressions;

namespace ClinicalTrial.Domain;

/// <summary>
/// A scheduled visit of the protocol: a code (e.g. <c>D28</c>), a target study day and a tolerance window
/// (e.g. day 28 − 3 / + 3), and the laboratory analytes the visit requires.
/// </summary>
public sealed partial class VisitDefinition : IEquatable<VisitDefinition>
{
    public const int MaxTargetDay = 1095;
    public const int MaxToleranceDays = 30;

    private readonly string[] _requiredAnalytes;

    public VisitDefinition(string code, int targetDay, int daysBefore, int daysAfter, IEnumerable<string> requiredAnalytes)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(requiredAnalytes);
        if (!CodePattern().IsMatch(code))
        {
            throw new FormatException($"'{code}' is not a visit code (an upper-case letter then up to 7 letters or digits).");
        }

        if (targetDay < 1 || targetDay > MaxTargetDay)
        {
            throw new ArgumentOutOfRangeException(nameof(targetDay), targetDay, $"A scheduled visit falls on study day 1 to {MaxTargetDay}.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(daysBefore);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysBefore, MaxToleranceDays);
        ArgumentOutOfRangeException.ThrowIfNegative(daysAfter);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysAfter, MaxToleranceDays);
        if (targetDay - daysBefore < 1)
        {
            throw new ArgumentException(
                $"The window of day {targetDay} cannot open {daysBefore} days earlier: it would start before randomisation (day 1).",
                nameof(daysBefore));
        }

        string[] analytes = requiredAnalytes.ToArray();
        foreach (string analyte in analytes)
        {
            if (analyte is null || !LabResult.IsAnalyteCode(analyte))
            {
                throw new ArgumentException($"'{analyte}' is not an analyte code.", nameof(requiredAnalytes));
            }
        }

        if (analytes.Distinct(StringComparer.Ordinal).Count() != analytes.Length)
        {
            throw new ArgumentException("An analyte is required once per visit.", nameof(requiredAnalytes));
        }

        Code = code;
        TargetDay = targetDay;
        DaysBefore = daysBefore;
        DaysAfter = daysAfter;
        _requiredAnalytes = analytes;
    }

    public string Code { get; }

    public int TargetDay { get; }

    public int DaysBefore { get; }

    public int DaysAfter { get; }

    public IReadOnlyList<string> RequiredAnalytes => _requiredAnalytes;

    public int EarliestDay => TargetDay - DaysBefore;

    public int LatestDay => TargetDay + DaysAfter;

    public bool Contains(int studyDay) => studyDay >= EarliestDay && studyDay <= LatestDay;

    [GeneratedRegex("^[A-Z][A-Z0-9]{0,7}$")]
    private static partial Regex CodePattern();

    public bool Equals(VisitDefinition? other) =>
        other is not null
        && other.Code == Code
        && other.TargetDay == TargetDay
        && other.DaysBefore == DaysBefore
        && other.DaysAfter == DaysAfter
        && other._requiredAnalytes.SequenceEqual(_requiredAnalytes);

    public override bool Equals(object? obj) => Equals(obj as VisitDefinition);

    public override int GetHashCode() => HashCode.Combine(Code, TargetDay, DaysBefore, DaysAfter);

    public override string ToString() => DaysBefore == DaysAfter
        ? $"{Code} (day {TargetDay} ± {DaysBefore})"
        : $"{Code} (day {TargetDay} −{DaysBefore}/+{DaysAfter})";
}
