// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CashFlowSchedule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CashFlowSchedule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCashFlowSchedule : IAny<CashFlowSchedule> {

    private readonly IAny<string>                _scheduleId;
    private readonly IAny<DateOnly>              _firstPaymentDate;
    private readonly IAny<int>                   _frequencyMonths;
    private readonly IAny<int>                   _numberOfPayments;
    private readonly IAny<decimal>               _amountPerPayment;
    private readonly IAny<Currency>              _currency;
    private readonly IAny<FlowDirection>         _direction;
    private readonly IAny<BusinessDayConvention> _convention;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCashFlowSchedule()
        : this(scheduleId:       AnyValidScheduleId(),
               firstPaymentDate: AnyValidFirstPaymentDate(),
               frequencyMonths:  AnyValidFrequencyMonths(),
               numberOfPayments: AnyValidNumberOfPayments(),
               amountPerPayment: AnyValidAmountPerPayment(),
               currency:         AnyValidCurrency(),
               direction:        AnyValidDirection(),
               convention:       AnyValidConvention()) { }

    private static AnyString AnyValidScheduleId() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidFirstPaymentDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    private static AnyInt32 AnyValidFrequencyMonths() {
        return Any.Int32().OneOf(1, 3, 6, 12);
    }

    private static AnyInt32 AnyValidNumberOfPayments() {
        return Any.Int32().Between(1, 600);
    }

    private static AnyDecimal AnyValidAmountPerPayment() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyEnum<FlowDirection> AnyValidDirection() {
        return Any.Enum<FlowDirection>();
    }

    private static AnyEnum<BusinessDayConvention> AnyValidConvention() {
        return Any.Enum<BusinessDayConvention>();
    }

    private AnyCashFlowSchedule(IAny<string>                scheduleId,
                                IAny<DateOnly>              firstPaymentDate,
                                IAny<int>                   frequencyMonths,
                                IAny<int>                   numberOfPayments,
                                IAny<decimal>               amountPerPayment,
                                IAny<Currency>              currency,
                                IAny<FlowDirection>         direction,
                                IAny<BusinessDayConvention> convention) {
        _scheduleId       = scheduleId;
        _firstPaymentDate = firstPaymentDate;
        _frequencyMonths  = frequencyMonths;
        _numberOfPayments = numberOfPayments;
        _amountPerPayment = amountPerPayment;
        _currency         = currency;
        _direction        = direction;
        _convention       = convention;
    }

    /// <summary>Pins <c>scheduleId</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithScheduleId(string value) {
        return new AnyCashFlowSchedule(new FixedValue<string>(value), _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Draws <c>scheduleId</c> from <c>AnyValidScheduleId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithScheduleId(Func<AnyString, AnyString> configure) {
        return new AnyCashFlowSchedule(configure(AnyValidScheduleId()), _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Pins <c>firstPaymentDate</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithFirstPaymentDate(DateOnly value) {
        return new AnyCashFlowSchedule(_scheduleId, new FixedValue<DateOnly>(value), _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Draws <c>firstPaymentDate</c> from <c>AnyValidFirstPaymentDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithFirstPaymentDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyCashFlowSchedule(_scheduleId, configure(AnyValidFirstPaymentDate()), _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Pins <c>frequencyMonths</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithFrequencyMonths(int value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, new FixedValue<int>(value), _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Draws <c>frequencyMonths</c> from <c>AnyValidFrequencyMonths()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithFrequencyMonths(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, configure(AnyValidFrequencyMonths()), _numberOfPayments, _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Pins <c>numberOfPayments</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithNumberOfPayments(int value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, new FixedValue<int>(value), _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Draws <c>numberOfPayments</c> from <c>AnyValidNumberOfPayments()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithNumberOfPayments(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, configure(AnyValidNumberOfPayments()), _amountPerPayment, _currency, _direction, _convention);
    }

    /// <summary>Pins <c>amountPerPayment</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithAmountPerPayment(decimal value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, new FixedValue<decimal>(value), _currency, _direction, _convention);
    }

    /// <summary>Draws <c>amountPerPayment</c> from <c>AnyValidAmountPerPayment()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithAmountPerPayment(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, configure(AnyValidAmountPerPayment()), _currency, _direction, _convention);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithCurrency(Currency value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, new FixedValue<Currency>(value), _direction, _convention);
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, configure(AnyValidCurrency()), _direction, _convention);
    }

    /// <summary>Pins <c>direction</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithDirection(FlowDirection value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, new FixedValue<FlowDirection>(value), _convention);
    }

    /// <summary>Draws <c>direction</c> from <c>AnyValidDirection()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithDirection(Func<AnyEnum<FlowDirection>, AnyEnum<FlowDirection>> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, configure(AnyValidDirection()), _convention);
    }

    /// <summary>Pins <c>convention</c> to a fixed value.</summary>
    public AnyCashFlowSchedule WithConvention(BusinessDayConvention value) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, new FixedValue<BusinessDayConvention>(value));
    }

    /// <summary>Draws <c>convention</c> from <c>AnyValidConvention()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCashFlowSchedule WithConvention(Func<AnyEnum<BusinessDayConvention>, AnyEnum<BusinessDayConvention>> configure) {
        return new AnyCashFlowSchedule(_scheduleId, _firstPaymentDate, _frequencyMonths, _numberOfPayments, _amountPerPayment, _currency, _direction, configure(AnyValidConvention()));
    }

    /// <summary>Produces one arbitrary <see cref="CashFlowSchedule" />.</summary>
    public CashFlowSchedule Generate() {
        return new CashFlowSchedule(_scheduleId.Generate(),
                                    _firstPaymentDate.Generate(),
                                    _frequencyMonths.Generate(),
                                    _numberOfPayments.Generate(),
                                    _amountPerPayment.Generate(),
                                    _currency.Generate(),
                                    _direction.Generate(),
                                    _convention.Generate());
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
