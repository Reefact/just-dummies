// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GiftCard --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="GiftCard" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGiftCard : IAny<GiftCard> {

    private readonly IAny<string>  _number;
    private readonly IAny<decimal> _balance;
    private readonly IAny<int>     _pin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGiftCard()
        : this(number:  AnyValidNumber(),
               balance: AnyValidBalance(),
               pin:     AnyValidPin()) { }

    private static AnyString AnyValidNumber() {
        // Sixteen ASCII digits.
        return Any.String().Numeric().WithLength(16);
    }

    private static AnyDecimal AnyValidBalance() {
        return Any.Decimal().Between(5m, 500m);
    }

    private static AnyInt32 AnyValidPin() {
        // Four digits: 0 to 9999.
        return Any.Int32().Between(0, 9999);
    }

    private AnyGiftCard(IAny<string>  number,
                        IAny<decimal> balance,
                        IAny<int>     pin) {
        _number  = number;
        _balance = balance;
        _pin     = pin;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyGiftCard WithNumber(string value) {
        return new AnyGiftCard(new FixedValue<string>(value), _balance, _pin);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGiftCard WithNumber(Func<AnyString, AnyString> configure) {
        return new AnyGiftCard(configure(AnyValidNumber()), _balance, _pin);
    }

    /// <summary>Pins <c>balance</c> to a fixed value.</summary>
    public AnyGiftCard WithBalance(decimal value) {
        return new AnyGiftCard(_number, new FixedValue<decimal>(value), _pin);
    }

    /// <summary>Draws <c>balance</c> from <c>AnyValidBalance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGiftCard WithBalance(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyGiftCard(_number, configure(AnyValidBalance()), _pin);
    }

    /// <summary>Pins <c>pin</c> to a fixed value.</summary>
    public AnyGiftCard WithPin(int value) {
        return new AnyGiftCard(_number, _balance, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>pin</c> from <c>AnyValidPin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGiftCard WithPin(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGiftCard(_number, _balance, configure(AnyValidPin()));
    }

    /// <summary>Produces one arbitrary <see cref="GiftCard" />.</summary>
    public GiftCard Generate() {
        return new GiftCard(_number.Generate(),
                            _balance.Generate(),
                            _pin.Generate());
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
