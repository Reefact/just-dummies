// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Rate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Rate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRate : IAny<Rate> {

    private IAny<decimal> _fraction;

    private AnyDecimal _fractionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRate()
        : this(fraction: AnyValidFraction()) { }

    private static AnyDecimal AnyValidFraction() {
        return Any.Decimal().Between(0m, 1m).WithScale(4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRate(AnyDecimal fraction) {
        _fraction       = fraction;
        _fractionRecipe = fraction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRate(AnyRate from) {
        _fraction       = from._fraction;
        _fractionRecipe = from._fractionRecipe;
    }

    /// <summary>Pins <c>fraction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRate WithFraction(decimal value) {
        AnyRate copy = new AnyRate(this);

        copy._fraction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fraction</c> is drawn from — <c>AnyValidFraction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRate WithFraction(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRate copy = new AnyRate(this);

        copy._fractionRecipe = configure(_fractionRecipe);
        copy._fraction       = copy._fractionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Rate" />.</summary>
    public Rate Generate() {
        return new Rate(_fraction.Generate());
    }

}
