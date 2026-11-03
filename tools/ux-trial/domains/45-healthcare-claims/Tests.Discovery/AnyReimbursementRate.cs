// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReimbursementRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace HealthcareClaims.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReimbursementRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReimbursementRate : IAny<ReimbursementRate> {

    private IAny<decimal> _percent;

    private AnyDecimal _percentRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReimbursementRate()
        : this(percent: AnyValidPercent()) { }

    private static AnyDecimal AnyValidPercent() {
        return Any.Decimal().Positive().LessThanOrEqualTo(100m).WithScale(1);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyReimbursementRate(AnyDecimal percent) {
        _percent       = percent;
        _percentRecipe = percent;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyReimbursementRate(AnyReimbursementRate from) {
        _percent       = from._percent;
        _percentRecipe = from._percentRecipe;
    }

    /// <summary>Pins <c>percent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyReimbursementRate WithPercent(decimal value) {
        AnyReimbursementRate copy = new AnyReimbursementRate(this);

        copy._percent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>percent</c> is drawn from — <c>AnyValidPercent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyReimbursementRate WithPercent(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReimbursementRate copy = new AnyReimbursementRate(this);

        copy._percentRecipe = configure(_percentRecipe);
        copy._percent       = copy._percentRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ReimbursementRate" />.</summary>
    public ReimbursementRate Generate() {
        return new ReimbursementRate(_percent.Generate());
    }

}
