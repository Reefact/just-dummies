// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DataBundle --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Telecom.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DataBundle" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDataBundle : IAny<DataBundle> {

    private readonly IAny<int>      _megabytes;
    private readonly IAny<decimal>  _price;
    private readonly IAny<TimeSpan> _validity;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDataBundle()
        : this(megabytes: AnyValidMegabytes(),
               price:     AnyValidPrice(),
               validity:  AnyValidValidity()) { }

    private static AnyInt32 AnyValidMegabytes() {
        return Any.Int32().Between(100, 100_000).MultipleOf(100);
    }

    private static AnyDecimal AnyValidPrice() {
        return Any.Decimal().Between(0.01m, 500m);
    }

    private static AnyTimeSpan AnyValidValidity() {
        return Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.FromDays(1));
    }

    private AnyDataBundle(IAny<int>      megabytes,
                          IAny<decimal>  price,
                          IAny<TimeSpan> validity) {
        _megabytes = megabytes;
        _price     = price;
        _validity  = validity;
    }

    /// <summary>Pins <c>megabytes</c> to a fixed value.</summary>
    public AnyDataBundle WithMegabytes(int value) {
        return new AnyDataBundle(new FixedValue<int>(value), _price, _validity);
    }

    /// <summary>Draws <c>megabytes</c> from <c>AnyValidMegabytes()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDataBundle WithMegabytes(Func<AnyInt32, AnyInt32> configure) {
        return new AnyDataBundle(configure(AnyValidMegabytes()), _price, _validity);
    }

    /// <summary>Pins <c>price</c> to a fixed value.</summary>
    public AnyDataBundle WithPrice(decimal value) {
        return new AnyDataBundle(_megabytes, new FixedValue<decimal>(value), _validity);
    }

    /// <summary>Draws <c>price</c> from <c>AnyValidPrice()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDataBundle WithPrice(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyDataBundle(_megabytes, configure(AnyValidPrice()), _validity);
    }

    /// <summary>Pins <c>validity</c> to a fixed value.</summary>
    public AnyDataBundle WithValidity(TimeSpan value) {
        return new AnyDataBundle(_megabytes, _price, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>validity</c> from <c>AnyValidValidity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDataBundle WithValidity(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyDataBundle(_megabytes, _price, configure(AnyValidValidity()));
    }

    /// <summary>Produces one arbitrary <see cref="DataBundle" />.</summary>
    public DataBundle Generate() {
        return new DataBundle(_megabytes.Generate(),
                              _price.Generate(),
                              _validity.Generate());
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
