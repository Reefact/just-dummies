namespace Laboratory.Domain;

/// <summary>How much a sample was diluted before measurement: from 1 (neat) to 1000, to two decimal places.</summary>
public sealed class DilutionFactor {

    public DilutionFactor(decimal value) {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1m);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1_000m);
        if (decimal.Round(value, 2) != value) {
            throw new ArgumentException("A dilution factor has at most two decimal places.", nameof(value));
        }
        Value = value;
    }

    public decimal Value { get; }

}
