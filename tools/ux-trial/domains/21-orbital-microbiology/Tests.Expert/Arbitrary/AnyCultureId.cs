// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CultureId --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using OrbitalMicrobiology.Domain;

namespace OrbitalMicrobiology.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="CultureId" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCultureId : IAny<CultureId> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCultureId()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // CHnn-Cnnnn, chamber nn in 01..MaxChamber (12).
        return Any.StringMatching("CH(0[1-9]|1[0-2])-C[0-9]{4}");
    }

    private AnyCultureId(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyCultureId WithValue(string value) {
        return new AnyCultureId(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCultureId WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCultureId(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="CultureId" />.</summary>
    public CultureId Generate() {
        return new CultureId(_value.Generate());
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
