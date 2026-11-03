// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BomItem --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<PartNumber>             _part;
    private IAny<string>                 _description;
    private IAny<int>                    _quantity;
    private IAny<IReadOnlyList<BomItem>> _children;

    private AnyPartNumber _partRecipe;
    private AnyString     _descriptionRecipe;
    private AnyInt32      _quantityRecipe;

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
        return Any.String().NotBlank().WithMaxLength(BomItem.MaximumDescriptionLength);
    }

    private static AnyInt32 AnyValidQuantity() {
        return Any.Int32().Positive().LessThanOrEqualTo(BomItem.MaximumQuantity);
    }

    private static IAny<IReadOnlyList<BomItem>> AnyValidChildren() {
        // A leaf by default: a recursive draw needs a depth bound and distinct part numbers down the tree,
        // which BomTrees (hand-written, beside this file) builds. Tests that want a structure pin WithChildren(...).
        return Any.Fixed<IReadOnlyList<BomItem>>(Array.Empty<BomItem>());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBomItem(AnyPartNumber                part,
                       AnyString                    description,
                       AnyInt32                     quantity,
                       IAny<IReadOnlyList<BomItem>> children) {
        _part              = part;
        _partRecipe        = part;
        _description       = description;
        _descriptionRecipe = description;
        _quantity          = quantity;
        _quantityRecipe    = quantity;
        _children          = children;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBomItem(AnyBomItem from) {
        _part              = from._part;
        _partRecipe        = from._partRecipe;
        _description       = from._description;
        _descriptionRecipe = from._descriptionRecipe;
        _quantity          = from._quantity;
        _quantityRecipe    = from._quantityRecipe;
        _children          = from._children;
    }

    /// <summary>Pins <c>part</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBomItem WithPart(PartNumber value) {
        AnyBomItem copy = new AnyBomItem(this);

        copy._part = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>part</c> is drawn from — <c>AnyValidPart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPartNumber, AnyPartNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBomItem WithPart(Func<AnyPartNumber, AnyPartNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBomItem copy = new AnyBomItem(this);

        copy._partRecipe = configure(_partRecipe);
        copy._part       = copy._partRecipe;

        return copy;
    }

    /// <summary>Pins <c>description</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBomItem WithDescription(string value) {
        AnyBomItem copy = new AnyBomItem(this);

        copy._description = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>description</c> is drawn from — <c>AnyValidDescription()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBomItem WithDescription(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBomItem copy = new AnyBomItem(this);

        copy._descriptionRecipe = configure(_descriptionRecipe);
        copy._description       = copy._descriptionRecipe;

        return copy;
    }

    /// <summary>Pins <c>quantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBomItem WithQuantity(int value) {
        AnyBomItem copy = new AnyBomItem(this);

        copy._quantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>quantity</c> is drawn from — <c>AnyValidQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBomItem WithQuantity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBomItem copy = new AnyBomItem(this);

        copy._quantityRecipe = configure(_quantityRecipe);
        copy._quantity       = copy._quantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>children</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyBomItem WithChildren(IReadOnlyList<BomItem> value) {
        AnyBomItem copy = new AnyBomItem(this);

        copy._children = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BomItem" />.</summary>
    public BomItem Generate() {
        return new BomItem(_part.Generate(),
                           _description.Generate(),
                           _quantity.Generate(),
                           _children.Generate());
    }

}
