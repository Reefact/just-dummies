namespace Booking.Domain;

public sealed class Room {

    public Room(RoomNumber number, RoomType type, int capacity, Money nightlyRate) {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(nightlyRate);
        int maximum = type switch {
            RoomType.Single => 1,
            RoomType.Double => 2,
            RoomType.Twin   => 2,
            RoomType.Suite  => 4,
            _               => throw new ArgumentOutOfRangeException(nameof(type))
        };
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        if (capacity > maximum) {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"A {type} room sleeps at most {maximum}.");
        }
        if (nightlyRate.Amount == 0m) {
            throw new ArgumentException("A room is never free.", nameof(nightlyRate));
        }
        Number      = number;
        Type        = type;
        Capacity    = capacity;
        NightlyRate = nightlyRate;
    }

    public RoomNumber Number      { get; }
    public RoomType   Type        { get; }
    public int        Capacity    { get; }
    public Money      NightlyRate { get; }

}
