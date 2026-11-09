// Scaffolded by `any` (JustDummies), then rewritten by hand: the scaffold left the amount and the value
// date `to verify` (positive, business day); here the base states both.

using System;

using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>A generator of arbitrary <see cref="SettlementInstruction" /> values: a positive amount, settling on a business day.</summary>
public sealed class AnySettlementInstruction : IAny<SettlementInstruction> {

    private readonly IAny<Lei>                 _counterparty;
    private readonly IAny<SettlementDirection> _direction;
    private readonly IAny<Money>               _amount;
    private readonly IAny<DateOnly>            _valueDate;

    public AnySettlementInstruction()
        : this(new AnyLei(), Any.Enum<SettlementDirection>(), new AnyMoney().Positive(), Days.Business()) { }

    private AnySettlementInstruction(IAny<Lei> counterparty, IAny<SettlementDirection> direction, IAny<Money> amount, IAny<DateOnly> valueDate) {
        _counterparty = counterparty;
        _direction    = direction;
        _amount       = amount;
        _valueDate    = valueDate;
    }

    public AnySettlementInstruction WithCounterparty(Lei value) {
        return new AnySettlementInstruction(Any.Fixed(value), _direction, _amount, _valueDate);
    }

    public AnySettlementInstruction WithDirection(SettlementDirection value) {
        return new AnySettlementInstruction(_counterparty, Any.Fixed(value), _amount, _valueDate);
    }

    public AnySettlementInstruction WithAmount(Money value) {
        return new AnySettlementInstruction(_counterparty, _direction, Any.Fixed(value), _valueDate);
    }

    public AnySettlementInstruction WithValueDate(DateOnly value) {
        return new AnySettlementInstruction(_counterparty, _direction, _amount, Any.Fixed(value));
    }

    public SettlementInstruction Generate() {
        return new SettlementInstruction(_counterparty.Generate(), _direction.Generate(), _amount.Generate(), _valueDate.Generate());
    }

}
