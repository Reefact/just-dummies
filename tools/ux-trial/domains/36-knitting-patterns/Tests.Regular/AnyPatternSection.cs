// Scaffolded by `any`, then rewritten by hand: the section's rows are consecutive and alternate sides, a rule
// between the rows that no per-parameter recipe expresses. `rows` is therefore drawn as a run (first row, first
// side, count, a template for each row) instead of as a free list. `any generate --force` would lose this.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>A generator of arbitrary <see cref="PatternSection" /> values: consecutive rows on alternating sides.</summary>
public sealed partial class AnyPatternSection : IAny<PatternSection> {

    private IAny<string>                        _name;
    private AnyString                           _nameRecipe;
    private IAny<IReadOnlyList<RowInstruction>>? _pinnedRows;
    private IAny<int>                           _firstRow;
    private IAny<FabricSide>                    _firstSide;
    private IAny<int>                           _rowCount;
    private AnyRowInstruction                   _row;

    public AnyPatternSection() {
        _nameRecipe = Any.String().NotBlank().WithMaxLength(PatternSection.MaxNameLength);
        _name       = _nameRecipe;
        _firstRow   = Any.Int32().Between(1, 50);
        _firstSide  = Any.Enum<FabricSide>();
        _rowCount   = Any.Int32().Between(1, 6);
        _row        = new AnyRowInstruction();
    }

    private AnyPatternSection(AnyPatternSection from) {
        _name       = from._name;
        _nameRecipe = from._nameRecipe;
        _pinnedRows = from._pinnedRows;
        _firstRow   = from._firstRow;
        _firstSide  = from._firstSide;
        _rowCount   = from._rowCount;
        _row        = from._row;
    }

    public AnyPatternSection WithName(string value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithName(Func<AnyString, AnyString> configure) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins the rows as they are, whatever their numbers and sides.</summary>
    public AnyPatternSection WithRows(IReadOnlyList<RowInstruction> value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._pinnedRows = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithFirstRow(int value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._firstRow = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithFirstSide(FabricSide value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._firstSide = Any.Fixed(value);

        return copy;
    }

    public AnyPatternSection WithRowCount(int value) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._rowCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the template every row of the run is drawn from (its number and side are set by the run).</summary>
    public AnyPatternSection WithRow(Func<AnyRowInstruction, AnyRowInstruction> configure) {
        AnyPatternSection copy = new AnyPatternSection(this);

        copy._row = configure(_row);

        return copy;
    }

    public PatternSection Generate() {
        if (_pinnedRows is not null) {
            return new PatternSection(_name.Generate(), _pinnedRows.Generate());
        }

        int        firstRow  = _firstRow.Generate();
        FabricSide firstSide = _firstSide.Generate();
        int        count     = _rowCount.Generate();

        List<RowInstruction> rows = new List<RowInstruction>();
        for (int i = 0; i < count; i++) {
            FabricSide side = i % 2 == 0 ? firstSide : (firstSide == FabricSide.RightSide ? FabricSide.WrongSide : FabricSide.RightSide);
            rows.Add(_row.WithRowNumber(firstRow + i).WithSide(side).Generate());
        }

        return new PatternSection(_name.Generate(), rows);
    }

}
