namespace Education.Domain;

// Variants of the real guards, kept only to see which shapes the `any` scaffolder reads.

public sealed class SplitRangeMark {
    public SplitRangeMark(decimal value) {
        if (value < 0m) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value > 20m) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }

    public decimal Value { get; }
}

public sealed class HelperRangeMark {
    public HelperRangeMark(decimal value) {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 20m);
        if (value % 0.5m != 0m) {
            throw new ArgumentException("half points", nameof(value));
        }
        Value = value;
    }

    public decimal Value { get; }
}

public sealed class CourseLoad {
    public CourseLoad(IReadOnlyList<string> codes) {
        ArgumentNullException.ThrowIfNull(codes);
        ArgumentOutOfRangeException.ThrowIfZero(codes.Count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(codes.Count, 8);
        Codes = codes;
    }

    public IReadOnlyList<string> Codes { get; }
}

public sealed class ConstBoundMark {
    public ConstBoundMark(decimal value) {
        if (value < 0m) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (value > Grade.Maximum) {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        Value = value;
    }

    public decimal Value { get; }
}

public sealed class SwitchGuardedCredits {
    public SwitchGuardedCredits(int credits) {
        Credits = credits switch {
            <= 0 or > 30 => throw new ArgumentOutOfRangeException(nameof(credits)),
            _            => credits
        };
    }

    public int Credits { get; }
}
