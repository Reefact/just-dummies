// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Payslip --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Payslip" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPayslip : IAny<Payslip> {

    private readonly IAny<EmploymentContract>         _contract;
    private readonly IAny<PayPeriod>                  _period;
    private readonly IAny<decimal>                    _hoursWorked;
    private readonly IAny<IReadOnlyList<PayslipLine>> _lines;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayslip()
        : this(contract:    AnyValidContract(),
               period:      AnyValidPeriod(),
               hoursWorked: AnyValidHoursWorked(),
               lines:       AnyValidLines()) { }

    private static AnyEmploymentContract AnyValidContract() {
        // A payslip cannot cover a month before the hiring date, and a period starts in 2000 at the
        // earliest: a contract signed before then is valid against every period AnyValidPeriod() draws.
        return new AnyEmploymentContract().WithHiredOn(h => h.Before(new DateOnly(2000, 1, 1)));
    }

    private static AnyPayPeriod AnyValidPeriod() {
        // The cross-field rule with the contract is kept by AnyValidContract().
        return new AnyPayPeriod();
    }

    private static AnyDecimal AnyValidHoursWorked() {
        return Any.Decimal().Between(0m, 300m);
    }

    private static AnyList<PayslipLine> AnyValidLines() {
        return Any.ListOf(new AnyPayslipLine());
    }

    private AnyPayslip(IAny<EmploymentContract>         contract,
                       IAny<PayPeriod>                  period,
                       IAny<decimal>                    hoursWorked,
                       IAny<IReadOnlyList<PayslipLine>> lines) {
        _contract    = contract;
        _period      = period;
        _hoursWorked = hoursWorked;
        _lines       = lines;
    }

    /// <summary>Pins <c>contract</c> to a fixed value.</summary>
    public AnyPayslip WithContract(EmploymentContract value) {
        return new AnyPayslip(new FixedValue<EmploymentContract>(value), _period, _hoursWorked, _lines);
    }

    /// <summary>Draws <c>contract</c> from <c>AnyValidContract()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslip WithContract(Func<AnyEmploymentContract, AnyEmploymentContract> configure) {
        return new AnyPayslip(configure(AnyValidContract()), _period, _hoursWorked, _lines);
    }

    /// <summary>Pins <c>period</c> to a fixed value.</summary>
    public AnyPayslip WithPeriod(PayPeriod value) {
        return new AnyPayslip(_contract, new FixedValue<PayPeriod>(value), _hoursWorked, _lines);
    }

    /// <summary>Draws <c>period</c> from <c>AnyValidPeriod()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslip WithPeriod(Func<AnyPayPeriod, AnyPayPeriod> configure) {
        return new AnyPayslip(_contract, configure(AnyValidPeriod()), _hoursWorked, _lines);
    }

    /// <summary>Pins <c>hoursWorked</c> to a fixed value.</summary>
    public AnyPayslip WithHoursWorked(decimal value) {
        return new AnyPayslip(_contract, _period, new FixedValue<decimal>(value), _lines);
    }

    /// <summary>Draws <c>hoursWorked</c> from <c>AnyValidHoursWorked()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslip WithHoursWorked(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyPayslip(_contract, _period, configure(AnyValidHoursWorked()), _lines);
    }

    /// <summary>Pins <c>lines</c> to a fixed value.</summary>
    public AnyPayslip WithLines(IReadOnlyList<PayslipLine> value) {
        return new AnyPayslip(_contract, _period, _hoursWorked, new FixedValue<IReadOnlyList<PayslipLine>>(value));
    }

    /// <summary>Draws <c>lines</c> from <c>AnyValidLines()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPayslip WithLines(Func<AnyList<PayslipLine>, AnyList<PayslipLine>> configure) {
        return new AnyPayslip(_contract, _period, _hoursWorked, configure(AnyValidLines()));
    }

    /// <summary>Produces one arbitrary <see cref="Payslip" />.</summary>
    public Payslip Generate() {
        return new Payslip(_contract.Generate(),
                           _period.Generate(),
                           _hoursWorked.Generate(),
                           _lines.Generate());
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
