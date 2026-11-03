// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AgeAtLeastCondition --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AgeAtLeastCondition" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAgeAtLeastCondition : IAny<AgeAtLeastCondition> {

    private IAny<int> _years;

    private AnyInt32 _yearsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAgeAtLeastCondition()
        : this(years: AnyValidYears()) { }

    private static AnyInt32 AnyValidYears() {
        return Any.Int32().Positive().LessThanOrEqualTo(AgeAtLeastCondition.MaxYears);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAgeAtLeastCondition(AnyInt32 years) {
        _years       = years;
        _yearsRecipe = years;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAgeAtLeastCondition(AnyAgeAtLeastCondition from) {
        _years       = from._years;
        _yearsRecipe = from._yearsRecipe;
    }

    /// <summary>Pins <c>years</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAgeAtLeastCondition WithYears(int value) {
        AnyAgeAtLeastCondition copy = new AnyAgeAtLeastCondition(this);

        copy._years = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>years</c> is drawn from — <c>AnyValidYears()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAgeAtLeastCondition WithYears(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAgeAtLeastCondition copy = new AnyAgeAtLeastCondition(this);

        copy._yearsRecipe = configure(_yearsRecipe);
        copy._years       = copy._yearsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AgeAtLeastCondition" />.</summary>
    public AgeAtLeastCondition Generate() {
        return new AgeAtLeastCondition(_years.Generate());
    }

}
