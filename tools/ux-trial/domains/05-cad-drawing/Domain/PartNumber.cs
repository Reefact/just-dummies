using System.Text.RegularExpressions;

namespace Drafting.Domain;

/// <summary>A part number such as P004217 or P004217-A2 (with a variant suffix).</summary>
public sealed class PartNumber : IEquatable<PartNumber> {

    private static readonly Regex Format = new(@"^P\d{6}(-[A-Z0-9]{2})?$", RegexOptions.Compiled);

    public PartNumber(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Validate(value);
        Value = value;
    }

    public string Value { get; }

    private static void Validate(string value) {
        if (!Format.IsMatch(value)) {
            throw new FormatException($"'{value}' is not a part number.");
        }
    }

    public bool Equals(PartNumber? other) {
        return other is not null && other.Value == Value;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as PartNumber);
    }

    public override int GetHashCode() {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString() {
        return Value;
    }

}
