// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReimbursementSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReimbursementSchedule : IAny<ReimbursementSchedule> {

    private IAny<IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate>> _rates;

    private AnyDictionary<ReimbursementCategory, ReimbursementRate> _ratesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReimbursementSchedule()
        : this(rates: AnyValidRates()) { }

    private static AnyDictionary<ReimbursementCategory, ReimbursementRate> AnyValidRates() {
        return Any.DictionaryOf(Any.Enum<ReimbursementCategory>(), new AnyReimbursementRate()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReimbursementSchedule(AnyDictionary<ReimbursementCategory, ReimbursementRate> rates) {
        _rates       = rates;
        _ratesRecipe = rates;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReimbursementSchedule(AnyReimbursementSchedule from) {
        _rates       = from._rates;
        _ratesRecipe = from._ratesRecipe;
    }

    /// <summary>Pins <c>rates</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementSchedule WithRates(IReadOnlyDictionary<ReimbursementCategory, ReimbursementRate> value) {
        AnyReimbursementSchedule copy = new AnyReimbursementSchedule(this);

        copy._rates = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rates</c> is drawn from — <c>AnyValidRates()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDictionary&lt;ReimbursementCategory, ReimbursementRate&gt;, AnyDictionary&lt;ReimbursementCategory, ReimbursementRate&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementSchedule WithRates(Func<AnyDictionary<ReimbursementCategory, ReimbursementRate>, AnyDictionary<ReimbursementCategory, ReimbursementRate>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementSchedule copy = new AnyReimbursementSchedule(this);

        copy._ratesRecipe = configure(_ratesRecipe);
        copy._rates       = copy._ratesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReimbursementSchedule" />.</summary>
    public ReimbursementSchedule Generate() {
        return new ReimbursementSchedule(_rates.Generate());
    }

}
