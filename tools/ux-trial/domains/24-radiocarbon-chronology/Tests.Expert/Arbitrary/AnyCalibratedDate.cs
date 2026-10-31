// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CalibratedDate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CalibratedDate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCalibratedDate : IAny<CalibratedDate> {

    private const int MaximumSpan = 200;

    private readonly IAny<int>                   _firstYear;
    private readonly IAny<IReadOnlyList<double>> _probabilities;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCalibratedDate()
        : this(firstYear:     AnyValidFirstYear(),
               probabilities: AnyValidProbabilities()) { }

    private static AnyInt32 AnyValidFirstYear() {
        // The span must end by 1950: starting early enough for the longest drawn distribution keeps it in range.
        return Any.Int32().Between(CalendarInterval.EarliestYear, CalendarInterval.LatestYear - (MaximumSpan - 1));
    }

    private static IAny<IReadOnlyList<double>> AnyValidProbabilities() {
        // Positive weights normalised to a total of one: the sum rule holds by construction.
        return Any.ListOf(Any.Double().Between(0.01, 1.0))
                  .WithCountBetween(1, MaximumSpan)
                  .As(weights => {
                      double total = weights.Sum();
                      return (IReadOnlyList<double>)weights.Select(weight => weight / total).ToList();
                  });
    }

    private AnyCalibratedDate(IAny<int>                   firstYear,
                              IAny<IReadOnlyList<double>> probabilities) {
        _firstYear     = firstYear;
        _probabilities = probabilities;
    }

    /// <summary>Pins <c>firstYear</c> to a fixed value.</summary>
    public AnyCalibratedDate WithFirstYear(int value) {
        return new AnyCalibratedDate(new FixedValue<int>(value), _probabilities);
    }

    /// <summary>Draws <c>firstYear</c> from <c>AnyValidFirstYear()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCalibratedDate WithFirstYear(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCalibratedDate(configure(AnyValidFirstYear()), _probabilities);
    }

    /// <summary>Pins <c>probabilities</c> to a fixed value.</summary>
    public AnyCalibratedDate WithProbabilities(IReadOnlyList<double> value) {
        return new AnyCalibratedDate(_firstYear, new FixedValue<IReadOnlyList<double>>(value));
    }

    /// <summary>Produces one arbitrary <see cref="CalibratedDate" />.</summary>
    public CalibratedDate Generate() {
        return new CalibratedDate(_firstYear.Generate(),
                                  _probabilities.Generate());
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
