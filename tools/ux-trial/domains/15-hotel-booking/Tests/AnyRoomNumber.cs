// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoomNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RoomNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoomNumber : IAny<RoomNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoomNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // The same pattern RoomNumber checks: floor 1-99, two room digits, an optional wing.
        return Any.StringMatching(@"^[1-9][0-9]?[0-9]{2}[A-D]?$");
    }

    private AnyRoomNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyRoomNumber WithValue(string value) {
        return new AnyRoomNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoomNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyRoomNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="RoomNumber" />.</summary>
    public RoomNumber Generate() {
        return new RoomNumber(_value.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
