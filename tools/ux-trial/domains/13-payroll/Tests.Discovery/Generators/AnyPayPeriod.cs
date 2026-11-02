// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PayPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PayPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPayPeriod : IAny<PayPeriod> {

    private IAny<int> _year;
    private IAny<int> _month;

    private AnyInt32 _yearRecipe;
    private AnyInt32 _monthRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayPeriod()
        : this(year:  AnyValidYear(),
               month: AnyValidMonth()) { }

    private static AnyInt32 AnyValidYear() {
        return Any.Int32().Between(2000, 2100);
    }

    private static AnyInt32 AnyValidMonth() {
        return Any.Int32().Positive().LessThanOrEqualTo(12);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPayPeriod(AnyInt32 year,
                         AnyInt32 month) {
        _year        = year;
        _yearRecipe  = year;
        _month       = month;
        _monthRecipe = month;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPayPeriod(AnyPayPeriod from) {
        _year        = from._year;
        _yearRecipe  = from._yearRecipe;
        _month       = from._month;
        _monthRecipe = from._monthRecipe;
    }

    /// <summary>Pins <c>year</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayPeriod WithYear(int value) {
        AnyPayPeriod copy = new AnyPayPeriod(this);

        copy._year = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>year</c> is drawn from — <c>AnyValidYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayPeriod WithYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayPeriod copy = new AnyPayPeriod(this);

        copy._yearRecipe = configure(_yearRecipe);
        copy._year       = copy._yearRecipe;

        return copy;
    }

    /// <summary>Pins <c>month</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayPeriod WithMonth(int value) {
        AnyPayPeriod copy = new AnyPayPeriod(this);

        copy._month = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>month</c> is drawn from — <c>AnyValidMonth()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayPeriod WithMonth(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayPeriod copy = new AnyPayPeriod(this);

        copy._monthRecipe = configure(_monthRecipe);
        copy._month       = copy._monthRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PayPeriod" />.</summary>
    public PayPeriod Generate() {
        return new PayPeriod(_year.Generate(),
                             _month.Generate());
    }

}
