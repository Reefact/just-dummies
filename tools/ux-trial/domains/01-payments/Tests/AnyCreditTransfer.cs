// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CreditTransfer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CreditTransfer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCreditTransfer : IAny<CreditTransfer> {

    private readonly IAny<string>                _endToEndId;
    private readonly IAny<Party>                 _debtor;
    private readonly IAny<Party>                 _creditor;
    private readonly IAny<Money>                 _amount;
    private readonly IAny<DateOnly>              _requestedExecutionDate;
    private readonly IAny<TransferPriority>      _priority;
    private readonly IAny<RemittanceInformation> _remittanceInformation;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCreditTransfer()
        : this(endToEndId:             AnyValidEndToEndId(),
               debtor:                 AnyValidDebtor(),
               creditor:               AnyValidCreditor(),
               amount:                 AnyValidAmount(),
               requestedExecutionDate: AnyValidRequestedExecutionDate(),
               priority:               AnyValidPriority(),
               remittanceInformation:  AnyValidRemittanceInformation()) { }

    private static AnyString AnyValidEndToEndId() {
        return Any.String().AlphaNumeric().WithLengthBetween(1, 35);
    }

    private static AnyParty AnyValidDebtor() {
        return new AnyParty();
    }

    private static AnyParty AnyValidCreditor() {
        return new AnyParty();
    }

    private static AnyMoney AnyValidAmount() {
        return new AnyMoney().WithCurrency(Currency.EUR).WithAmount(a => a.LessThanOrEqualTo(CreditTransfer.InstantCeiling));
    }

    private static AnyDateOnly AnyValidRequestedExecutionDate() {
        return Any.DateOnly().OneOf(BusinessDays);
    }

    private static AnyEnum<TransferPriority> AnyValidPriority() {
        return Any.Enum<TransferPriority>();
    }

    private static AnyRemittanceInformation AnyValidRemittanceInformation() {
        return new AnyRemittanceInformation();
    }

    private AnyCreditTransfer(IAny<string>                endToEndId,
                              IAny<Party>                 debtor,
                              IAny<Party>                 creditor,
                              IAny<Money>                 amount,
                              IAny<DateOnly>              requestedExecutionDate,
                              IAny<TransferPriority>      priority,
                              IAny<RemittanceInformation> remittanceInformation) {
        _endToEndId             = endToEndId;
        _debtor                 = debtor;
        _creditor               = creditor;
        _amount                 = amount;
        _requestedExecutionDate = requestedExecutionDate;
        _priority               = priority;
        _remittanceInformation  = remittanceInformation;
    }

    /// <summary>Pins <c>endToEndId</c> to a fixed value.</summary>
    public AnyCreditTransfer WithEndToEndId(string value) {
        return new AnyCreditTransfer(new FixedValue<string>(value), _debtor, _creditor, _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Draws <c>endToEndId</c> from <c>AnyValidEndToEndId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithEndToEndId(Func<AnyString, AnyString> configure) {
        return new AnyCreditTransfer(configure(AnyValidEndToEndId()), _debtor, _creditor, _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Pins <c>debtor</c> to a fixed value.</summary>
    public AnyCreditTransfer WithDebtor(Party value) {
        return new AnyCreditTransfer(_endToEndId, new FixedValue<Party>(value), _creditor, _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Draws <c>debtor</c> from <c>AnyValidDebtor()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithDebtor(Func<AnyParty, AnyParty> configure) {
        return new AnyCreditTransfer(_endToEndId, configure(AnyValidDebtor()), _creditor, _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Pins <c>creditor</c> to a fixed value.</summary>
    public AnyCreditTransfer WithCreditor(Party value) {
        return new AnyCreditTransfer(_endToEndId, _debtor, new FixedValue<Party>(value), _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Draws <c>creditor</c> from <c>AnyValidCreditor()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithCreditor(Func<AnyParty, AnyParty> configure) {
        return new AnyCreditTransfer(_endToEndId, _debtor, configure(AnyValidCreditor()), _amount, _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyCreditTransfer WithAmount(Money value) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, new FixedValue<Money>(value), _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithAmount(Func<AnyMoney, AnyMoney> configure) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, configure(AnyValidAmount()), _requestedExecutionDate, _priority, _remittanceInformation);
    }

    /// <summary>Pins <c>requestedExecutionDate</c> to a fixed value.</summary>
    public AnyCreditTransfer WithRequestedExecutionDate(DateOnly value) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, new FixedValue<DateOnly>(value), _priority, _remittanceInformation);
    }

    /// <summary>Draws <c>requestedExecutionDate</c> from <c>AnyValidRequestedExecutionDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithRequestedExecutionDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, configure(AnyValidRequestedExecutionDate()), _priority, _remittanceInformation);
    }

    /// <summary>Pins <c>priority</c> to a fixed value.</summary>
    public AnyCreditTransfer WithPriority(TransferPriority value) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, _requestedExecutionDate, new FixedValue<TransferPriority>(value), _remittanceInformation);
    }

    /// <summary>Draws <c>priority</c> from <c>AnyValidPriority()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithPriority(Func<AnyEnum<TransferPriority>, AnyEnum<TransferPriority>> configure) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, _requestedExecutionDate, configure(AnyValidPriority()), _remittanceInformation);
    }

    /// <summary>Pins <c>remittanceInformation</c> to a fixed value.</summary>
    public AnyCreditTransfer WithRemittanceInformation(RemittanceInformation value) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, _requestedExecutionDate, _priority, new FixedValue<RemittanceInformation>(value));
    }

    /// <summary>Draws <c>remittanceInformation</c> from <c>AnyValidRemittanceInformation()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditTransfer WithRemittanceInformation(Func<AnyRemittanceInformation, AnyRemittanceInformation> configure) {
        return new AnyCreditTransfer(_endToEndId, _debtor, _creditor, _amount, _requestedExecutionDate, _priority, configure(AnyValidRemittanceInformation()));
    }

    /// <summary>Produces one arbitrary <see cref="CreditTransfer" />.</summary>
    public CreditTransfer Generate() {
        return new CreditTransfer(_endToEndId.Generate(),
                                  _debtor.Generate(),
                                  _creditor.Generate(),
                                  _amount.Generate(),
                                  _requestedExecutionDate.Generate(),
                                  _priority.Generate(),
                                  _remittanceInformation.Generate());
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
