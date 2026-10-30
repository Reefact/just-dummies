namespace Trading.Domain;

/// <summary>A number of shares: strictly positive, at most one million per order.</summary>
public sealed class Quantity {

    public const int Maximum = 1_000_000;

    public Quantity(int shares) {
        if (shares is <= 0 or > Maximum) {
            throw new ArgumentOutOfRangeException(nameof(shares), shares, $"A quantity is between 1 and {Maximum}.");
        }

        Shares = shares;
    }

    public int Shares { get; }

    public override bool Equals(object? obj) {
        return obj is Quantity other && other.Shares == Shares;
    }

    public override int GetHashCode() {
        return Shares;
    }

}
