// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate IncomeAboveCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="IncomeAboveCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyIncomeAboveCondition : IAny<IncomeAboveCondition> {

    private IAny<IncomeKind> _kinds;
    private IAny<Money>      _threshold;

    private AnyEnum<IncomeKind> _kindsRecipe;
    private AnyMoney            _thresholdRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyIncomeAboveCondition()
        : this(kinds:     AnyValidKinds(),
               threshold: AnyValidThreshold()) { }

    private static AnyEnum<IncomeKind> AnyValidKinds() {
        return Plausible.TaxBase();
    }

    private static AnyMoney AnyValidThreshold() {
        return Plausible.Amount();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyIncomeAboveCondition(AnyEnum<IncomeKind> kinds,
                                    AnyMoney            threshold) {
        _kinds           = kinds;
        _kindsRecipe     = kinds;
        _threshold       = threshold;
        _thresholdRecipe = threshold;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyIncomeAboveCondition(AnyIncomeAboveCondition from) {
        _kinds           = from._kinds;
        _kindsRecipe     = from._kindsRecipe;
        _threshold       = from._threshold;
        _thresholdRecipe = from._thresholdRecipe;
    }

    /// <summary>Pins <c>kinds</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIncomeAboveCondition WithKinds(IncomeKind value) {
        AnyIncomeAboveCondition copy = new AnyIncomeAboveCondition(this);

        copy._kinds = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kinds</c> is drawn from — <c>AnyValidKinds()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;IncomeKind&gt;, AnyEnum&lt;IncomeKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIncomeAboveCondition WithKinds(Func<AnyEnum<IncomeKind>, AnyEnum<IncomeKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIncomeAboveCondition copy = new AnyIncomeAboveCondition(this);

        copy._kindsRecipe = configure(_kindsRecipe);
        copy._kinds       = copy._kindsRecipe;

        return copy;
    }

    /// <summary>Pins <c>threshold</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyIncomeAboveCondition WithThreshold(Money value) {
        AnyIncomeAboveCondition copy = new AnyIncomeAboveCondition(this);

        copy._threshold = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>threshold</c> is drawn from — <c>AnyValidThreshold()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyIncomeAboveCondition WithThreshold(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyIncomeAboveCondition copy = new AnyIncomeAboveCondition(this);

        copy._thresholdRecipe = configure(_thresholdRecipe);
        copy._threshold       = copy._thresholdRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="IncomeAboveCondition" />.</summary>
    public IncomeAboveCondition Generate() {
        return new IncomeAboveCondition(_kinds.Generate(),
                                        _threshold.Generate());
    }

}
