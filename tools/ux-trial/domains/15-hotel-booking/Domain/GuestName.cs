namespace Booking.Domain;

public sealed class GuestName {

    public const int MaxLength = 60;

    public GuestName(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > MaxLength) {
            throw new ArgumentOutOfRangeException(nameof(value), value.Length, $"A guest name holds at most {MaxLength} characters.");
        }
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;

}
