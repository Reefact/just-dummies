// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BomLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BomLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBomLine : IAny<BomLine> {

    private IAny<Part>                             _part;
    private bool                                   _designatorsPinned;
    private IAny<IEnumerable<ReferenceDesignator>> _designators;

    private AnyPart                          _partRecipe;
    private AnySequence<ReferenceDesignator> _designatorsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBomLine()
        : this(part:        AnyValidPart(),
               designators: AnyValidDesignators()) { }

    private static AnyPart AnyValidPart() {
        return new AnyPart();
    }

    private static AnySequence<ReferenceDesignator> AnyValidDesignators() {
        return Any.SequenceOf(new AnyReferenceDesignator());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBomLine(AnyPart                          part,
                       AnySequence<ReferenceDesignator> designators) {
        _part              = part;
        _partRecipe        = part;
        _designators       = designators;
        _designatorsRecipe = designators;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBomLine(AnyBomLine from) {
        _part              = from._part;
        _partRecipe        = from._partRecipe;
        _designators       = from._designators;
        _designatorsRecipe = from._designatorsRecipe;
        _designatorsPinned = from._designatorsPinned;
    }

    /// <summary>Pins <c>part</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBomLine WithPart(Part value) {
        AnyBomLine copy = new AnyBomLine(this);

        copy._part = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>part</c> is drawn from — <c>AnyValidPart()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPart, AnyPart&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBomLine WithPart(Func<AnyPart, AnyPart> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBomLine copy = new AnyBomLine(this);

        copy._partRecipe = configure(_partRecipe);
        copy._part       = copy._partRecipe;

        return copy;
    }

    /// <summary>Pins <c>designators</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBomLine WithDesignators(IEnumerable<ReferenceDesignator> value) {
        AnyBomLine copy = new AnyBomLine(this);

        copy._designators       = Any.Fixed(value);
        copy._designatorsPinned = true;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BomLine" />.</summary>
    public BomLine Generate() {
        // The part is drawn first: every designator must be of its kind, and a line lists each of them once.
        Part                             part        = _part.Generate();
        IEnumerable<ReferenceDesignator> designators = _designatorsPinned
            ? _designators.Generate()
            : Any.SequenceOf(new AnyReferenceDesignator().OfKind(part.Kind)).WithCountBetween(1, 6).Distinct().Generate();

        return new BomLine(part, designators);
    }

}
