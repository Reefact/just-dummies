// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SelfEmploymentIncome --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace TaxRules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SelfEmploymentIncome" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySelfEmploymentIncome : IAny<SelfEmploymentIncome> {

    private IAny<string> _activity;
    private IAny<Money>  _turnover;
    private IAny<Money>  _expenses;

    private AnyString _activityRecipe;
    private AnyMoney  _turnoverRecipe;
    private AnyMoney  _expensesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySelfEmploymentIncome()
        : this(activity: AnyValidActivity(),
               turnover: AnyValidTurnover(),
               expenses: AnyValidExpenses()) { }

    private static AnyString AnyValidActivity() {
        return Any.String().NotBlank().WithMaxLength(IncomeComponent.MaxSourceLength);
    }

    private static AnyMoney AnyValidTurnover() {
        return Plausible.Amount();
    }

    private static AnyMoney AnyValidExpenses() {
        return Plausible.Amount();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySelfEmploymentIncome(AnyString activity,
                                    AnyMoney  turnover,
                                    AnyMoney  expenses) {
        _activity       = activity;
        _activityRecipe = activity;
        _turnover       = turnover;
        _turnoverRecipe = turnover;
        _expenses       = expenses;
        _expensesRecipe = expenses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySelfEmploymentIncome(AnySelfEmploymentIncome from) {
        _activity       = from._activity;
        _activityRecipe = from._activityRecipe;
        _turnover       = from._turnover;
        _turnoverRecipe = from._turnoverRecipe;
        _expenses       = from._expenses;
        _expensesRecipe = from._expensesRecipe;
    }

    /// <summary>Pins <c>activity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySelfEmploymentIncome WithActivity(string value) {
        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._activity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>activity</c> is drawn from — <c>AnyValidActivity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySelfEmploymentIncome WithActivity(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._activityRecipe = configure(_activityRecipe);
        copy._activity       = copy._activityRecipe;

        return copy;
    }

    /// <summary>Pins <c>turnover</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySelfEmploymentIncome WithTurnover(Money value) {
        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._turnover = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>turnover</c> is drawn from — <c>AnyValidTurnover()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySelfEmploymentIncome WithTurnover(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._turnoverRecipe = configure(_turnoverRecipe);
        copy._turnover       = copy._turnoverRecipe;

        return copy;
    }

    /// <summary>Pins <c>expenses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySelfEmploymentIncome WithExpenses(Money value) {
        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._expenses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>expenses</c> is drawn from — <c>AnyValidExpenses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySelfEmploymentIncome WithExpenses(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySelfEmploymentIncome copy = new AnySelfEmploymentIncome(this);

        copy._expensesRecipe = configure(_expensesRecipe);
        copy._expenses       = copy._expensesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SelfEmploymentIncome" />.</summary>
    public SelfEmploymentIncome Generate() {
        return new SelfEmploymentIncome(_activity.Generate(),
                                        _turnover.Generate(),
                                        _expenses.Generate());
    }

}
