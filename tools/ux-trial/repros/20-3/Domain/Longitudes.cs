namespace Repro20Three;

public sealed class HalfOpenLongitude {
    public HalfOpenLongitude(double value) {
        if (value < -180.0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value >= 180.0) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }
    public double Value { get; }
}

public sealed class HelperLongitude {
    public HelperLongitude(double value) {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, -180.0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, 180.0);
        Value = value;
    }
    public double Value { get; }
}

public sealed class InclusiveFloor {
    public InclusiveFloor(int value) {
        if (value <= 10) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }
    public int Value { get; }
}
