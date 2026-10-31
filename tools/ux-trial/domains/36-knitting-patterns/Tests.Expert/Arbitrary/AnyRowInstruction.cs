// Scaffolded by `any` (JustDummies), then reworked by hand: the printed stitch count is not drawn,
// it is derived from the elements (a pattern prints what its row produces, or nothing).
// `any generate RowInstruction --force` would overwrite it.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>A generator of arbitrary <see cref="RowInstruction" /> values.</summary>
public sealed partial class AnyRowInstruction : IAny<RowInstruction> {

    private readonly IAny<int>                        _rowNumber;
    private readonly IAny<FabricSide>                 _side;
    private readonly IAny<IReadOnlyList<IRowElement>> _elements;
    private readonly IAny<bool>                       _printsCount;

    public AnyRowInstruction()
        : this(rowNumber:   AnyValidRowNumber(),
               side:        AnyValidSide(),
               elements:    AnyValidElements(),
               printsCount: Any.Boolean()) { }

    // Rows are numbered from 1; a flat piece rarely runs past a few hundred rows.
    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32().Between(1, 400);
    }

    private static AnyEnum<FabricSide> AnyValidSide() {
        return Any.Enum<FabricSide>();
    }

    // A written row holds a handful of elements; none of them is null by construction.
    private static AnyList<IRowElement> AnyValidElements() {
        return Any.ListOf(new AnyIRowElement()).WithCountBetween(1, 5);
    }

    private AnyRowInstruction(IAny<int>                        rowNumber,
                              IAny<FabricSide>                 side,
                              IAny<IReadOnlyList<IRowElement>> elements,
                              IAny<bool>                       printsCount) {
        _rowNumber   = rowNumber;
        _side        = side;
        _elements    = elements;
        _printsCount = printsCount;
    }

    public AnyRowInstruction WithRowNumber(int value) {
        return new AnyRowInstruction(new FixedValue<int>(value), _side, _elements, _printsCount);
    }

    public AnyRowInstruction WithRowNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRowInstruction(configure(AnyValidRowNumber()), _side, _elements, _printsCount);
    }

    public AnyRowInstruction WithSide(FabricSide value) {
        return new AnyRowInstruction(_rowNumber, new FixedValue<FabricSide>(value), _elements, _printsCount);
    }

    public AnyRowInstruction WithElements(IReadOnlyList<IRowElement> value) {
        return new AnyRowInstruction(_rowNumber, _side, new FixedValue<IReadOnlyList<IRowElement>>(value), _printsCount);
    }

    public AnyRowInstruction WithElements(Func<AnyList<IRowElement>, AnyList<IRowElement>> configure) {
        return new AnyRowInstruction(_rowNumber, _side, configure(AnyValidElements()), _printsCount);
    }

    /// <summary>Whether the pattern prints the stitch count at the end of the row.</summary>
    public AnyRowInstruction WithPrintedCount(bool value) {
        return new AnyRowInstruction(_rowNumber, _side, _elements, new FixedValue<bool>(value));
    }

    public RowInstruction Generate() {
        IReadOnlyList<IRowElement> elements = _elements.Generate();
        int?                       printed  = _printsCount.Generate() ? elements.Sum(e => e.Produces) : null;

        return new RowInstruction(_rowNumber.Generate(), _side.Generate(), elements, printed);
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
