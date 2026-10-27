// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StudentNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StudentNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStudentNumber : IAny<StudentNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStudentNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching(@"STU-(19|20)\d{2}-\d{5}");
    }

    private AnyStudentNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyStudentNumber WithValue(string value) {
        return new AnyStudentNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStudentNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyStudentNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="StudentNumber" />.</summary>
    public StudentNumber Generate() {
        return new StudentNumber(_value.Generate());
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
