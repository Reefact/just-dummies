// Scaffolded by `any` (JustDummies), then completed by hand: `rows` is not an independent parameter, so it
// is built in Generate() from a first row, a row count and the alternation of the sides.
// `any generate PatternSection --force` would overwrite it.

using System;
using System.Collections.Generic;

using JustDummies;
using KnittingPatterns.Domain;

namespace KnittingPatterns.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PatternSection" /> values: consecutive rows, alternating sides, the
///     first one on the side its number implies (odd rows are right side). The stitch counts of the rows are
///     arbitrary and unrelated: this is a section as a block of rows, not an expandable one.
/// </summary>
public sealed partial class AnyPatternSection : IAny<PatternSection> {

    private IAny<string>                       _name;
    private IAny<int>                          _firstRow;
    private IAny<int>                          _rowCount;
    private IAny<IReadOnlyList<RowInstruction>>? _rows;   // when pinned, replaces the built rows

    private AnyString _nameRecipe;
    private AnyInt32  _firstRowRecipe;
    private AnyInt32  _rowCountRecipe;

    /// <summary>Creates the generator with a default recipe for every parameter.</summary>
    public AnyPatternSection()
        : this(name:     AnyValidName(),
               firstRow: AnyValidFirstRow(),
               rowCount: AnyValidRowCount()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength);
    }

    private static AnyInt32 AnyValidFirstRow() {
        return Any.Int32().Between(1, 200);
    }

    private static AnyInt32 AnyValidRowCount() {
        return Any.Int32().Between(1, 6);
    }

    private AnyPatternSection(AnyString name, AnyInt32 firstRow, AnyInt32 rowCount) {
        _name           = name;
        _nameRecipe     = name;
        _firstRow       = firstRow;
        _firstRowRecipe = firstRow;
        _rowCount       = rowCount;
        _rowCountRecipe = rowCount;
    }

    private AnyPatternSection(AnyPatternSection from) {
        _name           = from._name;
        _nameRecipe     = from._nameRecipe;
        _firstRow       = from._firstRow;
        _firstRowRecipe = from._firstRowRecipe;
        _rowCount       = from._rowCount;
        _rowCountRecipe = from._rowCountRecipe;
        _rows           = from._rows;
    }

    public AnyPatternSection WithName(string value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPatternSection copy = new AnyPatternSection(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>The section starts at this row; its first row is on the side the row number implies.</summary>
    public AnyPatternSection StartingAtRow(int value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._firstRow = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithRowCount(int value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._rowCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the rows. Whatever the test passes is handed over as it is, valid or not.</summary>
    public AnyPatternSection WithRows(IReadOnlyList<RowInstruction> value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._rows = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PatternSection" />.</summary>
    public PatternSection Generate() {
        if (_rows is not null) {
            return new PatternSection(_name.Generate(), _rows.Generate());
        }

        int                  firstRow = _firstRow.Generate();
        int                  rowCount = _rowCount.Generate();
        List<RowInstruction> rows     = new List<RowInstruction>(rowCount);
        for (int i = 0; i < rowCount; i++) {
            int        rowNumber = firstRow + i;
            FabricSide side      = rowNumber % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;

            rows.Add(Any.RowInstruction().WithRowNumber(rowNumber).WithSide(side).Generate());
        }

        return new PatternSection(_name.Generate(), rows);
    }

}
