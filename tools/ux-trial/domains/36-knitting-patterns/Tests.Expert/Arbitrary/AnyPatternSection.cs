// Scaffolded by `any` (JustDummies), then reworked by hand: a section's rows are consecutive and
// alternate sides, a rule between the elements of the list that no per-element recipe can honour.
// Rows are therefore built from a first row and a row count. `any generate PatternSection --force`
// would overwrite it.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>A generator of arbitrary <see cref="PatternSection" /> values.</summary>
public sealed partial class AnyPatternSection : IAny<PatternSection> {

    private readonly IAny<string>                        _name;
    private readonly IAny<int>                           _firstRow;
    private readonly IAny<int>                           _rowCount;
    private readonly IAny<IReadOnlyList<RowInstruction>>? _rows;

    public AnyPatternSection()
        : this(AnyValidName(), Any.Int32().Between(1, 200), Any.Int32().Between(1, 12), rows: null) { }

    // "Ribbing", "Body", "Lace panel": a short readable name.
    private static AnyString AnyValidName() {
        return Any.String().Alpha().WithLengthBetween(3, PatternSection.MaxNameLength);
    }

    private AnyPatternSection(IAny<string> name, IAny<int> firstRow, IAny<int> rowCount, IAny<IReadOnlyList<RowInstruction>>? rows) {
        _name     = name;
        _firstRow = firstRow;
        _rowCount = rowCount;
        _rows     = rows;
    }

    public AnyPatternSection WithName(string value) {
        return new AnyPatternSection(new FixedValue<string>(value), _firstRow, _rowCount, _rows);
    }

    public AnyPatternSection WithName(Func<AnyString, AnyString> configure) {
        return new AnyPatternSection(configure(AnyValidName()), _firstRow, _rowCount, _rows);
    }

    /// <summary>Covers exactly rows <paramref name="firstRow" />..<paramref name="lastRow" />, their contents arbitrary.</summary>
    public AnyPatternSection Covering(int firstRow, int lastRow) {
        return new AnyPatternSection(_name, new FixedValue<int>(firstRow), new FixedValue<int>(lastRow - firstRow + 1), rows: null);
    }

    public AnyPatternSection WithRows(IReadOnlyList<RowInstruction> value) {
        return new AnyPatternSection(_name, _firstRow, _rowCount, new FixedValue<IReadOnlyList<RowInstruction>>(value));
    }

    public PatternSection Generate() {
        return new PatternSection(_name.Generate(), _rows?.Generate() ?? DrawRows());
    }

    // Consecutive rows, right side on odd rows as on a flat piece; what each row says is arbitrary.
    private RowInstruction[] DrawRows() {
        int firstRow = _firstRow.Generate();
        int rowCount = _rowCount.Generate();

        return Enumerable.Range(firstRow, rowCount)
                         .Select(n => new AnyRowInstruction().WithRowNumber(n)
                                                             .WithSide(n % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide)
                                                             .Generate())
                         .ToArray();
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
