// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LotQuantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LotQuantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLotQuantity : IAny<LotQuantity> {

    private IAny<LotNumber> _lot;
    private IAny<Quantity>  _quantity;

    private AnyLotNumber _lotRecipe;
    private AnyQuantity  _quantityRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLotQuantity()
        : this(lot:      AnyValidLot(),
               quantity: AnyValidQuantity()) { }

    private static AnyLotNumber AnyValidLot() {
        return new AnyLotNumber();
    }

    private static AnyQuantity AnyValidQuantity() {
        return new AnyQuantity().WithAmount(amount => amount.GreaterThan(0m));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyLotQuantity(AnyLotNumber lot,
                           AnyQuantity  quantity) {
        _lot            = lot;
        _lotRecipe      = lot;
        _quantity       = quantity;
        _quantityRecipe = quantity;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyLotQuantity(AnyLotQuantity from) {
        _lot            = from._lot;
        _lotRecipe      = from._lotRecipe;
        _quantity       = from._quantity;
        _quantityRecipe = from._quantityRecipe;
    }

    /// <summary>Pins <c>lot</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLotQuantity WithLot(LotNumber value) {
        AnyLotQuantity copy = new AnyLotQuantity(this);

        copy._lot = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lot</c> is drawn from — <c>AnyValidLot()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyLotNumber, AnyLotNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLotQuantity WithLot(Func<AnyLotNumber, AnyLotNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLotQuantity copy = new AnyLotQuantity(this);

        copy._lotRecipe = configure(_lotRecipe);
        copy._lot       = copy._lotRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyLotQuantity WithQuantity(Quantity value) {
        AnyLotQuantity copy = new AnyLotQuantity(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyLotQuantity WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyLotQuantity copy = new AnyLotQuantity(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="LotQuantity" />.</summary>
    public LotQuantity Generate() {
        return new LotQuantity(_lot.Generate(),
                               _quantity.Generate());
    }

}
