// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quote --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quote" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuote : IAny<Quote> {

    private IAny<decimal>  _amount;
    private IAny<decimal?> _discount;

    private AnyDecimal _amountRecipe;
    private AnyDecimal _discountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuote()
        : this(amount:   AnyValidAmount(),
               discount: AnyValidDiscount()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Positive();
    }

    private static AnyDecimal AnyValidDiscount() {
        return Any.Decimal();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyQuote(AnyDecimal amount,
                     AnyDecimal discount) {
        _amount         = amount;
        _amountRecipe   = amount;
        _discount       = discount.OrNull();
        _discountRecipe = discount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyQuote(AnyQuote from) {
        _amount         = from._amount;
        _amountRecipe   = from._amountRecipe;
        _discount       = from._discount;
        _discountRecipe = from._discountRecipe;
    }

    /// <summary>Pins <c>amount</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyQuote WithAmount(decimal value) {
        AnyQuote copy = new AnyQuote(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyQuote WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        AnyQuote copy = new AnyQuote(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>discount</c> to a fixed value; its recipe is kept for a later refinement.</summary>
    public AnyQuote WithDiscount(decimal? value) {
        AnyQuote copy = new AnyQuote(this);

        copy._discount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>discount</c> is drawn from — <c>AnyValidDiscount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyQuote WithDiscount(Func<AnyDecimal, AnyDecimal> configure) {
        AnyQuote copy = new AnyQuote(this);

        copy._discountRecipe = configure(_discountRecipe);
        copy._discount       = copy._discountRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>discount</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyQuote WithoutDiscount() {
        AnyQuote copy = new AnyQuote(this);

        copy._discount = Any.Fixed<decimal?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Quote" />.</summary>
    public Quote Generate() {
        return new Quote(_amount.Generate(),
                         _discount.Generate());
    }

}
