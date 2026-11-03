// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BatchGenealogy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BatchGenealogy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBatchGenealogy : IAny<BatchGenealogy> {

    private IAny<string> _siteCode;

    private AnyPattern _siteCodeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBatchGenealogy()
        : this(siteCode: AnyValidSiteCode()) { }

    private static AnyPattern AnyValidSiteCode() {
        return Any.StringMatching("[A-Z]{4}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBatchGenealogy(AnyPattern siteCode) {
        _siteCode       = siteCode;
        _siteCodeRecipe = siteCode;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBatchGenealogy(AnyBatchGenealogy from) {
        _siteCode       = from._siteCode;
        _siteCodeRecipe = from._siteCodeRecipe;
    }

    /// <summary>Pins <c>siteCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBatchGenealogy WithSiteCode(string value) {
        AnyBatchGenealogy copy = new AnyBatchGenealogy(this);

        copy._siteCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>siteCode</c> is drawn from — <c>AnyValidSiteCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBatchGenealogy WithSiteCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBatchGenealogy copy = new AnyBatchGenealogy(this);

        copy._siteCodeRecipe = configure(_siteCodeRecipe);
        copy._siteCode       = copy._siteCodeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BatchGenealogy" />.</summary>
    public BatchGenealogy Generate() {
        return new BatchGenealogy(_siteCode.Generate());
    }

}
