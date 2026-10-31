// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NonMaturityDeposit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="NonMaturityDeposit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNonMaturityDeposit : IAny<NonMaturityDeposit> {

    private readonly IAny<string>                _depositId;
    private readonly IAny<decimal>               _balance;
    private readonly IAny<Currency>              _currency;
    private readonly IAny<BehaviouralAssumption> _assumption;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNonMaturityDeposit()
        : this(depositId:  AnyValidDepositId(),
               balance:    AnyValidBalance(),
               currency:   AnyValidCurrency(),
               assumption: AnyValidAssumption()) { }

    private static AnyString AnyValidDepositId() {
        return Any.String().NotBlank();
    }

    private static AnyDecimal AnyValidBalance() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyCurrency AnyValidCurrency() {
        return new AnyCurrency();
    }

    private static AnyBehaviouralAssumption AnyValidAssumption() {
        return new AnyBehaviouralAssumption();
    }

    private AnyNonMaturityDeposit(IAny<string>                depositId,
                                  IAny<decimal>               balance,
                                  IAny<Currency>              currency,
                                  IAny<BehaviouralAssumption> assumption) {
        _depositId  = depositId;
        _balance    = balance;
        _currency   = currency;
        _assumption = assumption;
    }

    /// <summary>Pins <c>depositId</c> to a fixed value.</summary>
    public AnyNonMaturityDeposit WithDepositId(string value) {
        return new AnyNonMaturityDeposit(new FixedValue<string>(value), _balance, _currency, _assumption);
    }

    /// <summary>Draws <c>depositId</c> from <c>AnyValidDepositId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNonMaturityDeposit WithDepositId(Func<AnyString, AnyString> configure) {
        return new AnyNonMaturityDeposit(configure(AnyValidDepositId()), _balance, _currency, _assumption);
    }

    /// <summary>Pins <c>balance</c> to a fixed value.</summary>
    public AnyNonMaturityDeposit WithBalance(decimal value) {
        return new AnyNonMaturityDeposit(_depositId, new FixedValue<decimal>(value), _currency, _assumption);
    }

    /// <summary>Draws <c>balance</c> from <c>AnyValidBalance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNonMaturityDeposit WithBalance(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyNonMaturityDeposit(_depositId, configure(AnyValidBalance()), _currency, _assumption);
    }

    /// <summary>Pins <c>currency</c> to a fixed value.</summary>
    public AnyNonMaturityDeposit WithCurrency(Currency value) {
        return new AnyNonMaturityDeposit(_depositId, _balance, new FixedValue<Currency>(value), _assumption);
    }

    /// <summary>Draws <c>currency</c> from <c>AnyValidCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNonMaturityDeposit WithCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyNonMaturityDeposit(_depositId, _balance, configure(AnyValidCurrency()), _assumption);
    }

    /// <summary>Pins <c>assumption</c> to a fixed value.</summary>
    public AnyNonMaturityDeposit WithAssumption(BehaviouralAssumption value) {
        return new AnyNonMaturityDeposit(_depositId, _balance, _currency, new FixedValue<BehaviouralAssumption>(value));
    }

    /// <summary>Draws <c>assumption</c> from <c>AnyValidAssumption()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNonMaturityDeposit WithAssumption(Func<AnyBehaviouralAssumption, AnyBehaviouralAssumption> configure) {
        return new AnyNonMaturityDeposit(_depositId, _balance, _currency, configure(AnyValidAssumption()));
    }

    /// <summary>Produces one arbitrary <see cref="NonMaturityDeposit" />.</summary>
    public NonMaturityDeposit Generate() {
        return new NonMaturityDeposit(_depositId.Generate(),
                                      _balance.Generate(),
                                      _currency.Generate(),
                                      _assumption.Generate());
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
