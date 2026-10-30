namespace Payroll.Domain;

/// <summary>A gross hourly rate in euros, to the cent.</summary>
public sealed class HourlyRate {

    public const decimal Ceiling = 500m;

    public HourlyRate(decimal value) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Ceiling);
        if (decimal.Round(value, 2) != value) {
            throw new ArgumentException("An hourly rate is expressed to the cent.", nameof(value));
        }

        Value = value;
    }

    public decimal Value { get; }

}
