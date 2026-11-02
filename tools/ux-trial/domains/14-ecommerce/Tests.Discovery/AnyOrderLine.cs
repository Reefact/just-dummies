// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OrderLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OrderLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrderLine : IAny<OrderLine> {

    private IAny<Sku>      _sku;
    private IAny<Quantity> _quantity;
    private IAny<Money>    _unitPrice;

    private AnySku      _skuRecipe;
    private AnyQuantity _quantityRecipe;
    private AnyMoney    _unitPriceRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrderLine()
        : this(sku:       AnyValidSku(),
               quantity:  AnyValidQuantity(),
               unitPrice: AnyValidUnitPrice()) { }

    private static AnySku AnyValidSku() {
        return new AnySku();
    }

    private static AnyQuantity AnyValidQuantity() {
        return new AnyQuantity();
    }

    private static AnyMoney AnyValidUnitPrice() {
        return new AnyMoney();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrderLine(AnySku      sku,
                         AnyQuantity quantity,
                         AnyMoney    unitPrice) {
        _sku             = sku;
        _skuRecipe       = sku;
        _quantity        = quantity;
        _quantityRecipe  = quantity;
        _unitPrice       = unitPrice;
        _unitPriceRecipe = unitPrice;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrderLine(AnyOrderLine from) {
        _sku             = from._sku;
        _skuRecipe       = from._skuRecipe;
        _quantity        = from._quantity;
        _quantityRecipe  = from._quantityRecipe;
        _unitPrice       = from._unitPrice;
        _unitPriceRecipe = from._unitPriceRecipe;
    }

    /// <summary>Pins <c>sku</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrderLine WithSku(Sku value) {
        AnyOrderLine copy = new AnyOrderLine(this);

        copy._sku = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sku</c> is drawn from — <c>AnyValidSku()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySku, AnySku&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrderLine WithSku(Func<AnySku, AnySku> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrderLine copy = new AnyOrderLine(this);

        copy._skuRecipe = configure(_skuRecipe);
        copy._sku       = copy._skuRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrderLine WithQuantity(Quantity value) {
        AnyOrderLine copy = new AnyOrderLine(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyQuantity, AnyQuantity&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrderLine WithQuantity(Func<AnyQuantity, AnyQuantity> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrderLine copy = new AnyOrderLine(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>unitPrice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrderLine WithUnitPrice(Money value) {
        AnyOrderLine copy = new AnyOrderLine(this);

        copy._unitPrice = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitPrice</c> is drawn from — <c>AnyValidUnitPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrderLine WithUnitPrice(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrderLine copy = new AnyOrderLine(this);

        copy._unitPriceRecipe = configure(_unitPriceRecipe);
        copy._unitPrice       = copy._unitPriceRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="OrderLine" />.</summary>
    public OrderLine Generate() {
        return new OrderLine(_sku.Generate(),
                             _quantity.Generate(),
                             _unitPrice.Generate());
    }

}
