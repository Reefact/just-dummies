// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuantity : IAny<Quantity> {

    private IAny<decimal>      _amount;
    private IAny<QuantityUnit> _unit;

    private AnyDecimal            _amountRecipe;
    private AnyEnum<QuantityUnit> _unitRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuantity()
        : this(amount: AnyValidAmount(),
               unit:   AnyValidUnit()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Between(0m, Quantity.MaximumAmount);
    }

    private static AnyEnum<QuantityUnit> AnyValidUnit() {
        return Any.Enum<QuantityUnit>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyQuantity(AnyDecimal            amount,
                        AnyEnum<QuantityUnit> unit) {
        _amount       = amount;
        _amountRecipe = amount;
        _unit         = unit;
        _unitRecipe   = unit;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyQuantity(AnyQuantity from) {
        _amount       = from._amount;
        _amountRecipe = from._amountRecipe;
        _unit         = from._unit;
        _unitRecipe   = from._unitRecipe;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuantity WithAmount(decimal value) {
        AnyQuantity copy = new AnyQuantity(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuantity WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuantity copy = new AnyQuantity(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>unit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyQuantity WithUnit(QuantityUnit value) {
        AnyQuantity copy = new AnyQuantity(this);

        copy._unit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unit</c> is drawn from — <c>AnyValidUnit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;QuantityUnit&gt;, AnyEnum&lt;QuantityUnit&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyQuantity WithUnit(Func<AnyEnum<QuantityUnit>, AnyEnum<QuantityUnit>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyQuantity copy = new AnyQuantity(this);

        copy._unitRecipe = configure(_unitRecipe);
        copy._unit       = copy._unitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Quantity" />.</summary>
    public Quantity Generate() {
        return new Quantity(_amount.Generate(),
                            _unit.Generate());
    }

}
