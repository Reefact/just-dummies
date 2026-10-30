// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PromoCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using Shop.Domain;

namespace Shop.Tests;

/// <summary>
///     A generator of arbitrary <see cref="PromoCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPromoCode : IAny<PromoCode> {

    private readonly IAny<string>   _code;
    private readonly IAny<decimal>  _percentOff;
    private readonly IAny<DateOnly> _validFrom;
    private readonly IAny<DateOnly> _validUntil;
    private readonly IAny<decimal>  _minimumBasket;
    private readonly IAny<int>      _maxUses;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPromoCode()
        : this(code:          AnyValidCode(),
               percentOff:    AnyValidPercentOff(),
               validFrom:     AnyValidValidFrom(),
               validUntil:    AnyValidValidUntil(),
               minimumBasket: AnyValidMinimumBasket(),
               maxUses:       AnyValidMaxUses()) { }

    private static AnyString AnyValidCode() {
        // 4 to 12 characters; an alphanumeric body is what the marketing team issues.
        return Any.String().AlphaNumeric().WithLengthBetween(4, 12);
    }

    private static AnyDecimal AnyValidPercentOff() {
        // More than 0 % and at most 50 %.
        return Any.Decimal().GreaterThan(0m).LessThanOrEqualTo(50m);
    }

    private static AnyDateOnly AnyValidValidFrom() {
        // validFrom <= validUntil: two disjoint windows, as the bases are drawn independently.
        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31));
    }

    private static AnyDateOnly AnyValidValidUntil() {
        // On or after any validFrom the base draws (see AnyValidValidFrom).
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static AnyDecimal AnyValidMinimumBasket() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyInt32 AnyValidMaxUses() {
        return Any.Int32();
    }

    private AnyPromoCode(IAny<string>   code,
                         IAny<decimal>  percentOff,
                         IAny<DateOnly> validFrom,
                         IAny<DateOnly> validUntil,
                         IAny<decimal>  minimumBasket,
                         IAny<int>      maxUses) {
        _code          = code;
        _percentOff    = percentOff;
        _validFrom     = validFrom;
        _validUntil    = validUntil;
        _minimumBasket = minimumBasket;
        _maxUses       = maxUses;
    }

    /// <summary>Pins <c>code</c> to a fixed value.</summary>
    public AnyPromoCode WithCode(string value) {
        return new AnyPromoCode(new FixedValue<string>(value), _percentOff, _validFrom, _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Draws <c>code</c> from <c>AnyValidCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithCode(Func<AnyString, AnyString> configure) {
        return new AnyPromoCode(configure(AnyValidCode()), _percentOff, _validFrom, _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Pins <c>percentOff</c> to a fixed value.</summary>
    public AnyPromoCode WithPercentOff(decimal value) {
        return new AnyPromoCode(_code, new FixedValue<decimal>(value), _validFrom, _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Draws <c>percentOff</c> from <c>AnyValidPercentOff()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithPercentOff(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPromoCode(_code, configure(AnyValidPercentOff()), _validFrom, _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Pins <c>validFrom</c> to a fixed value.</summary>
    public AnyPromoCode WithValidFrom(DateOnly value) {
        return new AnyPromoCode(_code, _percentOff, new FixedValue<DateOnly>(value), _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Draws <c>validFrom</c> from <c>AnyValidValidFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithValidFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyPromoCode(_code, _percentOff, configure(AnyValidValidFrom()), _validUntil, _minimumBasket, _maxUses);
    }

    /// <summary>Pins <c>validUntil</c> to a fixed value.</summary>
    public AnyPromoCode WithValidUntil(DateOnly value) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, new FixedValue<DateOnly>(value), _minimumBasket, _maxUses);
    }

    /// <summary>Draws <c>validUntil</c> from <c>AnyValidValidUntil()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithValidUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, configure(AnyValidValidUntil()), _minimumBasket, _maxUses);
    }

    /// <summary>Pins <c>minimumBasket</c> to a fixed value.</summary>
    public AnyPromoCode WithMinimumBasket(decimal value) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, _validUntil, new FixedValue<decimal>(value), _maxUses);
    }

    /// <summary>Draws <c>minimumBasket</c> from <c>AnyValidMinimumBasket()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithMinimumBasket(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, _validUntil, configure(AnyValidMinimumBasket()), _maxUses);
    }

    /// <summary>Pins <c>maxUses</c> to a fixed value.</summary>
    public AnyPromoCode WithMaxUses(int value) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, _validUntil, _minimumBasket, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>maxUses</c> from <c>AnyValidMaxUses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPromoCode WithMaxUses(Func<AnyInt32, AnyInt32> configure) {
        return new AnyPromoCode(_code, _percentOff, _validFrom, _validUntil, _minimumBasket, configure(AnyValidMaxUses()));
    }

    /// <summary>Produces one arbitrary <see cref="PromoCode" />.</summary>
    public PromoCode Generate() {
        return new PromoCode(_code.Generate(),
                             _percentOff.Generate(),
                             _validFrom.Generate(),
                             _validUntil.Generate(),
                             _minimumBasket.Generate(),
                             _maxUses.Generate());
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
