// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bic --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Bic" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBic : IAny<Bic> {

    private IAny<string> _code;

    private AnyPattern _codeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBic()
        : this(code: AnyValidCode()) { }

    private static AnyPattern AnyValidCode() {
        return Any.StringMatching(@"[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBic(AnyPattern code) {
        _code       = code;
        _codeRecipe = code;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBic(AnyBic from) {
        _code       = from._code;
        _codeRecipe = from._codeRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBic WithCode(string value) {
        AnyBic copy = new AnyBic(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBic WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBic copy = new AnyBic(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Bic" />.</summary>
    public Bic Generate() {
        return new Bic(_code.Generate());
    }

}
