// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ContractualFlow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ContractualFlow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyContractualFlow : IAny<ContractualFlow> {

    private readonly IAny<string>        _flowId;
    private readonly IAny<DateOnly>      _valueDate;
    private readonly IAny<decimal>       _amount;
    private readonly IAny<Currency>      _currency;
    private readonly IAny<FlowDirection> _direction;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyContractualFlow()
        : this(flowId:    AnyValidFlowId(),
               valueDate: AnyValidValueDate(),
               amount:    AnyValidAmount(),
               currency:  AnyValidCurrency(),
               direction: AnyValidDirection()) { }

    private static AnyString AnyValidFlowId() {
        return Any.String().NotBlank();
    }

    private static AnyDateOnly AnyValidValueDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2099, 12, 31));
    }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyEnum<FlowDirection> AnyValidDirection() {
        return Any.Enum<FlowDirection>();
    }

    private AnyContractualFlow(IAny<string>        flowId,
                               IAny<DateOnly>      valueDate,
                               IAny<decimal>       amount,
                               IAny<Currency>      currency,
                               IAny<FlowDirection> direction) {
        _flowId    = flowId;
        _valueDate = valueDate;
        _amount    = amount;
        _currency  = currency;
        _direction = direction;
    }

    /// <summary>Pins <c>flowId</c> to a fixed value.</summary>
    public AnyContractualFlow WithFlowId(string value) {
        return new AnyContractualFlow(new FixedValue<string>(value), _valueDate, _amount, _currency, _direction);
    }

    /// <summary>Draws <c>flowId</c> from <c>AnyValidFlowId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContractualFlow WithFlowId(Func<AnyString, AnyString> configure) {
        return new AnyContractualFlow(configure(AnyValidFlowId()), _valueDate, _amount, _currency, _direction);
    }

    /// <summary>Pins <c>valueDate</c> to a fixed value.</summary>
    public AnyContractualFlow WithValueDate(DateOnly value) {
        return new AnyContractualFlow(_flowId, new FixedValue<DateOnly>(value), _amount, _currency, _direction);
    }

    /// <summary>Draws <c>valueDate</c> from <c>AnyValidValueDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContractualFlow WithValueDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyContractualFlow(_flowId, configure(AnyValidValueDate()), _amount, _currency, _direction);
    }

    /// <summary>Pins <c>amount</c> to a fixed value.</summary>
    public AnyContractualFlow WithAmount(decimal value) {
        return new AnyContractualFlow(_flowId, _valueDate, new FixedValue<decimal>(value), _currency, _direction);
    }

    /// <summary>Draws <c>amount</c> from <c>AnyValidAmount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContractualFlow WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyContractualFlow(_flowId, _valueDate, configure(AnyValidAmount()), _currency, _direction);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyContractualFlow WithCurrency(Currency value) {
        return new AnyContractualFlow(_flowId, _valueDate, _amount, new FixedValue<Currency>(value), _direction);
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContractualFlow WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyContractualFlow(_flowId, _valueDate, _amount, configure(AnyValidCurrency()), _direction);
    }

    /// <summary>Pins <c>direction</c> to a fixed value.</summary>
    public AnyContractualFlow WithDirection(FlowDirection value) {
        return new AnyContractualFlow(_flowId, _valueDate, _amount, _currency, new FixedValue<FlowDirection>(value));
    }

    /// <summary>Draws <c>direction</c> from <c>AnyValidDirection()</c>, refined by <paramref name="configure" />.</summary>
    public AnyContractualFlow WithDirection(Func<AnyEnum<FlowDirection>, AnyEnum<FlowDirection>> configure) {
        return new AnyContractualFlow(_flowId, _valueDate, _amount, _currency, configure(AnyValidDirection()));
    }

    /// <summary>Produces one arbitrary <see cref="ContractualFlow" />.</summary>
    public ContractualFlow Generate() {
        return new ContractualFlow(_flowId.Generate(),
                                   _valueDate.Generate(),
                                   _amount.Generate(),
                                   _currency.Generate(),
                                   _direction.Generate());
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
