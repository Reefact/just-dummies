// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Money --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Money" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMoney : IAny<Money> {

    private IAny<decimal> _amount;

    private AnyDecimal _amountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMoney()
        : this(amount: AnyValidAmount()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMoney(AnyDecimal amount) {
        _amount       = amount;
        _amountRecipe = amount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMoney(AnyMoney from) {
        _amount       = from._amount;
        _amountRecipe = from._amountRecipe;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMoney WithAmount(decimal value) {
        AnyMoney copy = new AnyMoney(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMoney WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        AnyMoney copy = new AnyMoney(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Money" />.</summary>
    public Money Generate() {
        return new Money(_amount.Generate());
    }

}
