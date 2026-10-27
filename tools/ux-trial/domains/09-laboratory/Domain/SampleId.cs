namespace Laboratory.Domain;

/// <summary>An accession number printed on the tube label: "S" followed by ten digits.</summary>
public sealed class SampleId : IEquatable<SampleId> {

    public SampleId(string value) {
        Validate(value);
        Value = value;
    }

    public string Value { get; }

    private static void Validate(string value) {
        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (value.Length != 11 || value[0] != 'S') {
            throw new ArgumentException("A sample id is 'S' followed by ten digits.", nameof(value));
        }
        for (int index = 1; index < value.Length; index++) {
            if (!char.IsAsciiDigit(value[index])) {
                throw new ArgumentException("A sample id is 'S' followed by ten digits.", nameof(value));
            }
        }
    }

    public bool Equals(SampleId? other) => other is not null && other.Value == Value;
    public override bool Equals(object? obj) => Equals(obj as SampleId);
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
