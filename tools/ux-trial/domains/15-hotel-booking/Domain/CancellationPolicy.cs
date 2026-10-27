namespace Booking.Domain;

/// <summary>Free cancellation up to <see cref="FreeCancellationDays"/> days before the check-in; after that, a share of the total is kept.</summary>
public sealed class CancellationPolicy {

    public CancellationPolicy(int freeCancellationDays, decimal penaltyRate) {
        ArgumentOutOfRangeException.ThrowIfNegative(freeCancellationDays);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(freeCancellationDays, 365);
        if (penaltyRate < 0m || penaltyRate > 1m) {
            throw new ArgumentOutOfRangeException(nameof(penaltyRate), penaltyRate, "A penalty rate lies between 0 and 1.");
        }
        FreeCancellationDays = freeCancellationDays;
        PenaltyRate          = penaltyRate;
    }

    public int     FreeCancellationDays { get; }
    public decimal PenaltyRate          { get; }

    public Money PenaltyFor(Money total, DateOnly cancelledOn, DateOnly checkIn) {
        int daysBefore = checkIn.DayNumber - cancelledOn.DayNumber;
        if (daysBefore >= FreeCancellationDays) {
            return new Money(0m, total.Currency);
        }

        return total.Percent(PenaltyRate);
    }

}
