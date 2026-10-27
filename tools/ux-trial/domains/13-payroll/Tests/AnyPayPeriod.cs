// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PayPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PayPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPayPeriod : IAny<PayPeriod> {

    private readonly IAny<int> _year;
    private readonly IAny<int> _month;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayPeriod()
        : this(year:  AnyValidYear(),
               month: AnyValidMonth()) { }

    private static AnyInt32 AnyValidYear() {
        return Any.Int32().Between(2000, 2100);
    }

    private static AnyInt32 AnyValidMonth() {
        return Any.Int32().Between(1, 12);
    }

    private AnyPayPeriod(IAny<int> year,
                         IAny<int> month) {
        _year  = year;
        _month = month;
    }

    /// <summary>Pins <c>year</c> to a fixed value.</summary>
    public AnyPayPeriod WithYear(int value) {
        return new AnyPayPeriod(new FixedValue<int>(value), _month);
    }

    /// <summary>Draws <c>year</c> from <c>AnyValidYear()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayPeriod WithYear(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPayPeriod(configure(AnyValidYear()), _month);
    }

    /// <summary>Pins <c>month</c> to a fixed value.</summary>
    public AnyPayPeriod WithMonth(int value) {
        return new AnyPayPeriod(_year, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>month</c> from <c>AnyValidMonth()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayPeriod WithMonth(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPayPeriod(_year, configure(AnyValidMonth()));
    }

    /// <summary>Produces one arbitrary <see cref="PayPeriod" />.</summary>
    public PayPeriod Generate() {
        return new PayPeriod(_year.Generate(),
                             _month.Generate());
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
