using System.Globalization;
using System.Text.RegularExpressions;

namespace Clinical.Domain;

/// <summary>French social security number (NIR): 13 characters plus a 2-digit control key.</summary>
public sealed class Nir {

    private const string Pattern = @"^[12][0-9]{2}(0[1-9]|1[0-2])([0-9]{2}|2[AB])[0-9]{6}[0-9]{2}$";

    public Nir(string value) {
        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (!Regex.IsMatch(value, Pattern)) {
            throw new ArgumentException($"'{value}' is not a well-formed NIR.", nameof(value));
        }
        int expectedKey = ComputeKey(value.Substring(0, 13));
        int actualKey   = int.Parse(value.Substring(13, 2), CultureInfo.InvariantCulture);
        if (expectedKey != actualKey) {
            throw new ArgumentException($"NIR control key {actualKey:00} is wrong, expected {expectedKey:00}.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public Sex Sex => Value[0] == '1' ? Sex.Male : Sex.Female;

    public int BirthYearTwoDigits => int.Parse(Value.Substring(1, 2), CultureInfo.InvariantCulture);

    public int BirthMonth => int.Parse(Value.Substring(3, 2), CultureInfo.InvariantCulture);

    /// <summary>The control key of a 13-character NIR body: 97 minus the body modulo 97 (Corsica: 2A reads 19, 2B reads 18).</summary>
    public static int ComputeKey(string body) {
        string numeric = body.Replace("2A", "19").Replace("2B", "18");
        long   number  = long.Parse(numeric, CultureInfo.InvariantCulture);

        return (int)(97 - number % 97);
    }

    public override bool Equals(object? obj) {
        return obj is Nir other && other.Value == Value;
    }

    public override int GetHashCode() {
        return Value.GetHashCode();
    }

    public override string ToString() {
        return Value;
    }

}
