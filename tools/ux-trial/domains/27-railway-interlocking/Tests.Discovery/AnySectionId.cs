// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SectionId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SectionId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySectionId : IAny<SectionId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySectionId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // SectionId.cs: 1-2 capital letters then 1-4 digits.
        return Any.StringMatching("[A-Z]{1,2}[0-9]{1,4}");
    }

    private AnySectionId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnySectionId WithValue(string value) {
        return new AnySectionId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnySectionId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnySectionId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="SectionId" />.</summary>
    public SectionId Generate() {
        return new SectionId(_value.Generate());
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
