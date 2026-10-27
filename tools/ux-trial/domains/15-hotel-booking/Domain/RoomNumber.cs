using System.Text.RegularExpressions;

namespace Booking.Domain;

/// <summary>A room number: a floor (1-99), two digits for the room, and an optional wing letter, e.g. "412" or "1207B".</summary>
public sealed class RoomNumber {

    private static readonly Regex Format = new Regex(@"^[1-9][0-9]?[0-9]{2}[A-D]?$", RegexOptions.Compiled);

    public RoomNumber(string value) {
        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a room number.", nameof(value));
        }
        Value = value;
    }

    public string Value { get; }

    public int Floor => int.Parse(Value.TrimEnd('A', 'B', 'C', 'D')) / 100;

    public override bool Equals(object? obj) => obj is RoomNumber other && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

}
