using System.Globalization;
using System.Text.RegularExpressions;

namespace InternationalCatalogue.Domain;

/// <summary>
/// A Dewey-style shelf mark: a three-digit class with an optional decimal part (1–8 digits), a space and a
/// Cutter number (an upper-case letter and 1–4 digits), e.g. <c>823.914 R884</c>. Ordered in shelf order:
/// class number, then Cutter letter, then Cutter digits read as a decimal fraction (R884 before R89).
/// </summary>
public sealed partial class CallNumber : IEquatable<CallNumber>, IComparable<CallNumber>
{
    public CallNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Match match = CallNumberPattern().Match(value);
        if (!match.Success)
        {
            throw new FormatException($"'{value}' is not a call number such as '823.914 R884'.");
        }

        ClassNumber = decimal.Parse(match.Groups["class"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        CutterLetter = match.Groups["letter"].Value[0];
        CutterFraction = decimal.Parse("0." + match.Groups["cutter"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        Value = value;
    }

    public string Value { get; }

    /// <summary>The class number, 0 to 999.99999999.</summary>
    public decimal ClassNumber { get; }

    public char CutterLetter { get; }

    /// <summary>The Cutter digits read as a decimal fraction (R884 → 0.884).</summary>
    public decimal CutterFraction { get; }

    public int CompareTo(CallNumber? other)
    {
        if (other is null)
        {
            return 1;
        }

        int byClass = ClassNumber.CompareTo(other.ClassNumber);
        if (byClass != 0)
        {
            return byClass;
        }

        int byLetter = CutterLetter.CompareTo(other.CutterLetter);
        if (byLetter != 0)
        {
            return byLetter;
        }

        int byCutter = CutterFraction.CompareTo(other.CutterFraction);
        return byCutter != 0 ? byCutter : string.CompareOrdinal(Value, other.Value);
    }

    [GeneratedRegex(@"^(?<class>[0-9]{3}(?:\.[0-9]{1,8})?) (?<letter>[A-Z])(?<cutter>[0-9]{1,4})\z")]
    private static partial Regex CallNumberPattern();

    public bool Equals(CallNumber? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as CallNumber);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
