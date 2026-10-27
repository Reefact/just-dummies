// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BomItem --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BomItem" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBomItem : IAny<BomItem> {

    private readonly IAny<PartNumber>             _part;
    private readonly IAny<string>                 _description;
    private readonly IAny<int>                    _quantity;
    private readonly IAny<IReadOnlyList<BomItem>> _children;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBomItem()
        : this(part:        AnyValidPart(),
               description: AnyValidDescription(),
               quantity:    AnyValidQuantity(),
               children:    AnyValidChildren()) { }

    private static AnyPartNumber AnyValidPart() {
        return new AnyPartNumber();
    }

    private static AnyString AnyValidDescription() {
        return Any.String().NotBlank().WithMaxLength(80);
    }

    private static AnyInt32 AnyValidQuantity() {
        return Any.Int32().Positive().LessThanOrEqualTo(9999);
    }

    private static AnyList<BomItem> AnyValidChildren() {
        // As emitted, `Any.ListOf(new AnyBomItem())` recursed in this very constructor: a stack overflow
        // that took the whole test host down. A BOM is a tree, so the base is one level of leaves;
        // WithDepth (AnyBomItem.Custom.cs) builds deeper ones.
        return Any.ListOf(Leaf()).WithMaxCount(5);
    }

    private AnyBomItem(IAny<PartNumber>             part,
                       IAny<string>                 description,
                       IAny<int>                    quantity,
                       IAny<IReadOnlyList<BomItem>> children) {
        _part        = part;
        _description = description;
        _quantity    = quantity;
        _children    = children;
    }

    /// <summary>Pins <c>part</c> to a fixed value.</summary>
    public AnyBomItem WithPart(PartNumber value) {
        return new AnyBomItem(new FixedValue<PartNumber>(value), _description, _quantity, _children);
    }

    /// <summary>Draws <c>part</c> from <c>AnyValidPart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBomItem WithPart(Func<AnyPartNumber, AnyPartNumber> configure) {
        return new AnyBomItem(configure(AnyValidPart()), _description, _quantity, _children);
    }

    /// <summary>Pins <c>description</c> to a fixed value.</summary>
    public AnyBomItem WithDescription(string value) {
        return new AnyBomItem(_part, new FixedValue<string>(value), _quantity, _children);
    }

    /// <summary>Draws <c>description</c> from <c>AnyValidDescription()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBomItem WithDescription(Func<AnyString, AnyString> configure) {
        return new AnyBomItem(_part, configure(AnyValidDescription()), _quantity, _children);
    }

    /// <summary>Pins <c>quantity</c> to a fixed value.</summary>
    public AnyBomItem WithQuantity(int value) {
        return new AnyBomItem(_part, _description, new FixedValue<int>(value), _children);
    }

    /// <summary>Draws <c>quantity</c> from <c>AnyValidQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBomItem WithQuantity(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBomItem(_part, _description, configure(AnyValidQuantity()), _children);
    }

    /// <summary>Pins <c>children</c> to a fixed value.</summary>
    public AnyBomItem WithChildren(IReadOnlyList<BomItem> value) {
        return new AnyBomItem(_part, _description, _quantity, new FixedValue<IReadOnlyList<BomItem>>(value));
    }

    /// <summary>Draws <c>children</c> from <c>AnyValidChildren()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBomItem WithChildren(Func<AnyList<BomItem>, AnyList<BomItem>> configure) {
        return new AnyBomItem(_part, _description, _quantity, configure(AnyValidChildren()));
    }

    /// <summary>Produces one arbitrary <see cref="BomItem" />.</summary>
    public BomItem Generate() {
        return new BomItem(_part.Generate(),
                           _description.Generate(),
                           _quantity.Generate(),
                           _children.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
