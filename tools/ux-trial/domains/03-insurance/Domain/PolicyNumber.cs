using System.Text.RegularExpressions;

namespace Insurance.Domain;

/// <summary>A policy number such as <c>POL-2026-FR123456</c>: year of issue, country, six digits.</summary>
public sealed class PolicyNumber {

    private static readonly Regex Format = new(@"^POL-\d{4}-[A-Z]{2}\d{6}$", RegexOptions.Compiled);

    public PolicyNumber(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a policy number (POL-yyyy-CC999999).", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override bool Equals(object? obj) => obj is PolicyNumber other && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
