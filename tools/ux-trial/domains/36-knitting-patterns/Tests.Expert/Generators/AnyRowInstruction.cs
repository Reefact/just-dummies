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
using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="RowInstruction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRowInstruction : IAny<RowInstruction> {

    private IAny<int>                        _rowNumber;
    private IAny<FabricSide>                 _side;
    private IAny<IReadOnlyList<IRowElement>> _elements;
    private IAny<int?>?                      _expectedStitchCount;   // null: derived from the drawn elements
    private bool                             _printsProducedCount;

    private AnyInt32            _rowNumberRecipe;
    private AnyEnum<FabricSide> _sideRecipe;
    private AnyInt32            _expectedStitchCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRowInstruction()
        : this(rowNumber:           AnyValidRowNumber(),
               side:                AnyValidSide(),
               elements:            AnyValidElements(),
               expectedStitchCount: AnyValidExpectedStitchCount()) { }

    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<FabricSide> AnyValidSide() {
        return Any.Enum<FabricSide>();
    }

    private static IAny<IReadOnlyList<IRowElement>> AnyValidElements() {
        return Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 4);
    }

    private static AnyInt32 AnyValidExpectedStitchCount() {
        // Not a base of its own: a printed count must equal what the elements produce, so it is derived in
        // Generate() from the drawn elements. This recipe only keeps the scaffold's shape.
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRowInstruction(AnyInt32                         rowNumber,
                              AnyEnum<FabricSide>              side,
                              IAny<IReadOnlyList<IRowElement>> elements,
                              AnyInt32                         expectedStitchCount) {
        _rowNumber                 = rowNumber;
        _rowNumberRecipe           = rowNumber;
        _side                      = side;
        _sideRecipe                = side;
        _elements                  = elements;
        _expectedStitchCount       = null;
        _expectedStitchCountRecipe = expectedStitchCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRowInstruction(AnyRowInstruction from) {
        _rowNumber                 = from._rowNumber;
        _rowNumberRecipe           = from._rowNumberRecipe;
        _side                      = from._side;
        _sideRecipe                = from._sideRecipe;
        _elements                  = from._elements;
        _expectedStitchCount       = from._expectedStitchCount;
        _printsProducedCount       = from._printsProducedCount;
        _expectedStitchCountRecipe = from._expectedStitchCountRecipe;
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

    /// <summary>Pins <c>expectedStitchCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRowInstruction WithExpectedStitchCount(int? value) {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._expectedStitchCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>The row prints the count its elements really produce.</summary>
    public AnyRowInstruction WithCorrectlyPrintedStitchCount() {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._printsProducedCount = true;

        return copy;
    }

    /// <summary>Pins <c>expectedStitchCount</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyRowInstruction WithoutExpectedStitchCount() {
        AnyRowInstruction copy = new AnyRowInstruction(this);

        copy._expectedStitchCount = Any.Fixed<int?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RowInstruction" />.</summary>
    public RowInstruction Generate() {
        IReadOnlyList<IRowElement> elements = _elements.Generate();
        int                        produced = elements.Sum(e => e.Produces);

        // A printed count is tied to the elements, so it is derived from them: printed correctly or absent,
        // one draw in two. Pin it (WithExpectedStitchCount) to exercise a wrong one.
        int? expected = _expectedStitchCount is not null           ? _expectedStitchCount.Generate()
                      : _printsProducedCount || Any.Boolean().Generate() ? produced
                                                                         : null;

        return new RowInstruction(_rowNumber.Generate(),
                                  _side.Generate(),
                                  elements,
                                  expected);
    }

}
