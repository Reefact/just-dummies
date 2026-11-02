// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Aircraft --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Aviation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Aircraft" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAircraft : IAny<Aircraft> {

    private IAny<AircraftRegistration> _registration;
    private IAny<string>               _typeDesignator;
    private IAny<int>                  _seats;

    private AnyAircraftRegistration _registrationRecipe;
    private AnyString               _typeDesignatorRecipe;
    private AnyInt32                _seatsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAircraft()
        : this(registration:   AnyValidRegistration(),
               typeDesignator: AnyValidTypeDesignator(),
               seats:          AnyValidSeats()) { }

    private static AnyAircraftRegistration AnyValidRegistration() {
        return new AnyAircraftRegistration();
    }

    private static AnyString AnyValidTypeDesignator() {
        return Any.String().OneOf(Aircraft.KnownTypes);
    }

    private static AnyInt32 AnyValidSeats() {
        // 78 seats fit every type of the fleet reference list (the smallest maximum, AT76).
        return Any.Int32().Between(1, 78);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAircraft(AnyAircraftRegistration registration,
                        AnyString               typeDesignator,
                        AnyInt32                seats) {
        _registration         = registration;
        _registrationRecipe   = registration;
        _typeDesignator       = typeDesignator;
        _typeDesignatorRecipe = typeDesignator;
        _seats                = seats;
        _seatsRecipe          = seats;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAircraft(AnyAircraft from) {
        _registration         = from._registration;
        _registrationRecipe   = from._registrationRecipe;
        _typeDesignator       = from._typeDesignator;
        _typeDesignatorRecipe = from._typeDesignatorRecipe;
        _seats                = from._seats;
        _seatsRecipe          = from._seatsRecipe;
    }

    /// <summary>Pins <c>registration</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAircraft WithRegistration(AircraftRegistration value) {
        AnyAircraft copy = new AnyAircraft(this);

        copy._registration = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>registration</c> is drawn from — <c>AnyValidRegistration()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAircraftRegistration, AnyAircraftRegistration&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAircraft WithRegistration(Func<AnyAircraftRegistration, AnyAircraftRegistration> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAircraft copy = new AnyAircraft(this);

        copy._registrationRecipe = configure(_registrationRecipe);
        copy._registration       = copy._registrationRecipe;

        return copy;
    }

    /// <summary>Pins <c>typeDesignator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAircraft WithTypeDesignator(string value) {
        AnyAircraft copy = new AnyAircraft(this);

        copy._typeDesignator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>typeDesignator</c> is drawn from — <c>AnyValidTypeDesignator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAircraft WithTypeDesignator(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAircraft copy = new AnyAircraft(this);

        copy._typeDesignatorRecipe = configure(_typeDesignatorRecipe);
        copy._typeDesignator       = copy._typeDesignatorRecipe;

        return copy;
    }

    /// <summary>Pins <c>seats</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAircraft WithSeats(int value) {
        AnyAircraft copy = new AnyAircraft(this);

        copy._seats = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>seats</c> is drawn from — <c>AnyValidSeats()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAircraft WithSeats(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAircraft copy = new AnyAircraft(this);

        copy._seatsRecipe = configure(_seatsRecipe);
        copy._seats       = copy._seatsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Aircraft" />.</summary>
    public Aircraft Generate() {
        return new Aircraft(_registration.Generate(),
                            _typeDesignator.Generate(),
                            _seats.Generate());
    }

}
