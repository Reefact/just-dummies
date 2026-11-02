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

    private IAny<string> _street;
    private IAny<string> _city;

    private AnyString _streetRecipe;
    private AnyString _cityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAddress()
        : this(street: AnyValidStreet(),
               city:   AnyValidCity()) { }

    private static AnyString AnyValidStreet() {
        return Any.String();
    }

    private static AnyString AnyValidCity() {
        return Any.String();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAddress(AnyString street,
                       AnyString city) {
        _street       = street;
        _streetRecipe = street;
        _city         = city;
        _cityRecipe   = city;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAddress(AnyAddress from) {
        _street       = from._street;
        _streetRecipe = from._streetRecipe;
        _city         = from._city;
        _cityRecipe   = from._cityRecipe;
    }

    /// <summary>Pins <c>street</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAddress WithStreet(string value) {
        AnyAddress copy = new AnyAddress(this);

        copy._street = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>street</c> is drawn from — <c>AnyValidStreet()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAddress WithStreet(Func<AnyString, AnyString> configure) {
        AnyAddress copy = new AnyAddress(this);

        copy._streetRecipe = configure(_streetRecipe);
        copy._street       = copy._streetRecipe;

        return copy;
    }

    /// <summary>Pins <c>city</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAddress WithCity(string value) {
        AnyAddress copy = new AnyAddress(this);

        copy._city = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>city</c> is drawn from — <c>AnyValidCity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAddress WithCity(Func<AnyString, AnyString> configure) {
        AnyAddress copy = new AnyAddress(this);

        copy._cityRecipe = configure(_cityRecipe);
        copy._city       = copy._cityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Address" />.</summary>
    public Address Generate() {
        return new Address(_street.Generate(),
                           _city.Generate());
    }

}
