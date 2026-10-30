namespace Freight.Domain;

/// <summary>A gross weight in kilograms, within what any ISO container can be rated for.</summary>
public sealed class Weight : IEquatable<Weight> {

    public const double MaxGrossKilograms = 32_500;

    public Weight(double kilograms) {
        if (!double.IsFinite(kilograms)) {
            throw new ArgumentOutOfRangeException(nameof(kilograms), kilograms, "A weight must be a finite number.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilograms);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(kilograms, MaxGrossKilograms);

        Kilograms = kilograms;
    }

    public double Kilograms { get; }

    public double MetricTons => Kilograms / 1_000;

    public bool Equals(Weight? other) => other is not null && Kilograms.Equals(other.Kilograms);

    public override bool Equals(object? obj) => Equals(obj as Weight);

    public override int GetHashCode() => Kilograms.GetHashCode();

    public override string ToString() => $"{Kilograms} kg";

}
