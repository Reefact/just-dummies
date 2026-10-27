namespace Trading.Domain;

/// <summary>The smallest price increment an instrument trades in (0.01, 0.05, 0.0001...).</summary>
public sealed class TickSize {

    public const decimal Largest = 1m;

    public TickSize(decimal increment) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(increment);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(increment, Largest);
        if (increment.Scale > 4) {
            throw new ArgumentException("A tick size has at most four decimal places.", nameof(increment));
        }

        Increment = increment;
    }

    public decimal Increment { get; }

    public bool Admits(decimal price) {
        return price % Increment == 0m;
    }

    /// <summary>Rounds a price to the tick grid, towards the side that never worsens the order.</summary>
    public decimal Round(decimal price, Side side) {
        decimal ticks = price / Increment;
        decimal whole = side switch {
            Side.Buy  => decimal.Floor(ticks),
            Side.Sell => decimal.Ceiling(ticks),
            _         => throw new ArgumentOutOfRangeException(nameof(side), side, null)
        };

        return whole * Increment;
    }

    public override bool Equals(object? obj) {
        return obj is TickSize other && other.Increment == Increment;
    }

    public override int GetHashCode() {
        return Increment.GetHashCode();
    }

}
