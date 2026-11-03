// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TickSize --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TickSize" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTickSize : IAny<TickSize> {

    private IAny<decimal> _increment;

    private AnyDecimal _incrementRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTickSize()
        : this(increment: AnyValidIncrement()) { }

    private static AnyDecimal AnyValidIncrement() {
        return Any.Decimal().Positive().LessThanOrEqualTo(TickSize.Largest).WithScale(4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTickSize(AnyDecimal increment) {
        _increment       = increment;
        _incrementRecipe = increment;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTickSize(AnyTickSize from) {
        _increment       = from._increment;
        _incrementRecipe = from._incrementRecipe;
    }

    /// <summary>Pins <c>increment</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTickSize WithIncrement(decimal value) {
        AnyTickSize copy = new AnyTickSize(this);

        copy._increment = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>increment</c> is drawn from — <c>AnyValidIncrement()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTickSize WithIncrement(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTickSize copy = new AnyTickSize(this);

        copy._incrementRecipe = configure(_incrementRecipe);
        copy._increment       = copy._incrementRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="TickSize" />.</summary>
    public TickSize Generate() {
        return new TickSize(_increment.Generate());
    }

}
