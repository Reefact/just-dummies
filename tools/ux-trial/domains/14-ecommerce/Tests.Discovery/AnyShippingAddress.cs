// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShippingAddress --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShippingAddress" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShippingAddress : IAny<ShippingAddress> {

    private IAny<string> _street;
    private IAny<string> _city;
    private IAny<string> _postalCode;
    private IAny<string> _countryCode;

    private AnyString _streetRecipe;
    private AnyString _cityRecipe;
    private AnyString _postalCodeRecipe;
    private AnyString _countryCodeRecipe;

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

    private static AnyString AnyValidPostalCode() {
        // Five digits (FR, DE); a Belgian postal code has four, which Generate() draws itself.
        return Any.String().Numeric().WithLength(5);
    }

    private static AnyString AnyValidCountryCode() {
        return Any.String().OneOf("FR", "BE", "DE");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShippingAddress(AnyString street,
                               AnyString city,
                               AnyString postalCode,
                               AnyString countryCode) {
        _street            = street;
        _streetRecipe      = street;
        _city              = city;
        _cityRecipe        = city;
        _postalCode        = postalCode;
        _postalCodeRecipe  = postalCode;
        _countryCode       = countryCode;
        _countryCodeRecipe = countryCode;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShippingAddress(AnyShippingAddress from) {
        _street            = from._street;
        _streetRecipe      = from._streetRecipe;
        _city              = from._city;
        _cityRecipe        = from._cityRecipe;
        _postalCode        = from._postalCode;
        _postalCodeRecipe  = from._postalCodeRecipe;
        _countryCode       = from._countryCode;
        _countryCodeRecipe = from._countryCodeRecipe;
    }

    /// <summary>Pins <c>street</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShippingAddress WithStreet(string value) {
        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._street = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>street</c> is drawn from — <c>AnyValidStreet()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShippingAddress WithStreet(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._streetRecipe = configure(_streetRecipe);
        copy._street       = copy._streetRecipe;

        return copy;
    }

    /// <summary>Pins <c>city</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShippingAddress WithCity(string value) {
        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._city = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>city</c> is drawn from — <c>AnyValidCity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShippingAddress WithCity(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._cityRecipe = configure(_cityRecipe);
        copy._city       = copy._cityRecipe;

        return copy;
    }

    /// <summary>Pins <c>postalCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShippingAddress WithPostalCode(string value) {
        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._postalCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>postalCode</c> is drawn from — <c>AnyValidPostalCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShippingAddress WithPostalCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._postalCodeRecipe = configure(_postalCodeRecipe);
        copy._postalCode       = copy._postalCodeRecipe;

        return copy;
    }

    /// <summary>Pins <c>countryCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShippingAddress WithCountryCode(string value) {
        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._countryCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>countryCode</c> is drawn from — <c>AnyValidCountryCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShippingAddress WithCountryCode(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShippingAddress copy = new AnyShippingAddress(this);

        copy._countryCodeRecipe = configure(_countryCodeRecipe);
        copy._countryCode       = copy._countryCodeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ShippingAddress" />.</summary>
    public ShippingAddress Generate() {
        string countryCode = _countryCode.Generate();
        string postalCode  = countryCode == "BE" ? Any.String().Numeric().WithLength(4).Generate() : _postalCode.Generate();

        return new ShippingAddress(_street.Generate(),
                                   _city.Generate(),
                                   postalCode,
                                   countryCode);
    }

}
