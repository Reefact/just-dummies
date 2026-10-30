namespace Shop.Domain;

/// <summary>How many units of one SKU a line carries: 1 to 99.</summary>
public sealed class Quantity {

    public const int Maximum = 99;

    public Quantity(int units) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(units);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(units, Maximum);

        Units = units;
    }

    public int Units { get; }

    public Quantity Add(Quantity other) => new Quantity(Units + other.Units);

    public override bool Equals(object? obj) => obj is Quantity other && other.Units == Units;

    public override int GetHashCode() => Units;

}
