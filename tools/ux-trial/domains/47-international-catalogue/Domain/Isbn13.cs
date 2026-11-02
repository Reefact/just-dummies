namespace InternationalCatalogue.Domain;

/// <summary>
/// An ISBN-13: prefix 978 or 979, nine more digits and a check digit (weights 1 and 3, modulo 10).
/// Hyphens may separate groups (never leading, trailing or doubled); the value is stored without them.
/// </summary>
public sealed class Isbn13 : IEquatable<Isbn13>
{
    public Isbn13(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string digits = RemoveHyphens(value, nameof(value));
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            throw new FormatException($"'{value}' is not 13 ASCII digits.");
        }

        if (!digits.StartsWith("978", StringComparison.Ordinal) && !digits.StartsWith("979", StringComparison.Ordinal))
        {
            throw new FormatException($"An ISBN-13 starts with 978 or 979, not '{digits[..3]}'.");
        }

        int expected = ComputeCheckDigit(digits[..12]);
        if (digits[12] - '0' != expected)
        {
            throw new FormatException($"Check digit of '{value}' should be {expected}.");
        }

        Value = digits;
    }

    /// <summary>The 13 digits, without hyphens.</summary>
    public string Value { get; }

    /// <summary>Only a 978 ISBN has an ISBN-10 form.</summary>
    public bool HasIsbn10Form => Value.StartsWith("978", StringComparison.Ordinal);

    public Isbn10 ToIsbn10()
    {
        if (!HasIsbn10Form)
        {
            throw new InvalidOperationException($"ISBN {Value} has prefix 979 and no ISBN-10 form.");
        }

        string nine = Value[3..12];
        return new Isbn10(nine + Isbn10.ComputeCheckCharacter(nine));
    }

    public static int ComputeCheckDigit(string twelveDigits)
    {
        ArgumentNullException.ThrowIfNull(twelveDigits);
        if (twelveDigits.Length != 12)
        {
            throw new ArgumentException("The check digit is computed over 12 digits.", nameof(twelveDigits));
        }

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += (twelveDigits[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return (10 - (sum % 10)) % 10;
    }

    internal static string RemoveHyphens(string value, string paramName)
    {
        if (value.StartsWith('-') || value.EndsWith('-') || value.Contains("--", StringComparison.Ordinal))
        {
            throw new ArgumentException("Hyphens only separate groups: never leading, trailing or doubled.", paramName);
        }

        return value.Replace("-", string.Empty, StringComparison.Ordinal);
    }

    public bool Equals(Isbn13? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Isbn13);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
