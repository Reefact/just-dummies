// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExpandedRow --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="ExpandedRow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExpandedRow : IAny<ExpandedRow> {

    private IAny<string>                    _sectionName;
    private IAny<int>                       _rowNumber;
    private IAny<FabricSide>                _side;
    private IAny<IReadOnlyList<StitchKind>> _operations;

    private AnyString           _sectionNameRecipe;
    private AnyInt32            _rowNumberRecipe;
    private AnyEnum<FabricSide> _sideRecipe;
    private AnyList<StitchKind> _operationsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExpandedRow()
        : this(sectionName:    AnyValidSectionName(),
               rowNumber:      AnyValidRowNumber(),
               side:           AnyValidSide(),
               operations:     AnyValidOperations()) { }

    private static AnyString AnyValidSectionName() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<FabricSide> AnyValidSide() {
        return Any.Enum<FabricSide>();
    }

    private static AnyList<StitchKind> AnyValidOperations() {
        // stitchesBefore is derived from the operations and a row starts with at least one stitch: the list
        // holds at least one stitch that consumes something, so a row of yarn overs alone is never drawn.
        return Any.ListOf(Any.Enum<StitchKind>())
                  .WithCountBetween(1, 30)
                  .ContainingAny(Any.Enum<StitchKind>().DifferentFrom(StitchKind.YarnOver));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExpandedRow(AnyString           sectionName,
                           AnyInt32            rowNumber,
                           AnyEnum<FabricSide> side,
                           AnyList<StitchKind> operations) {
        _sectionName          = sectionName;
        _sectionNameRecipe    = sectionName;
        _rowNumber            = rowNumber;
        _rowNumberRecipe      = rowNumber;
        _side                 = side;
        _sideRecipe           = side;
        _operations           = operations;
        _operationsRecipe     = operations;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExpandedRow(AnyExpandedRow from) {
        _sectionName          = from._sectionName;
        _sectionNameRecipe    = from._sectionNameRecipe;
        _rowNumber            = from._rowNumber;
        _rowNumberRecipe      = from._rowNumberRecipe;
        _side                 = from._side;
        _sideRecipe           = from._sideRecipe;
        _operations           = from._operations;
        _operationsRecipe     = from._operationsRecipe;
    }

    /// <summary>Pins <c>sectionName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExpandedRow WithSectionName(string value) {
        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._sectionName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sectionName</c> is drawn from — <c>AnyValidSectionName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExpandedRow WithSectionName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._sectionNameRecipe = configure(_sectionNameRecipe);
        copy._sectionName       = copy._sectionNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>rowNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExpandedRow WithRowNumber(int value) {
        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._rowNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rowNumber</c> is drawn from — <c>AnyValidRowNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExpandedRow WithRowNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._rowNumberRecipe = configure(_rowNumberRecipe);
        copy._rowNumber       = copy._rowNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>side</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExpandedRow WithSide(FabricSide value) {
        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._side = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>side</c> is drawn from — <c>AnyValidSide()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;FabricSide&gt;, AnyEnum&lt;FabricSide&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExpandedRow WithSide(Func<AnyEnum<FabricSide>, AnyEnum<FabricSide>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._sideRecipe = configure(_sideRecipe);
        copy._side       = copy._sideRecipe;

        return copy;
    }

    /// <summary>Pins <c>operations</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExpandedRow WithOperations(IReadOnlyList<StitchKind> value) {
        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._operations = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>operations</c> is drawn from — <c>AnyValidOperations()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;StitchKind&gt;, AnyList&lt;StitchKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExpandedRow WithOperations(Func<AnyList<StitchKind>, AnyList<StitchKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExpandedRow copy = new AnyExpandedRow(this);

        copy._operationsRecipe = configure(_operationsRecipe);
        copy._operations       = copy._operationsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ExpandedRow" />.</summary>
    public ExpandedRow Generate() {
        IReadOnlyList<StitchKind> operations = _operations.Generate();

        // The stitches the row starts with are the ones its operations consume: derived, not drawn.
        int stitchesBefore = operations.Sum(o => o.Consumes());

        return new ExpandedRow(_sectionName.Generate(),
                               _rowNumber.Generate(),
                               _side.Generate(),
                               stitchesBefore,
                               operations);
    }

}
