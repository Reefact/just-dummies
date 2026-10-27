namespace Laboratory.Domain;

/// <summary>A measured quantity: a finite, non-negative magnitude in a unit.</summary>
public sealed class Measurement {

    public Measurement(double value, MeasurementUnit unit) {
        Value = value switch {
            double.NaN                                      => throw new ArgumentException("A measurement cannot be NaN.", nameof(value)),
            double.PositiveInfinity or double.NegativeInfinity => throw new ArgumentException("A measurement must be finite.", nameof(value)),
            < 0                                             => throw new ArgumentOutOfRangeException(nameof(value), "A concentration cannot be negative."),
            _                                               => value
        };
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
    }

    public double          Value { get; }
    public MeasurementUnit Unit  { get; }

    /// <summary>The concentration in the undiluted sample.</summary>
    public Measurement Undiluted(DilutionFactor dilution) {
        ArgumentNullException.ThrowIfNull(dilution);

        return new Measurement(Value * (double)dilution.Value, Unit);
    }

    public override string ToString() => $"{Value} {Unit}";

}
