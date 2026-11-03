// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LossDistribution --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using CatastropheLosses.Domain;
using JustDummies;

namespace CatastropheLosses.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="LossDistribution" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLossDistribution : IAny<LossDistribution> {

    private IAny<IReadOnlyList<decimal>> _annualLosses;

    private AnyList<decimal> _annualLossesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLossDistribution()
        : this(annualLosses: AnyValidAnnualLosses()) { }

    private static AnyList<decimal> AnyValidAnnualLosses() {
        return Any.ListOf(Any.Decimal().GreaterThanOrEqualTo(0m)).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLossDistribution(AnyList<decimal> annualLosses) {
        _annualLosses       = annualLosses;
        _annualLossesRecipe = annualLosses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLossDistribution(AnyLossDistribution from) {
        _annualLosses       = from._annualLosses;
        _annualLossesRecipe = from._annualLossesRecipe;
    }

    /// <summary>Pins <c>annualLosses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLossDistribution WithAnnualLosses(IReadOnlyList<decimal> value) {
        AnyLossDistribution copy = new AnyLossDistribution(this);

        copy._annualLosses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>annualLosses</c> is drawn from — <c>AnyValidAnnualLosses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;decimal&gt;, AnyList&lt;decimal&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLossDistribution WithAnnualLosses(Func<AnyList<decimal>, AnyList<decimal>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLossDistribution copy = new AnyLossDistribution(this);

        copy._annualLossesRecipe = configure(_annualLossesRecipe);
        copy._annualLosses       = copy._annualLossesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LossDistribution" />.</summary>
    public LossDistribution Generate() {
        return new LossDistribution(_annualLosses.Generate());
    }

}
