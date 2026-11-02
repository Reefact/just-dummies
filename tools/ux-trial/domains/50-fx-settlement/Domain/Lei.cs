using System.Text.RegularExpressions;

namespace FxSettlement.Domain;

/// <summary>
/// A counterparty's Legal Entity Identifier (ISO 17442): 18 upper-case letters or digits followed by
/// two check digits, the whole code being 1 modulo 97 once letters are read as 10..35 (e.g. 5493001KJTIIGC8Y1R12).
/// </summary>
public sealed partial class Lei : IEquatable<Lei>
{
    public Lei(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Pattern().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not an LEI: 18 characters A-Z/0-9 then two check digits.");
        }

        if (Mod97(value) != 1)
        {
            throw new ArgumentException($"'{value}' fails the ISO 17442 check digits.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    private static int Mod97(string value)
    {
        int remainder = 0;
        foreach (char c in value)
        {
            int digitValue = char.IsDigit(c) ? c - '0' : c - 'A' + 10;
            remainder = digitValue >= 10
                ? ((remainder * 100) + digitValue) % 97
                : ((remainder * 10) + digitValue) % 97;
        }

        return remainder;
    }

    public bool Equals(Lei? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Lei);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{18}[0-9]{2}$")]
    private static partial Regex Pattern();
}
