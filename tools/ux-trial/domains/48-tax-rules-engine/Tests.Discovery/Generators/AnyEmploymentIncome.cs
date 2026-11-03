// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EmploymentIncome --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EmploymentIncome" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmploymentIncome : IAny<EmploymentIncome> {

    private IAny<string> _employer;
    private IAny<Money>  _gross;
    private IAny<Money>  _expenseDeduction;

    private AnyString _employerRecipe;
    private AnyMoney  _grossRecipe;
    private AnyMoney  _expenseDeductionRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmploymentIncome()
        : this(employer:         AnyValidEmployer(),
               gross:            AnyValidGross(),
               expenseDeduction: AnyValidExpenseDeduction()) { }

    private static AnyString AnyValidEmployer() {
        return Any.String().NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength);
    }

    private static AnyMoney AnyValidGross() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(1_000_000m));
    }

    private static AnyMoney AnyValidExpenseDeduction() {
        return new AnyMoney().WithAmount(a => a.LessThanOrEqualTo(1_000_000m));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEmploymentIncome(AnyString employer,
                                AnyMoney  gross,
                                AnyMoney  expenseDeduction) {
        _employer               = employer;
        _employerRecipe         = employer;
        _gross                  = gross;
        _grossRecipe            = gross;
        _expenseDeduction       = expenseDeduction;
        _expenseDeductionRecipe = expenseDeduction;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEmploymentIncome(AnyEmploymentIncome from) {
        _employer               = from._employer;
        _employerRecipe         = from._employerRecipe;
        _gross                  = from._gross;
        _grossRecipe            = from._grossRecipe;
        _expenseDeduction       = from._expenseDeduction;
        _expenseDeductionRecipe = from._expenseDeductionRecipe;
    }

    /// <summary>Pins <c>employer</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentIncome WithEmployer(string value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._employer = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>employer</c> is drawn from — <c>AnyValidEmployer()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentIncome WithEmployer(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._employerRecipe = configure(_employerRecipe);
        copy._employer       = copy._employerRecipe;

        return copy;
    }

    /// <summary>Pins <c>gross</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentIncome WithGross(Money value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._gross = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gross</c> is drawn from — <c>AnyValidGross()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentIncome WithGross(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._grossRecipe = configure(_grossRecipe);
        copy._gross       = copy._grossRecipe;

        return copy;
    }

    /// <summary>Pins <c>expenseDeduction</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmploymentIncome WithExpenseDeduction(Money value) {
        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._expenseDeduction = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>expenseDeduction</c> is drawn from — <c>AnyValidExpenseDeduction()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmploymentIncome WithExpenseDeduction(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmploymentIncome copy = new AnyEmploymentIncome(this);

        copy._expenseDeductionRecipe = configure(_expenseDeductionRecipe);
        copy._expenseDeduction       = copy._expenseDeductionRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="EmploymentIncome" />.</summary>
    public EmploymentIncome Generate() {
        // expenseDeduction <= gross ties two parameters: a deduction drawn above the gross is redrawn between zero and it.
        // (A pinned WithExpenseDeduction larger than the gross is therefore replaced rather than refused.)
        Money gross     = _gross.Generate();
        Money deduction = _expenseDeduction.Generate();

        if (deduction.CompareTo(gross) > 0) {
            deduction = new Money(Any.Decimal().Between(0m, gross.Amount).WithScale(2).Generate());
        }

        return new EmploymentIncome(_employer.Generate(), gross, deduction);
    }

}
