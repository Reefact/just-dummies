// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShippingAddress --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="ShippingAddress" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShippingAddress : IAny<ShippingAddress> {

    private readonly IAny<string> _street;
    private readonly IAny<string> _city;
    private readonly IAny<string> _postalCode;
    private readonly IAny<string> _countryCode;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShippingAddress()
        : this(street:      AnyValidStreet(),
               city:        AnyValidCity(),
               postalCode:  AnyValidPostalCode(),
               countryCode: AnyValidCountryCode()) { }

    private static AnyString AnyValidStreet() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidCity() {
        return Any.String().NotBlank().WithMaxLength(50);
    }

    private static AnyPattern AnyValidPostalCode() {
        // Five digits is the format in FR and DE; BE (four digits) is left out of the base below,
        // since a parameter's base cannot depend on another parameter's draw.
        return Any.StringMatching(@"\d{5}");
    }

    private static AnyString AnyValidCountryCode() {
        // The shop delivers to FR, BE and DE; BE is left out because its postal code has four digits.
        return Any.String().OneOf("FR", "DE");
    }

    private AnyShippingAddress(IAny<string> street,
                               IAny<string> city,
                               IAny<string> postalCode,
                               IAny<string> countryCode) {
        _street      = street;
        _city        = city;
        _postalCode  = postalCode;
        _countryCode = countryCode;
    }

    /// <summary>Pins <c>street</c> to a fixed value.</summary>
    public AnyShippingAddress WithStreet(string value) {
        return new AnyShippingAddress(new FixedValue<string>(value), _city, _postalCode, _countryCode);
    }

    /// <summary>Draws <c>street</c> from <c>AnyValidStreet()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShippingAddress WithStreet(Func<AnyString, AnyString> configure) {
        return new AnyShippingAddress(configure(AnyValidStreet()), _city, _postalCode, _countryCode);
    }

    /// <summary>Pins <c>city</c> to a fixed value.</summary>
    public AnyShippingAddress WithCity(string value) {
        return new AnyShippingAddress(_street, new FixedValue<string>(value), _postalCode, _countryCode);
    }

    /// <summary>Draws <c>city</c> from <c>AnyValidCity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShippingAddress WithCity(Func<AnyString, AnyString> configure) {
        return new AnyShippingAddress(_street, configure(AnyValidCity()), _postalCode, _countryCode);
    }

    /// <summary>Pins <c>postalCode</c> to a fixed value.</summary>
    public AnyShippingAddress WithPostalCode(string value) {
        return new AnyShippingAddress(_street, _city, new FixedValue<string>(value), _countryCode);
    }

    /// <summary>Draws <c>postalCode</c> from <c>AnyValidPostalCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShippingAddress WithPostalCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnyShippingAddress(_street, _city, configure(AnyValidPostalCode()), _countryCode);
    }

    /// <summary>Pins <c>countryCode</c> to a fixed value.</summary>
    public AnyShippingAddress WithCountryCode(string value) {
        return new AnyShippingAddress(_street, _city, _postalCode, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>countryCode</c> from <c>AnyValidCountryCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShippingAddress WithCountryCode(Func<AnyString, AnyString> configure) {
        return new AnyShippingAddress(_street, _city, _postalCode, configure(AnyValidCountryCode()));
    }

    /// <summary>Produces one arbitrary <see cref="ShippingAddress" />.</summary>
    public ShippingAddress Generate() {
        return new ShippingAddress(_street.Generate(),
                                   _city.Generate(),
                                   _postalCode.Generate(),
                                   _countryCode.Generate());
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
