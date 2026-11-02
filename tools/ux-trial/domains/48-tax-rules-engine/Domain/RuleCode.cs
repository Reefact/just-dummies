using System.Text.RegularExpressions;

namespace TaxRules.Domain;

/// <summary>
/// The code of a tax rule, unique within a schedule: 3 to 24 characters, upper-case words of
/// letters and digits separated by single underscores, starting with a letter (<c>INCOME_TAX</c>).
/// </summary>
public sealed class RuleCode : IEquatable<RuleCode>
{
    public const int MinLength = 3;
    public const int MaxLength = 24;

    private static readonly Regex Pattern = new("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$", RegexOptions.CultureInvariant);

    public RuleCode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length < MinLength || value.Length > MaxLength)
        {
            throw new ArgumentException($"A rule code has {MinLength} to {MaxLength} characters; '{value}' has {value.Length}.", nameof(value));
        }

        if (!Pattern.IsMatch(value))
        {
            throw new FormatException($"'{value}' is not a rule code: upper-case words joined by single underscores, starting with a letter.");
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(RuleCode? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as RuleCode);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
