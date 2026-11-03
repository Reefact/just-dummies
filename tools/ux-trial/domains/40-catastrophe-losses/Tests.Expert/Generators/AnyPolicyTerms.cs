// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PolicyTerms --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using CatastropheLosses.Domain;
using JustDummies;

namespace CatastropheLosses.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PolicyTerms" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPolicyTerms : IAny<PolicyTerms> {

    private IAny<decimal> _deductible;
    private IAny<decimal> _limit;

    private AnyDecimal _deductibleRecipe;
    private AnyDecimal _limitRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPolicyTerms()
        : this(deductible: AnyValidDeductible(),
               limit:      AnyValidLimit()) { }

    private static AnyDecimal AnyValidDeductible() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidLimit() {
        return Any.Decimal().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPolicyTerms(AnyDecimal deductible,
                           AnyDecimal limit) {
        _deductible       = deductible;
        _deductibleRecipe = deductible;
        _limit            = limit;
        _limitRecipe      = limit;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPolicyTerms(AnyPolicyTerms from) {
        _deductible       = from._deductible;
        _deductibleRecipe = from._deductibleRecipe;
        _limit            = from._limit;
        _limitRecipe      = from._limitRecipe;
    }

    /// <summary>Pins <c>deductible</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPolicyTerms WithDeductible(decimal value) {
        AnyPolicyTerms copy = new AnyPolicyTerms(this);

        copy._deductible = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deductible</c> is drawn from — <c>AnyValidDeductible()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPolicyTerms WithDeductible(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPolicyTerms copy = new AnyPolicyTerms(this);

        copy._deductibleRecipe = configure(_deductibleRecipe);
        copy._deductible       = copy._deductibleRecipe;

        return copy;
    }

    /// <summary>Pins <c>limit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPolicyTerms WithLimit(decimal value) {
        AnyPolicyTerms copy = new AnyPolicyTerms(this);

        copy._limit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>limit</c> is drawn from — <c>AnyValidLimit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPolicyTerms WithLimit(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPolicyTerms copy = new AnyPolicyTerms(this);

        copy._limitRecipe = configure(_limitRecipe);
        copy._limit       = copy._limitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PolicyTerms" />.</summary>
    public PolicyTerms Generate() {
        return new PolicyTerms(_deductible.Generate(),
                               _limit.Generate());
    }

}
