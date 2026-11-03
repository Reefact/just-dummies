// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShelfLife --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShelfLife" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShelfLife : IAny<ShelfLife> {

    private IAny<int> _months;

    private AnyInt32 _monthsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShelfLife()
        : this(months: AnyValidMonths()) { }

    private static AnyInt32 AnyValidMonths() {
        return Any.Int32().Positive().LessThanOrEqualTo(60);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyShelfLife(AnyInt32 months) {
        _months       = months;
        _monthsRecipe = months;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyShelfLife(AnyShelfLife from) {
        _months       = from._months;
        _monthsRecipe = from._monthsRecipe;
    }

    /// <summary>Pins <c>months</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyShelfLife WithMonths(int value) {
        AnyShelfLife copy = new AnyShelfLife(this);

        copy._months = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>months</c> is drawn from — <c>AnyValidMonths()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyShelfLife WithMonths(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShelfLife copy = new AnyShelfLife(this);

        copy._monthsRecipe = configure(_monthsRecipe);
        copy._months       = copy._monthsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ShelfLife" />.</summary>
    public ShelfLife Generate() {
        return new ShelfLife(_months.Generate());
    }

}
