// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Address --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Address" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAddress : IAny<Address> {

    private readonly IAny<string> _street;
    private readonly IAny<string> _city;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAddress()
        : this(street: AnyValidStreet(),
               city:   AnyValidCity()) { }

    private static AnyString AnyValidStreet() {
        return Any.String().NonEmpty();
    }

    private static AnyString AnyValidCity() {
        return Any.String().NonEmpty();
    }

    private AnyAddress(IAny<string> street,
                       IAny<string> city) {
        _street = street;
        _city   = city;
    }

    /// <summary>Pins <c>street</c> to a fixed value.</summary>
    public AnyAddress WithStreet(string value) {
        return new AnyAddress(new FixedValue<string>(value), _city);
    }

    /// <summary>Draws <c>street</c> from <c>AnyValidStreet()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAddress WithStreet(Func<AnyString, AnyString> configure) {
        return new AnyAddress(configure(AnyValidStreet()), _city);
    }

    /// <summary>Pins <c>city</c> to a fixed value.</summary>
    public AnyAddress WithCity(string value) {
        return new AnyAddress(_street, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>city</c> from <c>AnyValidCity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAddress WithCity(Func<AnyString, AnyString> configure) {
        return new AnyAddress(_street, configure(AnyValidCity()));
    }

    /// <summary>Produces one arbitrary <see cref="Address" />.</summary>
    public Address Generate() {
        return new Address(_street.Generate(),
                           _city.Generate());
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
