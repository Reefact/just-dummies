namespace Insurance.Domain;

/// <summary>
///     The French "coefficient de reduction-majoration": 1.00 for a new driver, down 5% per claim-free year
///     (floor 0.50), up 25% per responsible claim (ceiling 3.50).
/// </summary>
public sealed class BonusMalus {

    public const double Floor   = 0.50;
    public const double Ceiling = 3.50;

    public BonusMalus(double coefficient) {
        Validate(coefficient);
        Coefficient = coefficient;
    }

    public double Coefficient { get; }

    public static BonusMalus NewDriver => new(1.00);

    public BonusMalus AfterYear(int responsibleClaims, int partialClaims) {
        ArgumentOutOfRangeException.ThrowIfNegative(responsibleClaims);
        ArgumentOutOfRangeException.ThrowIfNegative(partialClaims);
        if (responsibleClaims == 0 && partialClaims == 0) {
            return new BonusMalus(Math.Max(Floor, Math.Round(Coefficient * 0.95, 2, MidpointRounding.ToZero)));
        }

        double next = Coefficient * Math.Pow(1.25, responsibleClaims) * Math.Pow(1.125, partialClaims);

        return new BonusMalus(Math.Min(Ceiling, Math.Round(next, 2, MidpointRounding.ToZero)));
    }

    private static void Validate(double coefficient) {
        if (double.IsNaN(coefficient)) {
            throw new ArgumentException("A coefficient is a number.", nameof(coefficient));
        }
        if (coefficient < Floor) {
            throw new ArgumentOutOfRangeException(nameof(coefficient), coefficient, "The bonus cannot go below 0.50.");
        }
        if (coefficient > Ceiling) {
            throw new ArgumentOutOfRangeException(nameof(coefficient), coefficient, "The malus cannot go above 3.50.");
        }
    }

}
