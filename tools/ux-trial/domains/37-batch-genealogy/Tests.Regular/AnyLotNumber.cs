// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate LotNumber --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="LotNumber" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLotNumber : IAny<LotNumber> {

    private readonly IAny<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLotNumber()
        : this(value: AnyValidValue()) { }

    private static AnyPattern AnyValidValue() {
        // A base lot number; a sub-lot is derived from it with LotNumber.SubLot(index).
        return Any.StringMatching("[A-Z]{2,4}-[0-9]{6}");
    }

    private AnyLotNumber(IAny<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public AnyLotNumber WithValue(string value) {
        return new AnyLotNumber(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <c>AnyValidValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLotNumber WithValue(Func<AnyPattern, AnyPattern> configure) {
        return new AnyLotNumber(configure(AnyValidValue()));
    }

    /// <summary>Produces one arbitrary <see cref="LotNumber" />.</summary>
    public LotNumber Generate() {
        return new LotNumber(_value.Generate());
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
