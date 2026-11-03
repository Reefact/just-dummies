// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SupplementaryPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Clinical.Domain;
using Insurance.Domain;
using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SupplementaryPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySupplementaryPolicy : IAny<SupplementaryPolicy> {

    private IAny<PolicyNumber>          _number;
    private IAny<Patient>               _insured;
    private IAny<CoveragePeriod>        _period;
    private IAny<Deductible>            _deductible;
    private IAny<Money>                 _annualCeiling;
    private IAny<ReimbursementSchedule> _schedule;
    private IAny<Party>                 _payee;

    private AnyPolicyNumber          _numberRecipe;
    private AnyPatient               _insuredRecipe;
    private AnyCoveragePeriod        _periodRecipe;
    private AnyDeductible            _deductibleRecipe;
    private AnyMoney                 _annualCeilingRecipe;
    private AnyReimbursementSchedule _scheduleRecipe;
    private AnyParty                 _payeeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySupplementaryPolicy()
        : this(number:        AnyValidNumber(),
               insured:       AnyValidInsured(),
               period:        AnyValidPeriod(),
               deductible:    AnyValidDeductible(),
               annualCeiling: AnyValidAnnualCeiling(),
               schedule:      AnyValidSchedule(),
               payee:         AnyValidPayee()) { }

    private static AnyPolicyNumber AnyValidNumber() {
        return new AnyPolicyNumber();
    }

    private static AnyPatient AnyValidInsured() {
        return new AnyPatient();
    }

    private static AnyCoveragePeriod AnyValidPeriod() {
        return new AnyCoveragePeriod();
    }

    private static AnyDeductible AnyValidDeductible() {
        return new AnyDeductible();
    }

    private static AnyMoney AnyValidAnnualCeiling() {
        return new AnyMoney().WithCurrency(Currency.EUR)
                             .WithAmount(amount => amount.GreaterThanOrEqualTo(100m).LessThanOrEqualTo(SupplementaryPolicy.MaximumAnnualCeiling));
    }

    private static AnyReimbursementSchedule AnyValidSchedule() {
        return new AnyReimbursementSchedule();
    }

    private static AnyParty AnyValidPayee() {
        return new AnyParty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySupplementaryPolicy(AnyPolicyNumber          number,
                                   AnyPatient               insured,
                                   AnyCoveragePeriod        period,
                                   AnyDeductible            deductible,
                                   AnyMoney                 annualCeiling,
                                   AnyReimbursementSchedule schedule,
                                   AnyParty                 payee) {
        _number              = number;
        _numberRecipe        = number;
        _insured             = insured;
        _insuredRecipe       = insured;
        _period              = period;
        _periodRecipe        = period;
        _deductible          = deductible;
        _deductibleRecipe    = deductible;
        _annualCeiling       = annualCeiling;
        _annualCeilingRecipe = annualCeiling;
        _schedule            = schedule;
        _scheduleRecipe      = schedule;
        _payee               = payee;
        _payeeRecipe         = payee;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySupplementaryPolicy(AnySupplementaryPolicy from) {
        _number              = from._number;
        _numberRecipe        = from._numberRecipe;
        _insured             = from._insured;
        _insuredRecipe       = from._insuredRecipe;
        _period              = from._period;
        _periodRecipe        = from._periodRecipe;
        _deductible          = from._deductible;
        _deductibleRecipe    = from._deductibleRecipe;
        _annualCeiling       = from._annualCeiling;
        _annualCeilingRecipe = from._annualCeilingRecipe;
        _schedule            = from._schedule;
        _scheduleRecipe      = from._scheduleRecipe;
        _payee               = from._payee;
        _payeeRecipe         = from._payeeRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithNumber(PolicyNumber value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPolicyNumber, AnyPolicyNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithNumber(Func<AnyPolicyNumber, AnyPolicyNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>insured</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithInsured(Patient value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._insured = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>insured</c> is drawn from — <c>AnyValidInsured()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPatient, AnyPatient&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithInsured(Func<AnyPatient, AnyPatient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._insuredRecipe = configure(_insuredRecipe);
        copy._insured       = copy._insuredRecipe;

        return copy;
    }

    /// <summary>Pins <c>period</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithPeriod(CoveragePeriod value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._period = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>period</c> is drawn from — <c>AnyValidPeriod()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCoveragePeriod, AnyCoveragePeriod&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithPeriod(Func<AnyCoveragePeriod, AnyCoveragePeriod> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._periodRecipe = configure(_periodRecipe);
        copy._period       = copy._periodRecipe;

        return copy;
    }

    /// <summary>Pins <c>deductible</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithDeductible(Deductible value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._deductible = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>deductible</c> is drawn from — <c>AnyValidDeductible()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDeductible, AnyDeductible&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithDeductible(Func<AnyDeductible, AnyDeductible> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._deductibleRecipe = configure(_deductibleRecipe);
        copy._deductible       = copy._deductibleRecipe;

        return copy;
    }

    /// <summary>Pins <c>annualCeiling</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithAnnualCeiling(Money value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._annualCeiling = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>annualCeiling</c> is drawn from — <c>AnyValidAnnualCeiling()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithAnnualCeiling(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._annualCeilingRecipe = configure(_annualCeilingRecipe);
        copy._annualCeiling       = copy._annualCeilingRecipe;

        return copy;
    }

    /// <summary>Pins <c>schedule</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithSchedule(ReimbursementSchedule value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._schedule = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>schedule</c> is drawn from — <c>AnyValidSchedule()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyReimbursementSchedule, AnyReimbursementSchedule&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithSchedule(Func<AnyReimbursementSchedule, AnyReimbursementSchedule> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._scheduleRecipe = configure(_scheduleRecipe);
        copy._schedule       = copy._scheduleRecipe;

        return copy;
    }

    /// <summary>Pins <c>payee</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySupplementaryPolicy WithPayee(Party value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._payee = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>payee</c> is drawn from — <c>AnyValidPayee()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyParty, AnyParty&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySupplementaryPolicy WithPayee(Func<AnyParty, AnyParty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._payeeRecipe = configure(_payeeRecipe);
        copy._payee       = copy._payeeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SupplementaryPolicy" />.</summary>
    public SupplementaryPolicy Generate() {
        Money annualCeiling = _annualCeiling.Generate();

        // A deductible stays strictly below the ceiling: unless a test pinned it, draw one that does.
        IAny<Deductible> deductible = ReferenceEquals(_deductible, _deductibleRecipe)
            ? _deductibleRecipe.WithAmount(amount => amount.LessThan(annualCeiling.Amount))
            : _deductible;

        return new SupplementaryPolicy(_number.Generate(),
                                       _insured.Generate(),
                                       _period.Generate(),
                                       deductible.Generate(),
                                       annualCeiling,
                                       _schedule.Generate(),
                                       _payee.Generate());
    }

}
