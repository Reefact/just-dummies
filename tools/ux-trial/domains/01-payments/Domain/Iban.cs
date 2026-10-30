using System.Numerics;
using System.Text.RegularExpressions;

namespace Payments.Domain;

/// <summary>An International Bank Account Number, stored in its electronic (unspaced, upper-case) form.</summary>
public sealed class Iban : IEquatable<Iban> {

    private static readonly Regex Format = new(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$", RegexOptions.Compiled);

    public Iban(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Validate(value);
        Value = value;
    }

    public string Value { get; }

    public string CountryCode => Value[..2];

    /// <summary>Computes the two check digits for a country code and a BBAN (ISO 13616, mod 97-10).</summary>
    public static string ComputeCheckDigits(string countryCode, string bban) {
        BigInteger number = BigInteger.Parse(ToDigits(bban + countryCode + "00"));
        int checkDigits = 98 - (int)(number % 97);

        return checkDigits.ToString("00");
    }

    private static void Validate(string value) {
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not an IBAN in electronic format.", nameof(value));
        }
        string rearranged = value[4..] + value[..4];
        if (BigInteger.Parse(ToDigits(rearranged)) % 97 != 1) {
            throw new ArgumentException($"'{value}' has wrong check digits.", nameof(value));
        }
    }

    private static string ToDigits(string text) {
        return string.Concat(text.Select(c => char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));
    }

    public bool Equals(Iban? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Iban);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

}
