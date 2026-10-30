namespace Warehouse.Domain;

/// <summary>A count of units; a single receipt line never exceeds ten thousand units.</summary>
public sealed class Quantity {

    public const int Maximum = 10_000;

    public Quantity(int units) {
        ArgumentOutOfRangeException.ThrowIfNegative(units);
        if (units > Maximum) {
            throw new ArgumentOutOfRangeException(nameof(units), units, $"A line holds at most {Maximum} units.");
        }

        Units = units;
    }

    public int Units { get; }

    public override bool Equals(object? obj) => obj is Quantity other && other.Units == Units;

    public override int GetHashCode() => Units;

    public override string ToString() => Units.ToString(System.Globalization.CultureInfo.InvariantCulture);

}
