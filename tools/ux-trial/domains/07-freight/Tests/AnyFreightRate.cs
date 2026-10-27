// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FreightRate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Freight.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FreightRate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFreightRate : IAny<FreightRate> {

    private readonly IAny<decimal> _perRevenueTon;
    private readonly IAny<string>  _currency;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFreightRate()
        : this(perRevenueTon: AnyValidPerRevenueTon(),
               currency:      AnyValidCurrency()) { }

    private static AnyDecimal AnyValidPerRevenueTon() {
        return Any.Decimal().Positive().LessThanOrEqualTo(10_000m).WithScale(2);
    }

    private static AnyString AnyValidCurrency() {
        // Hand fix: the constructor checks ^[A-Z]{3}$, a guard the scaffolder did not report.
        return Any.String().Alpha().InUpperCase().WithLength(3);
    }

    private AnyFreightRate(IAny<decimal> perRevenueTon,
                           IAny<string>  currency) {
        _perRevenueTon = perRevenueTon;
        _currency      = currency;
    }

    /// <summary>Pins <c>perRevenueTon</c> to a fixed value.</summary>
    public AnyFreightRate WithPerRevenueTon(decimal value) {
        return new AnyFreightRate(new FixedValue<decimal>(value), _currency);
    }

    /// <summary>Draws <c>perRevenueTon</c> from <c>AnyValidPerRevenueTon()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFreightRate WithPerRevenueTon(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyFreightRate(configure(AnyValidPerRevenueTon()), _currency);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyFreightRate WithCurrency(string value) {
        return new AnyFreightRate(_perRevenueTon, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFreightRate WithCurrency(Func<AnyString, AnyString> configure) {
        return new AnyFreightRate(_perRevenueTon, configure(AnyValidCurrency()));
    }

    /// <summary>Produces one arbitrary <see cref="FreightRate" />.</summary>
    public FreightRate Generate() {
        return new FreightRate(_perRevenueTon.Generate(),
                               _currency.Generate());
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
