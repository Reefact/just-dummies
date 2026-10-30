using System.Text.RegularExpressions;

namespace Serialisation.Domain;

/// <summary>A GS1 Global Trade Item Number in its 14-digit form, check digit included.</summary>
public sealed class Gtin {

    private static readonly Regex Shape = new Regex(@"^\d{14}$", RegexOptions.Compiled);

    public Gtin(string value) {
        Validate(value);
        Value = value;
    }

    public string Value { get; }

    /// <summary>Builds a GTIN from its first 13 digits, appending the GS1 mod-10 check digit.</summary>
    public static Gtin FromBody(string firstThirteenDigits) {
        if (firstThirteenDigits is null || firstThirteenDigits.Length != 13 || !firstThirteenDigits.All(char.IsAsciiDigit)) {
            throw new ArgumentException("A GTIN body is exactly 13 digits.", nameof(firstThirteenDigits));
        }

        return new Gtin(firstThirteenDigits + CheckDigitOf(firstThirteenDigits));
    }

    internal static int CheckDigitOf(string body) {
        int sum = 0;
        for (int index = 0; index < body.Length; index++) {
            int digit = body[body.Length - 1 - index] - '0';
            sum += index % 2 == 0 ? digit * 3 : digit;
        }

        return (10 - sum % 10) % 10;
    }

    private static void Validate(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (!Shape.IsMatch(value)) {
            throw new ArgumentException($"A GTIN is 14 digits, got '{value}'.", nameof(value));
        }
        if (CheckDigitOf(value[..13]) != value[13] - '0') {
            throw new ArgumentException($"GTIN '{value}' has a wrong check digit.", nameof(value));
        }
    }

    public override bool Equals(object? obj) => obj is Gtin other && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
