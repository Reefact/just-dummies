namespace Booking.Domain;

/// <summary>A stay: from the check-in date to the check-out date, at least one night, at most thirty.</summary>
public sealed class StayPeriod {

    public const int MaxNights = 30;

    public StayPeriod(DateOnly checkIn, DateOnly checkOut) {
        if (checkOut <= checkIn) {
            throw new ArgumentException("The check-out must come after the check-in.", nameof(checkOut));
        }
        if (checkOut.DayNumber - checkIn.DayNumber > MaxNights) {
            throw new ArgumentOutOfRangeException(nameof(checkOut), $"A stay lasts at most {MaxNights} nights.");
        }
        CheckIn  = checkIn;
        CheckOut = checkOut;
    }

    public DateOnly CheckIn  { get; }
    public DateOnly CheckOut { get; }

    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

    public bool Overlaps(StayPeriod other) => CheckIn < other.CheckOut && other.CheckIn < CheckOut;

}
