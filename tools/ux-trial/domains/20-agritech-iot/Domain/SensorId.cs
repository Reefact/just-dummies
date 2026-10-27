namespace AgriTech.Domain;

/// <summary>A field sensor identifier: "AG-" followed by six upper-case hexadecimal digits.</summary>
public sealed class SensorId : IEquatable<SensorId> {

    public const string Prefix = "AG-";

    public SensorId(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != 9 || !value.StartsWith(Prefix, StringComparison.Ordinal)) {
            throw new ArgumentException("A sensor id is AG- followed by six hexadecimal digits.", nameof(value));
        }
        foreach (char digit in value.AsSpan(Prefix.Length)) {
            if (!char.IsAsciiHexDigitUpper(digit) && !char.IsAsciiDigit(digit)) {
                throw new ArgumentException("A sensor id is AG- followed by six hexadecimal digits.", nameof(value));
            }
        }
        Value = value;
    }

    public string Value { get; }

    public bool Equals(SensorId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as SensorId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

}
