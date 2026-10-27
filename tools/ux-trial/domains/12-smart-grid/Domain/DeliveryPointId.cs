using System.Text.RegularExpressions;

namespace SmartGrid.Domain;

/// <summary>A delivery point identifier: exactly fourteen digits.</summary>
public sealed class DeliveryPointId : IEquatable<DeliveryPointId> {

    private static readonly Regex Format = new Regex(@"^\d{14}$", RegexOptions.Compiled);

    public DeliveryPointId(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Format.IsMatch(value)) {
            throw new FormatException($"A delivery point id is fourteen digits, got '{value}'.");
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(DeliveryPointId? other) {
        return other is not null && Value == other.Value;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as DeliveryPointId);
    }

    public override int GetHashCode() {
        return Value.GetHashCode(StringComparison.Ordinal);
    }

    public override string ToString() {
        return Value;
    }

}
