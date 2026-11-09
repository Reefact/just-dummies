// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LotCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GuardMiss.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LotCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLotCode : IAny<LotCode> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLotCode()
        : this(value: AnyValidValue()) { }

    private static AnyString AnyValidValue() {
        return Any.String().NonEmpty();
    }

    private AnyLotCode(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyLotCode WithValue(string value) {
        return new AnyLotCode(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLotCode WithValue(Func<AnyString, AnyString> configure) {
        return new AnyLotCode(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="LotCode" />.</summary>
    public LotCode Generate() {
        return new LotCode(_value.Generate());
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
