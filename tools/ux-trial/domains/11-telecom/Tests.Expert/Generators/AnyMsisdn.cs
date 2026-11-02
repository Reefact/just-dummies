// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Msisdn --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Telecom.Domain;

namespace Telecom.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Msisdn" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMsisdn : IAny<Msisdn> {

    private IAny<string> _digits;

    private AnyPattern _digitsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMsisdn()
        : this(digits: AnyValidDigits()) { }

    private static AnyPattern AnyValidDigits() {
        return Any.StringMatching("[1-9][0-9]{7,14}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMsisdn(AnyPattern digits) {
        _digits       = digits;
        _digitsRecipe = digits;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMsisdn(AnyMsisdn from) {
        _digits       = from._digits;
        _digitsRecipe = from._digitsRecipe;
    }

    /// <summary>Pins <c>digits</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMsisdn WithDigits(string value) {
        AnyMsisdn copy = new AnyMsisdn(this);

        copy._digits = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>digits</c> is drawn from — <c>AnyValidDigits()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMsisdn WithDigits(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMsisdn copy = new AnyMsisdn(this);

        copy._digitsRecipe = configure(_digitsRecipe);
        copy._digits       = copy._digitsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Msisdn" />.</summary>
    public Msisdn Generate() {
        return new Msisdn(_digits.Generate());
    }

}
