using System.Text.RegularExpressions;

namespace Aviation.Domain;

/// <summary>A three-letter IATA airport code, such as CDG.</summary>
public sealed class IataAirportCode {

    private static readonly Regex Format = new Regex("^[A-Z]{3}$", RegexOptions.Compiled);

    public IataAirportCode(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not an IATA airport code.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override bool Equals(object? obj) => obj is IataAirportCode other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

}
