// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Staff --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Staff" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStaff : IAny<Staff> {

    private readonly IAny<string>                 _name;
    private readonly IAny<IReadOnlyList<Measure>> _measures;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStaff()
        : this(name:     AnyValidName(),
               measures: AnyValidMeasures()) { }

    private static AnyString AnyValidName() {
        // "Violin I", "Piano RH": letters, at most 40 characters, no surrounding blank.
        return Any.String().Alpha().WithLengthBetween(1, Staff.MaxNameLength);
    }

    private static IAny<IReadOnlyList<Measure>> AnyValidMeasures() {
        // A staff numbers its measures 1..n in order: draw how many, then number them.
        return Any.Int32().Between(1, 8).As(count => MeasuresShapedBy(Enumerable.Repeat<Func<AnyMeasure, AnyMeasure>>(measure => measure, count)));
    }

    private AnyStaff(IAny<string>                 name,
                     IAny<IReadOnlyList<Measure>> measures) {
        _name     = name;
        _measures = measures;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyStaff WithName(string value) {
        return new AnyStaff(new FixedValue<string>(value), _measures);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStaff WithName(Func<AnyString, AnyString> configure) {
        return new AnyStaff(configure(AnyValidName()), _measures);
    }

    /// <summary>Pins <c>measures</c> to a fixed value.</summary>
    public AnyStaff WithMeasures(IReadOnlyList<Measure> value) {
        return new AnyStaff(_name, new FixedValue<IReadOnlyList<Measure>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="Staff" />.</summary>
    public Staff Generate() {
        return new Staff(_name.Generate(),
                         _measures.Generate());
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
