// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Grade --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Repro.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Grade" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGrade : IAny<Grade> {

    private readonly IAny<int> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGrade()
        : this(value: AnyValidValue()) { }

    private static AnyInt32 AnyValidValue() {
        return Any.Int32().Between(0, 4);
    }

    private AnyGrade(IAny<int> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyGrade WithValue(int value) {
        return new AnyGrade(new FixedValue<int>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGrade WithValue(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGrade(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="Grade" />.</summary>
    public Grade Generate() {
        return new Grade(_value.Generate());
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
