// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Payslip --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<EmploymentContract>         _contract;
    private IAny<PayPeriod>?                 _period;
    private IAny<decimal>                    _hoursWorked;
    private IAny<IReadOnlyList<PayslipLine>> _lines;

    private AnyEmploymentContract _contractRecipe;
    private AnyDecimal            _hoursWorkedRecipe;
    private AnyList<PayslipLine>  _linesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayslip()
        : this(contract:    AnyValidContract(),
               hoursWorked: AnyValidHoursWorked(),
               lines:       AnyValidLines()) { }

    private static AnyEmploymentContract AnyValidContract() {
        return new AnyEmploymentContract();
    }

    private static AnyDecimal AnyValidHoursWorked() {
        return Any.Decimal().Between(0m, 300m);
    }

    private static AnyList<PayslipLine> AnyValidLines() {
        return Any.ListOf(new AnyPayslipLine());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPayslip(AnyEmploymentContract contract,
                       AnyDecimal            hoursWorked,
                       AnyList<PayslipLine>  lines) {
        _contract          = contract;
        _contractRecipe    = contract;
        _hoursWorked       = hoursWorked;
        _hoursWorkedRecipe = hoursWorked;
        _lines             = lines;
        _linesRecipe       = lines;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPayslip(AnyPayslip from) {
        _contract          = from._contract;
        _contractRecipe    = from._contractRecipe;
        _period            = from._period;
        _hoursWorked       = from._hoursWorked;
        _hoursWorkedRecipe = from._hoursWorkedRecipe;
        _lines             = from._lines;
        _linesRecipe       = from._linesRecipe;
    }

    /// <summary>Pins <c>contract</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslip WithContract(EmploymentContract value) {
        AnyPayslip copy = new AnyPayslip(this);

        copy._contract = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>contract</c> is drawn from — <c>AnyValidContract()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEmploymentContract, AnyEmploymentContract&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslip WithContract(Func<AnyEmploymentContract, AnyEmploymentContract> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslip copy = new AnyPayslip(this);

        copy._contractRecipe = configure(_contractRecipe);
        copy._contract       = copy._contractRecipe;

        return copy;
    }

    /// <summary>Pins <c>period</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslip WithPeriod(PayPeriod value) {
        AnyPayslip copy = new AnyPayslip(this);

        copy._period = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>hoursWorked</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslip WithHoursWorked(decimal value) {
        AnyPayslip copy = new AnyPayslip(this);

        copy._hoursWorked = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hoursWorked</c> is drawn from — <c>AnyValidHoursWorked()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslip WithHoursWorked(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslip copy = new AnyPayslip(this);

        copy._hoursWorkedRecipe = configure(_hoursWorkedRecipe);
        copy._hoursWorked       = copy._hoursWorkedRecipe;

        return copy;
    }

    /// <summary>Pins <c>lines</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslip WithLines(IReadOnlyList<PayslipLine> value) {
        AnyPayslip copy = new AnyPayslip(this);

        copy._lines = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lines</c> is drawn from — <c>AnyValidLines()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;PayslipLine&gt;, AnyList&lt;PayslipLine&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslip WithLines(Func<AnyList<PayslipLine>, AnyList<PayslipLine>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslip copy = new AnyPayslip(this);

        copy._linesRecipe = configure(_linesRecipe);
        copy._lines       = copy._linesRecipe;

        return copy;
    }

    /// <summary>
    ///     Produces one arbitrary <see cref="Payslip" />. Unless <c>WithPeriod</c> pinned it, the period is
    ///     drawn after the contract, in a month that does not end before the hiring date.
    /// </summary>
    public Payslip Generate() {
        EmploymentContract contract = _contract.Generate();
        PayPeriod          period   = _period is not null ? _period.Generate() : AnyPeriodNotBefore(contract).Generate();

        return new Payslip(contract,
                           period,
                           _hoursWorked.Generate(),
                           _lines.Generate());
    }

    private static IAny<PayPeriod> AnyPeriodNotBefore(EmploymentContract contract) {
        DateOnly earliest = contract.HiredOn > new DateOnly(2000, 1, 1) ? contract.HiredOn : new DateOnly(2000, 1, 1);

        return Any.DateOnly().Between(earliest, new DateOnly(2100, 12, 31)).As(day => new PayPeriod(day.Year, day.Month));
    }

}
