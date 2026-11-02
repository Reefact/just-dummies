namespace HealthcareClaims.Domain;

/// <summary>The share of the price paid that the supplementary insurer reimburses, in percent: above 0, at most 100, one decimal.</summary>
public sealed class ReimbursementRate : IEquatable<ReimbursementRate> {

    public ReimbursementRate(decimal percent) {
        if (percent <= 0m || percent > 100m) {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "A reimbursement rate lies in (0, 100] percent.");
        }
        if (decimal.Round(percent, 1) != percent) {
            throw new ArgumentException("A reimbursement rate carries at most one decimal place.", nameof(percent));
        }

        Percent = percent;
    }

    public decimal Percent { get; }

    /// <summary>The rate applied to an amount, rounded down to the cent: an insurer never pays the extra half-cent.</summary>
    public decimal Of(decimal amount) {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        return decimal.Round(amount * Percent / 100m, 2, MidpointRounding.ToZero);
    }

    public bool Equals(ReimbursementRate? other) => other is not null && other.Percent == Percent;
    public override bool Equals(object? obj) => Equals(obj as ReimbursementRate);
    public override int GetHashCode() => Percent.GetHashCode();
    public override string ToString() => $"{Percent} %";

}
