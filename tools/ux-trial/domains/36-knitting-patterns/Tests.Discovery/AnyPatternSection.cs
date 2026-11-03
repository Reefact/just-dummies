// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PatternSection --force` overwrites it. This type is partial, so members you add in a
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
///     A generator of arbitrary <see cref="PatternSection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPatternSection : IAny<PatternSection> {

    private IAny<string>                        _name;
    private IAny<IReadOnlyList<RowInstruction>> _rows;

    private AnyString               _nameRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPatternSection()
        : this(name: AnyValidName(),
               rows: AnyValidRows()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength);
    }

    private static IAny<IReadOnlyList<RowInstruction>> AnyValidRows() {
        // Rows are consecutive and alternate sides: draw where the run starts and how long it is, then number and side every row from that.
        return Any.Combine(Any.Int32().Between(1, 1_000), Any.Enum<FabricSide>(), Any.Int32().Between(1, 6), (first, firstSide, count) => (first, firstSide, count))
                  .Then(run => RowsFrom(run.first, run.firstSide, run.count));
    }

    private static IAny<IReadOnlyList<RowInstruction>> RowsFrom(int first, FabricSide firstSide, int count) {
        IAny<IReadOnlyList<RowInstruction>> rows = Any.Fixed<IReadOnlyList<RowInstruction>>(Array.Empty<RowInstruction>());
        for (int i = 0; i < count; i++) {
            IAny<IReadOnlyList<RowInstruction>> before = rows;
            IAny<RowInstruction> row = new AnyRowInstruction().WithRowNumber(first + i)
                                                              .WithSide(i % 2 == 0 ? firstSide : Opposite(firstSide));

            rows = Any.Combine(before, row, (done, next) => (IReadOnlyList<RowInstruction>)done.Append(next).ToList());
        }

        return rows;
    }

    private static FabricSide Opposite(FabricSide side) {
        return side == FabricSide.RightSide ? FabricSide.WrongSide : FabricSide.RightSide;
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPatternSection(AnyString                        name,
                              IAny<IReadOnlyList<RowInstruction>> rows) {
        _name       = name;
        _nameRecipe = name;
        _rows       = rows;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPatternSection(AnyPatternSection from) {
        _name       = from._name;
        _nameRecipe = from._nameRecipe;
        _rows       = from._rows;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatternSection WithName(string value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPatternSection WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatternSection copy = new AnyPatternSection(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>rows</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPatternSection WithRows(IReadOnlyList<RowInstruction> value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._rows = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PatternSection" />.</summary>
    public PatternSection Generate() {
        return new PatternSection(_name.Generate(),
                                  _rows.Generate());
    }

}
