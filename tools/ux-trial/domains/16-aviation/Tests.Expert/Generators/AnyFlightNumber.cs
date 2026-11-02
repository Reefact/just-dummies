// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FlightNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Aviation.Domain;
using JustDummies;

namespace Aviation.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="FlightNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFlightNumber : IAny<FlightNumber> {

    private IAny<string> _airlineDesignator;
    private IAny<int>    _number;

    private AnyString _airlineDesignatorRecipe;
    private AnyInt32  _numberRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFlightNumber()
        : this(airlineDesignator: AnyValidAirlineDesignator(),
               number:            AnyValidNumber()) { }

    private static AnyString AnyValidAirlineDesignator() {
        // Two upper-case letters or digits.
        return Any.String().AlphaNumeric().InUpperCase().WithLength(2);
    }

    private static AnyInt32 AnyValidNumber() {
        // The digits of a designator: one to four of them.
        return Any.Int32().Between(1, 9999);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyFlightNumber(AnyString airlineDesignator,
                            AnyInt32  number) {
        _airlineDesignator       = airlineDesignator;
        _airlineDesignatorRecipe = airlineDesignator;
        _number                  = number;
        _numberRecipe            = number;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyFlightNumber(AnyFlightNumber from) {
        _airlineDesignator       = from._airlineDesignator;
        _airlineDesignatorRecipe = from._airlineDesignatorRecipe;
        _number                  = from._number;
        _numberRecipe            = from._numberRecipe;
    }

    /// <summary>Pins <c>airlineDesignator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlightNumber WithAirlineDesignator(string value) {
        AnyFlightNumber copy = new AnyFlightNumber(this);

        copy._airlineDesignator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>airlineDesignator</c> is drawn from — <c>AnyValidAirlineDesignator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlightNumber WithAirlineDesignator(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlightNumber copy = new AnyFlightNumber(this);

        copy._airlineDesignatorRecipe = configure(_airlineDesignatorRecipe);
        copy._airlineDesignator       = copy._airlineDesignatorRecipe;

        return copy;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyFlightNumber WithNumber(int value) {
        AnyFlightNumber copy = new AnyFlightNumber(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyFlightNumber WithNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyFlightNumber copy = new AnyFlightNumber(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="FlightNumber" />.</summary>
    public FlightNumber Generate() {
        return new FlightNumber(_airlineDesignator.Generate(),
                                _number.Generate());
    }

}
