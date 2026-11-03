// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RowInstruction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RowInstruction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRowInstruction : IAny<RowInstruction> {

    private IAny<int>                        _rowNumber;
    private IAny<FabricSide>                 _side;
    private IAny<IReadOnlyList<IRowElement>> _elements;
    private IAny<bool>                       _printsCount = Any.Boolean();

    private AnyInt32            _rowNumberRecipe;
    private AnyEnum<FabricSide> _sideRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRowInstruction()
        : this(rowNumber:           AnyValidRowNumber(),
               side:                AnyValidSide(),
               elements:            AnyValidElements()) { }

    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<FabricSide> AnyValidSide() {
        return Any.Enum<FabricSide>();
    }

    private static IAny<IReadOnlyList<IRowElement>> AnyValidElements() {
        return Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRowInstruction(AnyInt32                         rowNumber,
                              AnyEnum<FabricSide>              side,
                              IAny<IReadOnlyList<IRowElement>> elements) {
        _rowNumber                 = rowNumber;
        _rowNumberRecipe           = rowNumber;
        _side                      = side;
        _sideRecipe                = side;
        _elements                  = elements;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRowInstruction(AnyRowInstruction from) {
        _rowNumber                 = from._rowNumber;
        _rowNumberRecipe           = from._rowNumberRecipe;
        _side                      = from._side;
        _sideRecipe                = from._sideRecipe;
        _elements                  = from._elements;
        _printsCount               = from._printsCount;
    }

    /// <summary>Pins <c>rowNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRowInstruction WithRowNumber(int value) {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._rowNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rowNumber</c> is drawn from — <c>AnyValidRowNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRowInstruction WithRowNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._rowNumberRecipe = configure(_rowNumberRecipe);
        copy._rowNumber       = copy._rowNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>side</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRowInstruction WithSide(FabricSide value) {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._side = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>side</c> is drawn from — <c>AnyValidSide()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;FabricSide&gt;, AnyEnum&lt;FabricSide&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRowInstruction WithSide(Func<AnyEnum<FabricSide>, AnyEnum<FabricSide>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._sideRecipe = configure(_sideRecipe);
        copy._side       = copy._sideRecipe;

        return copy;
    }

    /// <summary>Pins <c>elements</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type.</summary>
    public AnyRowInstruction WithElements(IReadOnlyList<IRowElement> value) {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._elements = Any.Fixed(value);

        return copy;
    }

    /// <summary>The row prints its stitch count at the end, as the elements produce it.</summary>
    public AnyRowInstruction WithPrintedStitchCount() {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._printsCount = Any.Fixed(true);

        return copy;
    }

    /// <summary>The row prints no stitch count.</summary>
    public AnyRowInstruction WithoutPrintedStitchCount() {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._printsCount = Any.Fixed(false);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RowInstruction" />.</summary>
    public RowInstruction Generate() {
        int                        rowNumber = _rowNumber.Generate();
        FabricSide                 side      = _side.Generate();
        IReadOnlyList<IRowElement> elements  = _elements.Generate();
        int?                       printed   = _printsCount.Generate() ? elements.Sum(e => e.Produces) : null;

        return new RowInstruction(rowNumber, side, elements, printed);
    }

}
