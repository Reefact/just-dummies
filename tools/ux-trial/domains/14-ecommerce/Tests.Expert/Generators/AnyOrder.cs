// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Order --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Order" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOrder : IAny<Order> {

    private IAny<Guid>                     _id;
    private IAny<IReadOnlyList<OrderLine>> _lines;
    private IAny<ShippingAddress>          _shippingAddress;
    private IAny<DiscountPercentage>       _discount;
    private IAny<ShippingMethod>           _shippingMethod;

    private AnyGuid                 _idRecipe;
    private AnyShippingAddress      _shippingAddressRecipe;
    private AnyDiscountPercentage   _discountRecipe;
    private AnyEnum<ShippingMethod> _shippingMethodRecipe;
    private AnyDateTime             _placedAtRecipe;
    private AnyDateTime             _deliveryDeadlineRecipe;

    private DateTime? _placedAtPin;
    private DateTime? _deliveryDeadlinePin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOrder()
        : this(id:               AnyValidId(),
               lines:            AnyValidLines(),
               shippingAddress:  AnyValidShippingAddress(),
               discount:         AnyValidDiscount(),
               shippingMethod:   AnyValidShippingMethod(),
               placedAt:         AnyValidPlacedAt(),
               deliveryDeadline: AnyValidDeliveryDeadline()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static IAny<IReadOnlyList<OrderLine>> AnyValidLines() {
        // An order's lines share one currency: draw the currency once, then every line in it.
        return Any.String().OneOf("EUR", "USD", "GBP")
                  .Then(currency => Any.ListOf(new AnyOrderLine().WithUnitPrice(price => price.WithCurrency(currency)))
                                       .WithCountBetween(1, 5)
                                       .As(lines => (IReadOnlyList<OrderLine>)lines));
    }

    private static AnyShippingAddress AnyValidShippingAddress() {
        return new AnyShippingAddress();
    }

    private static AnyDiscountPercentage AnyValidDiscount() {
        return new AnyDiscountPercentage();
    }

    private static AnyEnum<ShippingMethod> AnyValidShippingMethod() {
        return Any.Enum<ShippingMethod>();
    }

    private static AnyDateTime AnyValidPlacedAt() {
        return Any.DateTime();
    }

    private static AnyDateTime AnyValidDeliveryDeadline() {
        return Any.DateTime();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyOrder(AnyGuid                 id,
                     IAny<IReadOnlyList<OrderLine>> lines,
                     AnyShippingAddress      shippingAddress,
                     AnyDiscountPercentage   discount,
                     AnyEnum<ShippingMethod> shippingMethod,
                     AnyDateTime             placedAt,
                     AnyDateTime             deliveryDeadline) {
        _id                          = id;
        _idRecipe                    = id;
        _lines                       = lines;
        _shippingAddress             = shippingAddress;
        _shippingAddressRecipe       = shippingAddress;
        _discount                    = discount;
        _discountRecipe              = discount;
        _shippingMethod              = shippingMethod;
        _shippingMethodRecipe        = shippingMethod;
        _placedAtRecipe              = placedAt;
        _deliveryDeadlineRecipe      = deliveryDeadline;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyOrder(AnyOrder from) {
        _id                          = from._id;
        _idRecipe                    = from._idRecipe;
        _lines                       = from._lines;
        _shippingAddress             = from._shippingAddress;
        _shippingAddressRecipe       = from._shippingAddressRecipe;
        _discount                    = from._discount;
        _discountRecipe              = from._discountRecipe;
        _shippingMethod              = from._shippingMethod;
        _shippingMethodRecipe        = from._shippingMethodRecipe;
        _placedAtRecipe              = from._placedAtRecipe;
        _placedAtPin                 = from._placedAtPin;
        _deliveryDeadlineRecipe      = from._deliveryDeadlineRecipe;
        _deliveryDeadlinePin         = from._deliveryDeadlinePin;
    }

    /// <summary>Draws <c>placedAt</c> and <c>deliveryDeadline</c>, <c>placedAt</c> always before <c>deliveryDeadline</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (DateTime PlacedAt, DateTime DeliveryDeadline) PlacedAtAndDeliveryDeadline() {
        if (_placedAtPin is { } placedAt) { return (placedAt, _deliveryDeadlinePin ?? _deliveryDeadlineRecipe.After(placedAt).Generate()); }
        if (_deliveryDeadlinePin is { } deliveryDeadline) { return (_placedAtRecipe.Before(deliveryDeadline).Generate(), deliveryDeadline); }

        return Any.Pair(_placedAtRecipe, _deliveryDeadlineRecipe).Having.Item1BeforeItem2().Generate();
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithId(Guid value) {
        AnyOrder copy = new AnyOrder(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>lines</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithLines(IReadOnlyList<OrderLine> value) {
        AnyOrder copy = new AnyOrder(this);

        copy._lines = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>shippingAddress</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithShippingAddress(ShippingAddress value) {
        AnyOrder copy = new AnyOrder(this);

        copy._shippingAddress = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shippingAddress</c> is drawn from — <c>AnyValidShippingAddress()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyShippingAddress, AnyShippingAddress&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithShippingAddress(Func<AnyShippingAddress, AnyShippingAddress> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._shippingAddressRecipe = configure(_shippingAddressRecipe);
        copy._shippingAddress       = copy._shippingAddressRecipe;

        return copy;
    }

    /// <summary>Pins <c>discount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithDiscount(DiscountPercentage value) {
        AnyOrder copy = new AnyOrder(this);

        copy._discount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>discount</c> is drawn from — <c>AnyValidDiscount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDiscountPercentage, AnyDiscountPercentage&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithDiscount(Func<AnyDiscountPercentage, AnyDiscountPercentage> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._discountRecipe = configure(_discountRecipe);
        copy._discount       = copy._discountRecipe;

        return copy;
    }

    /// <summary>Pins <c>shippingMethod</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithShippingMethod(ShippingMethod value) {
        AnyOrder copy = new AnyOrder(this);

        copy._shippingMethod = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>shippingMethod</c> is drawn from — <c>AnyValidShippingMethod()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ShippingMethod&gt;, AnyEnum&lt;ShippingMethod&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithShippingMethod(Func<AnyEnum<ShippingMethod>, AnyEnum<ShippingMethod>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._shippingMethodRecipe = configure(_shippingMethodRecipe);
        copy._shippingMethod       = copy._shippingMethodRecipe;

        return copy;
    }

    /// <summary>Pins <c>placedAt</c> to a fixed value, which bounds <c>deliveryDeadline</c>; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithPlacedAt(DateTime value) {
        AnyOrder copy = new AnyOrder(this);

        copy._placedAtPin                 = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>placedAt</c> is drawn from — <c>AnyValidPlacedAt()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>deliveryDeadline</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithPlacedAt(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._placedAtRecipe              = configure(_placedAtRecipe);
        copy._placedAtPin                 = null;

        return copy;
    }

    /// <summary>Pins <c>deliveryDeadline</c> to a fixed value, which bounds <c>placedAt</c>; its recipe is kept for a later refinement.</summary>
    public AnyOrder WithDeliveryDeadline(DateTime value) {
        AnyOrder copy = new AnyOrder(this);

        copy._deliveryDeadlinePin         = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>deliveryDeadline</c> is drawn from — <c>AnyValidDeliveryDeadline()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>placedAt</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyOrder WithDeliveryDeadline(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyOrder copy = new AnyOrder(this);

        copy._deliveryDeadlineRecipe      = configure(_deliveryDeadlineRecipe);
        copy._deliveryDeadlinePin         = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Order" />.</summary>
    public Order Generate() {
        (DateTime placedAt, DateTime deliveryDeadline) = PlacedAtAndDeliveryDeadline();

        return new Order(_id.Generate(),
                         _lines.Generate(),
                         _shippingAddress.Generate(),
                         _discount.Generate(),
                         _shippingMethod.Generate(),
                         placedAt,
                         deliveryDeadline);
    }

}
