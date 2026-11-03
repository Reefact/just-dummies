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
using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CalibratedDate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibratedDate : IAny<CalibratedDate> {

    private const int MaximumSpan = 120;

    private IAny<int>                   _firstYear;
    private IAny<IReadOnlyList<double>> _probabilities;

    private AnyInt32        _firstYearRecipe;
    private AnyList<double> _probabilitiesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibratedDate()
        : this(firstYear:     AnyValidFirstYear(),
               probabilities: AnyValidProbabilities()) { }

    private static AnyInt32 AnyValidFirstYear() {
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear - (MaximumSpan - 1));
    }

    private static AnyList<double> AnyValidProbabilities() {
        // Positive weights, normalised in Generate: the constructor asks for a sum of 1 that no per-item recipe can give.
        return Any.ListOf(Any.Double().Between(0.001, 1d)).WithCountBetween(1, MaximumSpan);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCalibratedDate(AnyInt32        firstYear,
                              AnyList<double> probabilities) {
        _firstYear           = firstYear;
        _firstYearRecipe     = firstYear;
        _probabilities       = probabilities;
        _probabilitiesRecipe = probabilities;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCalibratedDate(AnyCalibratedDate from) {
        _firstYear           = from._firstYear;
        _firstYearRecipe     = from._firstYearRecipe;
        _probabilities       = from._probabilities;
        _probabilitiesRecipe = from._probabilitiesRecipe;
    }

    /// <summary>Pins <c>firstYear</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibratedDate WithFirstYear(int value) {
        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._firstYear = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstYear</c> is drawn from — <c>AnyValidFirstYear()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibratedDate WithFirstYear(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._firstYearRecipe = configure(_firstYearRecipe);
        copy._firstYear       = copy._firstYearRecipe;

        return copy;
    }

    /// <summary>Pins <c>probabilities</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCalibratedDate WithProbabilities(IReadOnlyList<double> value) {
        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._probabilities = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>probabilities</c> is drawn from — <c>AnyValidProbabilities()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;double&gt;, AnyList&lt;double&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCalibratedDate WithProbabilities(Func<AnyList<double>, AnyList<double>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCalibratedDate copy = new AnyCalibratedDate(this);

        copy._probabilitiesRecipe = configure(_probabilitiesRecipe);
        copy._probabilities       = copy._probabilitiesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CalibratedDate" />.</summary>
    public CalibratedDate Generate() {
        IReadOnlyList<double> weights = _probabilities.Generate();
        double                total   = weights.Sum();
        double[]              normalised = weights.Select(weight => weight / total).ToArray();

        // The constructor ties the span to the calendar range: slide the first year back if the span overruns it.
        int firstYear = Math.Min(_firstYear.Generate(), CalendarInterval.LatestYear - normalised.Length + 1);

        return new CalibratedDate(firstYear, normalised);
    }

}
