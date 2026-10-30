// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GuestName --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GuestName" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGuestName : IAny<GuestName> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGuestName()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        return Any.String().NotBlank().WithMaxLength(60);
    }

    private AnyGuestName(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyGuestName WithValue(string value) {
        return new AnyGuestName(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestName WithValue(Func<AnyString, AnyString> configure) {
        return new AnyGuestName(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="GuestName" />.</summary>
    public GuestName Generate() {
        return new GuestName(_value.Generate());
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
