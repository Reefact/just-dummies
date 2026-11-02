using System.Text.RegularExpressions;

namespace ClinicalTrial.Domain;

/// <summary>
/// One laboratory value of a visit. The result is <see langword="null"/> while not entered, or resolved as an
/// observed value or a missing reason. An observed value carries its unit; a missing one carries neither unit
/// nor reference range.
/// </summary>
public sealed partial class LabResult : IEquatable<LabResult>
{
    public const int MaxUnitLength = 20;

    public LabResult(
        string analyteCode,
        Observed<decimal>? result,
        string? unit,
        decimal? lowerLimitOfNormal,
        decimal? upperLimitOfNormal)
    {
        ArgumentNullException.ThrowIfNull(analyteCode);
        if (!IsAnalyteCode(analyteCode))
        {
            throw new FormatException($"'{analyteCode}' is not an analyte code (an upper-case letter then 1 to 9 letters or digits, e.g. ALT, HGB).");
        }

        if (unit is not null && (unit.Length == 0 || unit.Length > MaxUnitLength || unit.Any(char.IsWhiteSpace)))
        {
            throw new ArgumentException($"A unit is 1 to {MaxUnitLength} characters without spaces (e.g. g/L, 10^9/L).", nameof(unit));
        }

        if (result?.Value is decimal value && value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(result), value, "A laboratory value is not negative.");
        }

        if (lowerLimitOfNormal is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lowerLimitOfNormal), lowerLimitOfNormal, "A limit of normal is not negative.");
        }

        if (upperLimitOfNormal is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(upperLimitOfNormal), upperLimitOfNormal, "A limit of normal is not negative.");
        }

        if (lowerLimitOfNormal is decimal lower && upperLimitOfNormal is decimal upper && lower >= upper)
        {
            throw new ArgumentException($"The lower limit of normal ({lower}) must be below the upper limit ({upper}).", nameof(upperLimitOfNormal));
        }

        bool hasRange = lowerLimitOfNormal.HasValue || upperLimitOfNormal.HasValue;
        if (result is { IsObserved: true } && unit is null)
        {
            throw new ArgumentException($"An observed value of {analyteCode} needs its unit.", nameof(unit));
        }

        if (result is { IsObserved: false })
        {
            if (unit is not null)
            {
                throw new ArgumentException($"A value of {analyteCode} missing as {result.Reason} carries no unit.", nameof(unit));
            }

            if (hasRange)
            {
                throw new ArgumentException($"A value of {analyteCode} missing as {result.Reason} carries no reference range.", nameof(lowerLimitOfNormal));
            }
        }

        if (hasRange && unit is null)
        {
            throw new ArgumentException("A reference range is expressed in a unit.", nameof(unit));
        }

        AnalyteCode = analyteCode;
        Result = result;
        Unit = unit;
        LowerLimitOfNormal = lowerLimitOfNormal;
        UpperLimitOfNormal = upperLimitOfNormal;
    }

    public string AnalyteCode { get; }

    public Observed<decimal>? Result { get; }

    public string? Unit { get; }

    public decimal? LowerLimitOfNormal { get; }

    public decimal? UpperLimitOfNormal { get; }

    public bool IsResolved => Result is not null;

    /// <summary>Low, normal or high against the reference range; not evaluable without a value or a range.</summary>
    public LabFlag Flag
    {
        get
        {
            if (Result?.Value is not decimal value || (LowerLimitOfNormal is null && UpperLimitOfNormal is null))
            {
                return LabFlag.NotEvaluable;
            }

            if (value < LowerLimitOfNormal)
            {
                return LabFlag.Low;
            }

            return value > UpperLimitOfNormal ? LabFlag.High : LabFlag.Normal;
        }
    }

    public static bool IsAnalyteCode(string code) => code is not null && AnalytePattern().IsMatch(code);

    [GeneratedRegex("^[A-Z][A-Z0-9]{1,9}$")]
    private static partial Regex AnalytePattern();

    public bool Equals(LabResult? other) =>
        other is not null
        && other.AnalyteCode == AnalyteCode
        && Equals(other.Result, Result)
        && other.Unit == Unit
        && other.LowerLimitOfNormal == LowerLimitOfNormal
        && other.UpperLimitOfNormal == UpperLimitOfNormal;

    public override bool Equals(object? obj) => Equals(obj as LabResult);

    public override int GetHashCode() => HashCode.Combine(AnalyteCode, Result, Unit, LowerLimitOfNormal, UpperLimitOfNormal);
}
