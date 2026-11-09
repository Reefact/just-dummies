namespace Repro.Domain;

public sealed class Grade {

    public Grade(int value) {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 4);
        Value = value;
    }

    public int Value { get; }

    public override bool Equals(object? obj) {
        return obj is Grade other && other.Value == Value;
    }

    public override int GetHashCode() {
        return Value;
    }

}
