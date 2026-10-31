// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StudbookId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CaptiveBreeding.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StudbookId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStudbookId : IAny<StudbookId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStudbookId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // StudbookId.Format: 2-4 uppercase letters, a dash, 4-6 digits (e.g. ZSL-01234).
        return Any.StringMatching("[A-Z]{2,4}-[0-9]{4,6}");
    }

    private AnyStudbookId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyStudbookId WithValue(string value) {
        return new AnyStudbookId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStudbookId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyStudbookId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="StudbookId" />.</summary>
    public StudbookId Generate() {
        return new StudbookId(_value.Generate());
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
