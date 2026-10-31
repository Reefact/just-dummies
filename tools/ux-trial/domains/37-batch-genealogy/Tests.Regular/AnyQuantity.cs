// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Quantity --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Quantity" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyQuantity : IAny<Quantity> {

    private readonly IAny<decimal>      _amount;
    private readonly IAny<QuantityUnit> _unit;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyQuantity()
        : this(amount: AnyValidAmount(),
               unit:   AnyValidUnit()) { }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Between(0m, 1000000000m);
    }

    private static AnyEnum<QuantityUnit> AnyValidUnit() {
        return Any.Enum<QuantityUnit>();
    }

    private AnyQuantity(IAny<decimal>      amount,
                        IAny<QuantityUnit> unit) {
        _amount = amount;
        _unit   = unit;
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyQuantity WithAmount(decimal value) {
        return new AnyQuantity(new FixedValue<decimal>(value), _unit);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuantity WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyQuantity(configure(AnyValidAmount()), _unit);
    }

    /// <summary>Pins <c>unit</c> to a fixed value.</summary>
    public AnyQuantity WithUnit(QuantityUnit value) {
        return new AnyQuantity(_amount, new FixedValue<QuantityUnit>(value));
    }

    /// <summary>Draws <c>unit</c> from <c>AnyValidUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyQuantity WithUnit(Func<AnyEnum<QuantityUnit>, AnyEnum<QuantityUnit>> configure) {
        return new AnyQuantity(_amount, configure(AnyValidUnit()));
    }

    /// <summary>Produces one arbitrary <see cref="Quantity" />.</summary>
    public Quantity Generate() {
        return new Quantity(_amount.Generate(),
                            _unit.Generate());
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
