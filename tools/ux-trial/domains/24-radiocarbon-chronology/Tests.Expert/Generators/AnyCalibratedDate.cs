// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalibratedDate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalibratedDate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibratedDate : IAny<CalibratedDate> {

    private IAny<IReadOnlyList<double>> _probabilities;

    private AnyInt32 _firstYearRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibratedDate()
        : this(firstYear:     AnyValidFirstYear(),
               probabilities: AnyValidProbabilities()) { }

    private static AnyInt32 AnyValidFirstYear() {
        return Any.Int32().GreaterThanOrEqualTo(CalendarInterval.EarliestYear);
    }

    private static IAny<IReadOnlyList<double>> AnyValidProbabilities() {
        // One probability per year, each in [0, 1], summing to 1: positive weights, normalised.
        return Any.ListOf(Any.Double().Between(0.001, 1d))
                  .WithCountBetween(1, 60)
                  .As(weights => (IReadOnlyList<double>)weights.Select(weight => weight / weights.Sum()).ToList());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalibratedDate(AnyInt32                     firstYear,
                              IAny<IReadOnlyList<double>> probabilities) {
        _firstYearRecipe = firstYear;
        _probabilities   = probabilities;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCalibratedDate(AnyCalibratedDate from) {
        _firstYearRecipe = from._firstYearRecipe;
        _probabilities   = from._probabilities;
    }

    /// <summary>Pins <c>firstYear</c>. It is applied to the recipe, since Generate() bounds it by the span the probabilities cover.</summary>
    public AnyCalibratedDate WithFirstYear(int value) {
        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._firstYearRecipe = _firstYearRecipe.OneOf(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstYear</c> is drawn from — <c>AnyValidFirstYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    public AnyCalibratedDate WithFirstYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._firstYearRecipe = configure(_firstYearRecipe);

        return copy;
    }

    /// <summary>Pins <c>probabilities</c> to <paramref name="value" />, which every draw hands over as it is.</summary>
    public AnyCalibratedDate WithProbabilities(IReadOnlyList<double> value) {
        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._probabilities = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CalibratedDate" />.</summary>
    public CalibratedDate Generate() {
        IReadOnlyList<double> probabilities = _probabilities.Generate();

        // Written by hand: the span the probabilities cover must fit the calendar, so firstYear is drawn after them.
        int firstYear = _firstYearRecipe.LessThanOrEqualTo(CalendarInterval.LatestYear - probabilities.Count + 1).Generate();

        return new CalibratedDate(firstYear, probabilities);
    }

}
