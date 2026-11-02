// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AircraftRegistration --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="AircraftRegistration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAircraftRegistration : IAny<AircraftRegistration> {

    // N + 1..5 digits + 0..1 letter, or N + 1..4 digits + 2 letters: the N-number form, kept within seven characters.
    internal const string UnitedStatesPattern = "N[1-9]([0-9]{0,4}[A-Z]?|[0-9]{0,3}[A-Z]{2})";
    internal const string HyphenatedPattern   = "[A-MO-Z][A-Z]?-[A-Z]{3,4}";

    private IAny<string> _mark;

    private AnyPattern _markRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAircraftRegistration()
        : this(mark: AnyValidMark()) { }

    private static AnyPattern AnyValidMark() {
        // Either form the registration accepts, within its seven characters. The hyphenated form cannot begin with N:
        // a mark whose first character is N is only ever matched against the N-number form.
        return Any.StringMatching(UnitedStatesPattern + "|" + HyphenatedPattern);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAircraftRegistration(AnyPattern mark) {
        _mark       = mark;
        _markRecipe = mark;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAircraftRegistration(AnyAircraftRegistration from) {
        _mark       = from._mark;
        _markRecipe = from._markRecipe;
    }

    /// <summary>Pins <c>mark</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAircraftRegistration WithMark(string value) {
        AnyAircraftRegistration copy = new AnyAircraftRegistration(this);

        copy._mark = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>mark</c> is drawn from — <c>AnyValidMark()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAircraftRegistration WithMark(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAircraftRegistration copy = new AnyAircraftRegistration(this);

        copy._markRecipe = configure(_markRecipe);
        copy._mark       = copy._markRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AircraftRegistration" />.</summary>
    public AircraftRegistration Generate() {
        return new AircraftRegistration(_mark.Generate());
    }

}
