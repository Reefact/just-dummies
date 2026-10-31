// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RowInstruction --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RowInstruction" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRowInstruction : IAny<RowInstruction> {

    private readonly IAny<int>                        _rowNumber;
    private readonly IAny<FabricSide>                 _side;
    private readonly IAny<IReadOnlyList<IRowElement>> _elements;
    private readonly IAny<int?>?                      _expectedStitchCount;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRowInstruction()
        : this(rowNumber:           AnyValidRowNumber(),
               side:                AnyValidSide(),
               elements:            AnyValidElements(),
               expectedStitchCount: null) { }

    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32().Positive();
    }

    private static AnyEnum<FabricSide> AnyValidSide() {
        return Any.Enum<FabricSide>();
    }

    private static AnyList<IRowElement> AnyValidElements() {
        return Any.ListOf(new AnyRowElement()).WithCountBetween(1, 6);
    }

    private static readonly IAny<bool> AnyPrintsItsCount = Any.Boolean();

    private static AnyInt32 AnyValidExpectedStitchCount() {
        return Any.Int32().Positive();
    }

    private AnyRowInstruction(IAny<int>                        rowNumber,
                              IAny<FabricSide>                 side,
                              IAny<IReadOnlyList<IRowElement>> elements,
                              IAny<int?>?                      expectedStitchCount) {
        _rowNumber           = rowNumber;
        _side                = side;
        _elements            = elements;
        _expectedStitchCount = expectedStitchCount;
    }

    /// <summary>Pins <c>rowNumber</c> to a fixed value.</summary>
    public AnyRowInstruction WithRowNumber(int value) {
        return new AnyRowInstruction(new FixedValue<int>(value), _side, _elements, _expectedStitchCount);
    }

    /// <summary>Draws <c>rowNumber</c> from <c>AnyValidRowNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRowInstruction WithRowNumber(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRowInstruction(configure(AnyValidRowNumber()), _side, _elements, _expectedStitchCount);
    }

    /// <summary>Pins <c>side</c> to a fixed value.</summary>
    public AnyRowInstruction WithSide(FabricSide value) {
        return new AnyRowInstruction(_rowNumber, new FixedValue<FabricSide>(value), _elements, _expectedStitchCount);
    }

    /// <summary>Draws <c>side</c> from <c>AnyValidSide()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRowInstruction WithSide(Func<AnyEnum<FabricSide>, AnyEnum<FabricSide>> configure) {
        return new AnyRowInstruction(_rowNumber, configure(AnyValidSide()), _elements, _expectedStitchCount);
    }

    /// <summary>Pins <c>elements</c> to a fixed value.</summary>
    public AnyRowInstruction WithElements(IReadOnlyList<IRowElement> value) {
        return new AnyRowInstruction(_rowNumber, _side, new FixedValue<IReadOnlyList<IRowElement>>(value), _expectedStitchCount);
    }

    /// <summary>Draws <c>elements</c> from <c>AnyValidElements()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRowInstruction WithElements(Func<AnyList<IRowElement>, AnyList<IRowElement>> configure) {
        return new AnyRowInstruction(_rowNumber, _side, configure(AnyValidElements()), _expectedStitchCount);
    }

    /// <summary>Pins <c>expectedStitchCount</c> to a fixed value.</summary>
    public AnyRowInstruction WithExpectedStitchCount(int? value) {
        return new AnyRowInstruction(_rowNumber, _side, _elements, new FixedValue<int?>(value));
    }

    /// <summary>Draws <c>expectedStitchCount</c> from <c>AnyValidExpectedStitchCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRowInstruction WithExpectedStitchCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyRowInstruction(_rowNumber, _side, _elements, configure(AnyValidExpectedStitchCount()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="RowInstruction" />.</summary>
    public RowInstruction Generate() {
        // A printed count must equal what the elements produce: unless a test set it, the row prints
        // the count its elements produce, or prints none.
        int                        rowNumber = _rowNumber.Generate();
        FabricSide                 side      = _side.Generate();
        IReadOnlyList<IRowElement> elements  = _elements.Generate();
        int? expectedStitchCount = _expectedStitchCount is not null
                                       ? _expectedStitchCount.Generate()
                                       : AnyPrintsItsCount.Generate() ? elements.Sum(e => e.Produces) : null;

        return new RowInstruction(rowNumber, side, elements, expectedStitchCount);
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
