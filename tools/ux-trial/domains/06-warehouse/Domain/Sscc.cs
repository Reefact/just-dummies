using System.Text.RegularExpressions;

namespace Warehouse.Domain;

/// <summary>Serial Shipping Container Code: 18 digits, the last one a GS1 check digit.</summary>
public sealed class Sscc {

    private static readonly Regex Format = new(@"^[0-9]{18}$");

    public Sscc(string value) {
        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"An SSCC is 18 digits, got '{value}'.", nameof(value));
        }
        if (!Gs1.HasValidCheckDigit(value)) {
            throw new ArgumentException($"'{value}' does not carry a valid GS1 check digit.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public char ExtensionDigit => Value[0];

    public override bool Equals(object? obj) => obj is Sscc other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

}
