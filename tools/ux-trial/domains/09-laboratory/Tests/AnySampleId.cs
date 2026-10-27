// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SampleId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SampleId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySampleId : IAny<SampleId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySampleId()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        // 'S' followed by ten digits.
        return Any.String().StartingWith("S").Numeric().WithLength(11);
    }

    private AnySampleId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySampleId WithValue(string value) {
        return new AnySampleId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnySampleId WithValue(Func<AnyString, AnyString> configure) {
        return new AnySampleId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="SampleId" />.</summary>
    public SampleId Generate() {
        return new SampleId(_value.Generate());
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
