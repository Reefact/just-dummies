namespace InternationalCatalogue.Domain;

/// <summary>
/// A legacy ISBN-10: nine digits and a check character 0–9 or X (weights 10 down to 1, sum divisible by 11).
/// A lower-case x is accepted and stored as X. Hyphens follow the same rule as for <see cref="Isbn13"/>.
/// </summary>
public sealed class Isbn10 : IEquatable<Isbn10>
{
    public Isbn10(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string compact = Isbn13.RemoveHyphens(value, nameof(value));
        if (compact.Length != 10)
        {
            throw new FormatException($"'{value}' does not have 10 characters once hyphens are removed.");
        }

        for (int i = 0; i < 9; i++)
        {
            if (!char.IsAsciiDigit(compact[i]))
            {
                throw new FormatException($"Character {i + 1} of '{value}' must be a digit.");
            }
        }

        char check = char.ToUpperInvariant(compact[9]);
        if (!char.IsAsciiDigit(check) && check != 'X')
        {
            throw new FormatException($"The check character of '{value}' must be a digit or X.");
        }

        char expected = ComputeCheckCharacter(compact[..9]);
        if (check != expected)
        {
            throw new FormatException($"Check character of '{value}' should be {expected}.");
        }

        Value = compact[..9] + check;
    }

    /// <summary>Nine digits and the check character, without hyphens.</summary>
    public string Value { get; }

    public Isbn13 ToIsbn13()
    {
        string twelve = "978" + Value[..9];
        return new Isbn13(twelve + Isbn13.ComputeCheckDigit(twelve));
    }

    public static char ComputeCheckCharacter(string nineDigits)
    {
        ArgumentNullException.ThrowIfNull(nineDigits);
        if (nineDigits.Length != 9)
        {
            throw new ArgumentException("The check character is computed over 9 digits.", nameof(nineDigits));
        }

        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            sum += (nineDigits[i] - '0') * (10 - i);
        }

        int check = (11 - (sum % 11)) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }

    public bool Equals(Isbn10? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as Isbn10);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
