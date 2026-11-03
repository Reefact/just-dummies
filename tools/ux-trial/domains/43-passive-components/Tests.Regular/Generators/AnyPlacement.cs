// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Placement --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Placement" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlacement : IAny<Placement> {

    private IAny<ReferenceDesignator> _designator;
    private IAny<Part>                _part;
    private bool                      _designatorPinned;

    private AnyReferenceDesignator _designatorRecipe;
    private AnyPart                _partRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlacement()
        : this(designator: AnyValidDesignator(),
               part:       AnyValidPart()) { }

    private static AnyReferenceDesignator AnyValidDesignator() {
        return new AnyReferenceDesignator();
    }

    private static AnyPart AnyValidPart() {
        return new AnyPart();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPlacement(AnyReferenceDesignator designator,
                         AnyPart                part) {
        _designator       = designator;
        _designatorRecipe = designator;
        _part             = part;
        _partRecipe       = part;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPlacement(AnyPlacement from) {
        _designator       = from._designator;
        _designatorRecipe = from._designatorRecipe;
        _designatorPinned = from._designatorPinned;
        _part             = from._part;
        _partRecipe       = from._partRecipe;
    }

    /// <summary>Pins <c>designator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlacement WithDesignator(ReferenceDesignator value) {
        AnyPlacement copy = new AnyPlacement(this);

        copy._designator       = Any.Fixed(value);
        copy._designatorPinned = true;

        return copy;
    }

    /// <summary>Pins <c>part</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPlacement WithPart(Part value) {
        AnyPlacement copy = new AnyPlacement(this);

        copy._part = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>part</c> is drawn from — <c>AnyValidPart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPart, AnyPart&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPlacement WithPart(Func<AnyPart, AnyPart> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPlacement copy = new AnyPlacement(this);

        copy._partRecipe = configure(_partRecipe);
        copy._part       = copy._partRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Placement" />.</summary>
    public Placement Generate() {
        // The part is drawn first: the designator's letter must name its kind.
        Part                part       = _part.Generate();
        ReferenceDesignator designator = _designatorPinned ? _designator.Generate() : new AnyReferenceDesignator().OfKind(part.Kind).Generate();

        return new Placement(designator, part);
    }

}
