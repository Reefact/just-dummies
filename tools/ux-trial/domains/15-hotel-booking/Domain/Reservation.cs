namespace Booking.Domain;

public sealed class Reservation {

    private readonly List<GuestName> _guests;

    public Reservation(Guid id, Room room, StayPeriod stay, IReadOnlyList<GuestName> guests, CancellationPolicy policy) {
        if (id == Guid.Empty) {
            throw new ArgumentException("A reservation has an identity.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stay);
        ArgumentNullException.ThrowIfNull(policy);
        ValidateGuests(guests, room);
        Id      = id;
        Room    = room;
        Stay    = stay;
        Policy  = policy;
        _guests = guests.ToList();
        Status  = ReservationStatus.Confirmed;
    }

    public Guid                     Id     { get; }
    public Room                     Room   { get; }
    public StayPeriod               Stay   { get; }
    public CancellationPolicy       Policy { get; }
    public IReadOnlyList<GuestName> Guests => _guests;
    public ReservationStatus        Status { get; private set; }

    public Money Total => Room.NightlyRate.Times(Stay.Nights);

    public void CheckIn(DateOnly today) {
        if (Status != ReservationStatus.Confirmed) {
            throw new InvalidOperationException($"Cannot check in a {Status} reservation.");
        }
        if (today < Stay.CheckIn || today >= Stay.CheckOut) {
            throw new InvalidOperationException("Check-in happens during the stay.");
        }
        Status = ReservationStatus.CheckedIn;
    }

    public void CheckOut() {
        if (Status != ReservationStatus.CheckedIn) {
            throw new InvalidOperationException($"Cannot check out a {Status} reservation.");
        }
        Status = ReservationStatus.CheckedOut;
    }

    public Money Cancel(DateOnly cancelledOn) {
        if (Status != ReservationStatus.Confirmed) {
            throw new InvalidOperationException($"Cannot cancel a {Status} reservation.");
        }
        Status = ReservationStatus.Cancelled;

        return Policy.PenaltyFor(Total, cancelledOn, Stay.CheckIn);
    }

    public bool ConflictsWith(Reservation other) => Room.Number.Equals(other.Room.Number) && Stay.Overlaps(other.Stay)
                                                    && Status != ReservationStatus.Cancelled && other.Status != ReservationStatus.Cancelled;

    private static void ValidateGuests(IReadOnlyList<GuestName> guests, Room room) {
        ArgumentNullException.ThrowIfNull(guests);
        if (guests.Count == 0) {
            throw new ArgumentException("A reservation has at least one guest.", nameof(guests));
        }
        if (guests.Count > room.Capacity) {
            throw new ArgumentException($"Room {room.Number} sleeps {room.Capacity}.", nameof(guests));
        }
    }

}
