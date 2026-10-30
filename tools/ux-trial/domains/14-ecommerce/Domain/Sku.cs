using System.Text.RegularExpressions;

namespace Shop.Domain;

/// <summary>A stock keeping unit: three upper-case letters, a hyphen, four digits (e.g. "TSH-0042").</summary>
public sealed class Sku {

    private static readonly Regex Format = new Regex(@"^[A-Z]{3}-\d{4}$", RegexOptions.Compiled);

    public Sku(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a SKU (expected AAA-9999).", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public string Category => Value.Substring(0, 3);

    public override bool Equals(object? obj) => obj is Sku other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

}
