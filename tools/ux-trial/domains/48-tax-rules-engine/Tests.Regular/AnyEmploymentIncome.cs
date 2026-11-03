// Scaffolded by `any` (JustDummies), then completed by hand: the deduction never exceeds the gross salary,
// so it is drawn inside Generate() once the gross is known.

using System;

using JustDummies;

namespace TaxRules.Domain;

public sealed partial class AnyEmploymentIncome : IAny<EmploymentIncome> {

    private IAny<string> _employer;
    private IAny<Money>  _gross;
    private Money?       _pinnedDeduction;

    private AnyString _employerRecipe;
    private AnyMoney  _grossRecipe;

    public AnyEmploymentIncome()
        : this(employer: Dummies.Source(),
               gross:    Dummies.IncomeMoney()) { }

    private AnyEmploymentIncome(AnyString employer, AnyMoney gross) {
        _employer       = employer;
        _employerRecipe = employer;
        _gross          = gross;
        _grossRecipe    = gross;
    }

    private AnyEmploymentIncome(AnyEmploymentIncome from) {
        _employer        = from._employer;
        _employerRecipe  = from._employerRecipe;
        _gross           = from._gross;
        _grossRecipe     = from._grossRecipe;
        _pinnedDeduction = from._pinnedDeduction;
    }

    public AnyEmploymentIncome WithEmployer(string value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._employer = Any.Fixed(value);

        return copy;
    }

    public AnyEmploymentIncome WithGross(Money value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._gross = Any.Fixed(value);

        return copy;
    }

    public AnyEmploymentIncome WithExpenseDeduction(Money value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._pinnedDeduction = value;

        return copy;
    }

    public EmploymentIncome Generate() {
        Money gross     = _gross.Generate();
        Money deduction = _pinnedDeduction ?? new Money(Any.Decimal().Between(0m, gross.Amount).WithScale(2).Generate());

        return new EmploymentIncome(_employer.Generate(), gross, deduction);
    }

}
