namespace SmartGrid.Domain;

/// <summary>A cumulative register value in kWh, at most three decimal places.</summary>
public sealed class MeterIndex : IEquatable<MeterIndex> {

    public const decimal MaxKwh = 99_999_999m;

    public MeterIndex(decimal kwh) {
        ArgumentOutOfRangeException.ThrowIfNegative(kwh);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(kwh, MaxKwh);
        if (kwh.Scale > 3) {
            throw new ArgumentException("A meter index carries at most three decimal places.", nameof(kwh));
        }

        Kwh = kwh;
    }

    public decimal Kwh { get; }

    public bool Equals(MeterIndex? other) {
        return other is not null && Kwh == other.Kwh;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as MeterIndex);
    }

    public override int GetHashCode() {
        return Kwh.GetHashCode();
    }

}
