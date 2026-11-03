// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SiteZone --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using ShiftPlanning.Domain;

namespace ShiftPlanning.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="SiteZone" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySiteZone : IAny<SiteZone> {

    private IAny<string> _ianaId;

    private AnyString _ianaIdRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySiteZone()
        : this(ianaId: AnyValidIanaId()) { }

    private static AnyString AnyValidIanaId() {
        // Zones that are all out of any transition between January and February: a dummy zone must not move a test across one.
        return Any.String().OneOf("Europe/Paris", "America/New_York", "Asia/Tokyo", "Australia/Sydney", "Asia/Kolkata", "UTC");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySiteZone(AnyString ianaId) {
        _ianaId       = ianaId;
        _ianaIdRecipe = ianaId;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySiteZone(AnySiteZone from) {
        _ianaId       = from._ianaId;
        _ianaIdRecipe = from._ianaIdRecipe;
    }

    /// <summary>Pins <c>ianaId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySiteZone WithIanaId(string value) {
        AnySiteZone copy = new AnySiteZone(this);

        copy._ianaId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>ianaId</c> is drawn from — <c>AnyValidIanaId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySiteZone WithIanaId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySiteZone copy = new AnySiteZone(this);

        copy._ianaIdRecipe = configure(_ianaIdRecipe);
        copy._ianaId       = copy._ianaIdRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SiteZone" />.</summary>
    public SiteZone Generate() {
        return new SiteZone(_ianaId.Generate());
    }

}
