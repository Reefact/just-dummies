// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StayPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StayPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStayPeriod : IAny<StayPeriod> {

    private readonly IAny<DateOnly>                 _checkIn;
    private readonly Func<DateOnly, IAny<DateOnly>> _checkOut;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStayPeriod()
        : this(checkIn:  AnyValidCheckIn(),
               checkOut: AnyValidCheckOut) { }

    // Hand-edited: the scaffold drew both dates independently. The check-out depends on the
    // check-in drawn first (StayPeriod's cross-field guard), so its base is a function of it.
    private static AnyDateOnly AnyValidCheckIn() {
        // A hotel's booking horizon; it also keeps AddDays(30) far from DateOnly.MaxValue.
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2040, 12, 31));
    }

    private static AnyDateOnly AnyValidCheckOut(DateOnly checkIn) {
        return Any.DateOnly().After(checkIn).BeforeOrEqualTo(checkIn.AddDays(StayPeriod.MaxNights));
    }

    private AnyStayPeriod(IAny<DateOnly>                 checkIn,
                          Func<DateOnly, IAny<DateOnly>> checkOut) {
        _checkIn  = checkIn;
        _checkOut = checkOut;
    }

    /// <summary>Pins <c>checkIn</c> to a fixed value.</summary>
    public AnyStayPeriod WithCheckIn(DateOnly value) {
        return new AnyStayPeriod(new FixedValue<DateOnly>(value), _checkOut);
    }

    /// <summary>Draws <c>checkIn</c> from <c>AnyValidCheckIn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStayPeriod WithCheckIn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyStayPeriod(configure(AnyValidCheckIn()), _checkOut);
    }

    /// <summary>Pins the number of nights; the check-out follows the drawn check-in.</summary>
    public AnyStayPeriod WithNights(int nights) {
        return new AnyStayPeriod(_checkIn, checkIn => new FixedValue<DateOnly>(checkIn.AddDays(nights)));
    }

    /// <summary>Draws <c>checkOut</c> from <c>AnyValidCheckOut(checkIn)</c>, refined by <paramref name="configure" />.</summary>
    public AnyStayPeriod WithCheckOut(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyStayPeriod(_checkIn, checkIn => configure(AnyValidCheckOut(checkIn)));
    }

    /// <summary>Produces one arbitrary <see cref="StayPeriod" />.</summary>
    public StayPeriod Generate() {
        DateOnly checkIn = _checkIn.Generate();

        return new StayPeriod(checkIn, _checkOut(checkIn).Generate());
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
