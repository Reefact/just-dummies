using System.Text.RegularExpressions;

namespace Telecom.Domain;

/// <summary>An international mobile number in E.164 digits: a country code, no leading zero, 8 to 15 digits.</summary>
public sealed class Msisdn {

    private static readonly Regex Format = new Regex(@"^[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant);

    public Msisdn(string digits) {
        if (!Format.IsMatch(digits)) {
            throw new ArgumentException($"'{digits}' is not an E.164 MSISDN.", nameof(digits));
        }

        Digits = digits;
    }

    public string Digits { get; }

    public bool IsFrenchMobile => Digits.StartsWith("336", StringComparison.Ordinal) || Digits.StartsWith("337", StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Msisdn other && other.Digits == Digits;

    public override int GetHashCode() => Digits.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => "+" + Digits;

}
