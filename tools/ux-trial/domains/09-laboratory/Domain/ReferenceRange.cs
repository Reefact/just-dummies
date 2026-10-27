namespace Laboratory.Domain;

/// <summary>
/// The interval of values considered normal for an analyte, in one unit. Both bounds are inclusive.
/// Critical limits, when given, lie outside the normal interval.
/// </summary>
public sealed class ReferenceRange {

    public ReferenceRange(double lower, double upper, MeasurementUnit unit, double? criticalLow = null, double? criticalHigh = null) {
        ArgumentNullException.ThrowIfNull(unit);
        if (!double.IsFinite(lower) || !double.IsFinite(upper)) {
            throw new ArgumentException("Reference bounds must be finite numbers.");
        }
        ArgumentOutOfRangeException.ThrowIfNegative(lower);
        if (lower >= upper) {
            throw new ArgumentException($"The lower bound ({lower}) must be below the upper bound ({upper}).", nameof(lower));
        }
        if (criticalLow is not null && criticalLow.Value > lower) {
            throw new ArgumentOutOfRangeException(nameof(criticalLow), "A critical low limit cannot be above the lower bound.");
        }
        if (criticalHigh is not null && criticalHigh.Value < upper) {
            throw new ArgumentOutOfRangeException(nameof(criticalHigh), "A critical high limit cannot be below the upper bound.");
        }

        Lower        = lower;
        Upper        = upper;
        Unit         = unit;
        CriticalLow  = criticalLow;
        CriticalHigh = criticalHigh;
    }

    public double          Lower        { get; }
    public double          Upper        { get; }
    public MeasurementUnit Unit         { get; }
    public double?         CriticalLow  { get; }
    public double?         CriticalHigh { get; }

    public ResultFlag Classify(double value) {
        if (CriticalLow is not null && value <= CriticalLow.Value) {
            return ResultFlag.CriticalLow;
        }
        if (CriticalHigh is not null && value >= CriticalHigh.Value) {
            return ResultFlag.CriticalHigh;
        }
        if (value < Lower) {
            return ResultFlag.Low;
        }
        if (value > Upper) {
            return ResultFlag.High;
        }

        return ResultFlag.Normal;
    }

}
