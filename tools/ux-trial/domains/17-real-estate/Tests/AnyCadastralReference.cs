// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CadastralReference --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CadastralReference" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCadastralReference : IAny<CadastralReference> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCadastralReference()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // Commune (5 digits, or 2A/2B + 3 digits), prefix, section, plan number from 0001.
        return Any.StringMatching(@"(\d{5}|2[AB]\d{3}) \d{3} [A-Z0-9]{2} (\d{3}[1-9]|\d{2}[1-9]\d|\d[1-9]\d{2}|[1-9]\d{3})");
    }

    private AnyCadastralReference(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyCadastralReference WithValue(string value) {
        return new AnyCadastralReference(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCadastralReference WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCadastralReference(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="CadastralReference" />.</summary>
    public CadastralReference Generate() {
        return new CadastralReference(_value.Generate());
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
