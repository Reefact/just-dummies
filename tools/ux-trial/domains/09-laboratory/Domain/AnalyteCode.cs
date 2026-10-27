using System.Text.RegularExpressions;

namespace Laboratory.Domain;

/// <summary>A LOINC-style analyte code: one to five digits, a hyphen, one check digit (e.g. "2345-7").</summary>
public sealed class AnalyteCode : IEquatable<AnalyteCode> {

    private static readonly Regex Format = new(@"^\d{1,5}-\d$", RegexOptions.CultureInvariant);

    public AnalyteCode(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a LOINC-style code.", nameof(value));
        }
        Value = value;
    }

    public string Value { get; }

    public bool Equals(AnalyteCode? other) => other is not null && other.Value == Value;
    public override bool Equals(object? obj) => Equals(obj as AnalyteCode);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
