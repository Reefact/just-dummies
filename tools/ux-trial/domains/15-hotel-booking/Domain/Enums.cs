namespace Booking.Domain;

public enum RoomType {
    Single,
    Double,
    Twin,
    Suite
}

public enum ReservationStatus {
    Confirmed,
    CheckedIn,
    CheckedOut,
    Cancelled
}
