// Scaffolded by `any`, then rewritten by hand where the constructor ties parameters together
// (see the comments on Generate()). `any generate SupplementaryPolicy --force` would overwrite it.

using System;

using Clinical.Domain;
using Insurance.Domain;
using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Domain;

/// <summary>A generator of arbitrary <see cref="SupplementaryPolicy" /> values.</summary>
public sealed partial class AnySupplementaryPolicy : IAny<SupplementaryPolicy> {

    private IAny<PolicyNumber>          _number;
    private IAny<Patient>               _insured;
    private IAny<CoveragePeriod>        _period;
    private IAny<ReimbursementSchedule> _schedule;
    private IAny<Party>                 _payee;
    private decimal?                    _deductible;
    private decimal?                    _annualCeiling;

    private AnyPolicyNumber          _numberRecipe;
    private AnyPatient               _insuredRecipe;
    private AnyCoveragePeriod        _periodRecipe;
    private AnyReimbursementSchedule _scheduleRecipe;
    private AnyParty                 _payeeRecipe;

    public AnySupplementaryPolicy()
        : this(number:   AnyValidNumber(),
               insured:  new AnyPatient(),
               period:   new AnyCoveragePeriod(),
               schedule: new AnyReimbursementSchedule(),
               payee:    new AnyParty()) { }

    // The year carried by the number is a floor for the period's start; the default periods lie in 2026.
    private static AnyPolicyNumber AnyValidNumber() {
        return new AnyPolicyNumber().IssuedBetween(2015, 2025);
    }

    private AnySupplementaryPolicy(AnyPolicyNumber          number,
                                   AnyPatient               insured,
                                   AnyCoveragePeriod        period,
                                   AnyReimbursementSchedule schedule,
                                   AnyParty                 payee) {
        _number         = number;
        _numberRecipe   = number;
        _insured        = insured;
        _insuredRecipe  = insured;
        _period         = period;
        _periodRecipe   = period;
        _schedule       = schedule;
        _scheduleRecipe = schedule;
        _payee          = payee;
        _payeeRecipe    = payee;
    }

    private AnySupplementaryPolicy(AnySupplementaryPolicy from) {
        _number         = from._number;
        _numberRecipe   = from._numberRecipe;
        _insured        = from._insured;
        _insuredRecipe  = from._insuredRecipe;
        _period         = from._period;
        _periodRecipe   = from._periodRecipe;
        _schedule       = from._schedule;
        _scheduleRecipe = from._scheduleRecipe;
        _payee          = from._payee;
        _payeeRecipe    = from._payeeRecipe;
        _deductible     = from._deductible;
        _annualCeiling  = from._annualCeiling;
    }

    public AnySupplementaryPolicy WithNumber(PolicyNumber value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    public AnySupplementaryPolicy WithNumber(Func<AnyPolicyNumber, AnyPolicyNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    public AnySupplementaryPolicy WithInsured(Patient value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._insured = Any.Fixed(value);

        return copy;
    }

    public AnySupplementaryPolicy WithInsured(Func<AnyPatient, AnyPatient> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._insuredRecipe = configure(_insuredRecipe);
        copy._insured       = copy._insuredRecipe;

        return copy;
    }

    public AnySupplementaryPolicy WithPeriod(CoveragePeriod value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._period = Any.Fixed(value);

        return copy;
    }

    public AnySupplementaryPolicy WithPeriod(Func<AnyCoveragePeriod, AnyCoveragePeriod> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._periodRecipe = configure(_periodRecipe);
        copy._period       = copy._periodRecipe;

        return copy;
    }

    /// <summary>Pins the deductible, in euros; the ceiling follows it unless it is pinned too.</summary>
    public AnySupplementaryPolicy WithDeductible(decimal euros) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._deductible = euros;

        return copy;
    }

    /// <summary>Pins the annual ceiling, in euros; the deductible follows it unless it is pinned too.</summary>
    public AnySupplementaryPolicy WithAnnualCeiling(decimal euros) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._annualCeiling = euros;

        return copy;
    }

    public AnySupplementaryPolicy WithSchedule(ReimbursementSchedule value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._schedule = Any.Fixed(value);

        return copy;
    }

    public AnySupplementaryPolicy WithPayee(Party value) {
        AnySupplementaryPolicy copy = new AnySupplementaryPolicy(this);

        copy._payee = Any.Fixed(value);

        return copy;
    }

    public SupplementaryPolicy Generate() {
        // The deductible lies strictly below the ceiling: the one tie the scaffolder cannot see, since the two
        // parameters are a Deductible and a Money. Any.OrderedPair draws them in that order by construction.
        AnyOrderedPair<decimal> deductibleAndCeiling = Any.OrderedPair(Any.Decimal().Between(0m, Deductible.Ceiling).WithScale(2),
                                                                       Any.Decimal().Between(0.01m, SupplementaryPolicy.MaximumAnnualCeiling).WithScale(2),
                                                                       strict: true);
        if (_deductible is { } deductibleEuros) { deductibleAndCeiling = deductibleAndCeiling.WithFirst(deductibleEuros); }
        if (_annualCeiling is { } ceilingEuros) { deductibleAndCeiling = deductibleAndCeiling.WithSecond(ceilingEuros); }
        (decimal deductible, decimal ceiling) = deductibleAndCeiling.Generate();

        return new SupplementaryPolicy(_number.Generate(),
                                       _insured.Generate(),
                                       _period.Generate(),
                                       new Deductible(deductible),
                                       new Money(ceiling, Currency.EUR),
                                       _schedule.Generate(),
                                       _payee.Generate());
    }

}
