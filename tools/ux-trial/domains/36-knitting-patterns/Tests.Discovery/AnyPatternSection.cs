// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PatternSection --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PatternSection" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPatternSection : IAny<PatternSection> {

    private readonly IAny<string>                        _name;
    private readonly IAny<IReadOnlyList<RowInstruction>> _rows;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPatternSection()
        : this(name: AnyValidName(),
               rows: AnyValidRows()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(60);
    }

    private static AnyList<RowInstruction> AnyValidRows() {
        // Rows must be consecutive and alternate sides: the base is a single right-side row 1, which is
        // also what a pattern's first section needs.
        return Any.ListOf(new AnyRowInstruction().WithRowNumber(1).WithSide(FabricSide.RightSide)).WithCount(1);
    }

    private AnyPatternSection(IAny<string>                        name,
                              IAny<IReadOnlyList<RowInstruction>> rows) {
        _name = name;
        _rows = rows;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyPatternSection WithName(string value) {
        return new AnyPatternSection(new FixedValue<string>(value), _rows);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatternSection WithName(Func<AnyString, AnyString> configure) {
        return new AnyPatternSection(configure(AnyValidName()), _rows);
    }

    /// <summary>Pins <c>rows</c> to a fixed value.</summary>
    public AnyPatternSection WithRows(IReadOnlyList<RowInstruction> value) {
        return new AnyPatternSection(_name, new FixedValue<IReadOnlyList<RowInstruction>>(value));
    }

    /// <summary>Draws <c>rows</c> from <c>AnyValidRows()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPatternSection WithRows(Func<AnyList<RowInstruction>, AnyList<RowInstruction>> configure) {
        return new AnyPatternSection(_name, configure(AnyValidRows()));
    }

    /// <summary>Produces one arbitrary <see cref="PatternSection" />.</summary>
    public PatternSection Generate() {
        return new PatternSection(_name.Generate(),
                                  _rows.Generate());
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
