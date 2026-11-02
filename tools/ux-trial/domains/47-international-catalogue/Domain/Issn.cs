using System.Text.RegularExpressions;

namespace InternationalCatalogue.Domain;

/// <summary>An ISSN, written <c>NNNN-NNNC</c>: seven digits (weights 8 down to 2) and a check character 0–9 or X.</summary>
public sealed partial class Issn : IEquatable<Issn>
{
    public Issn(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!IssnPattern().IsMatch(value))
        {
            throw new FormatException($"'{value}' is not written NNNN-NNNC (C a digit or upper-case X).");
        }

        string seven = value[..4] + value[5..8];
        char expected = ComputeCheckCharacter(seven);
        if (value[8] != expected)
        {
            throw new FormatException($"Check character of '{value}' should be {expected}.");
        }

        Value = value;
    }

    public string Value { get; }

    public static char ComputeCheckCharacter(string sevenDigits)
    {
        ArgumentNullException.ThrowIfNull(sevenDigits);
        int sum = 0;
        for (int i = 0; i < 7; i++)
        {
            sum += (sevenDigits[i] - '0') * (8 - i);
        }

        int check = (11 - (sum % 11)) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }

    [GeneratedRegex(@"^[0-9]{4}-[0-9]{3}[0-9X]\z")]
    private static partial Regex IssnPattern();

    public bool Equals(Issn? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Issn);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
