// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Duration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Duration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDuration : IAny<Duration> {

    private readonly IAny<long> _numerator;
    private readonly IAny<long> _denominator;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDuration()
        : this(numerator:   AnyValidNumerator(),
               denominator: AnyValidDenominator()) { }

    private static AnyInt64 AnyValidNumerator() {
        // A duration is non-negative; it has no upper bound, but sixteen whole notes keeps sums of
        // dummies far from the checked overflow of the rational arithmetic.
        return Any.Int64().Between(0, 16 * Duration.MaxDenominator);
    }

    private static AnyInt64 AnyValidDenominator() {
        // The normalised denominator never exceeds the written one, so 1..1024 keeps it notatable.
        return Any.Int64().Between(1, Duration.MaxDenominator);
    }

    private AnyDuration(IAny<long> numerator,
                        IAny<long> denominator) {
        _numerator   = numerator;
        _denominator = denominator;
    }

    /// <summary>Pins <c>numerator</c> to a fixed value.</summary>
    public AnyDuration WithNumerator(long value) {
        return new AnyDuration(new FixedValue<long>(value), _denominator);
    }

    /// <summary>Draws <c>numerator</c> from <c>AnyValidNumerator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDuration WithNumerator(Func<AnyInt64, AnyInt64> configure) {
        return new AnyDuration(configure(AnyValidNumerator()), _denominator);
    }

    /// <summary>Pins <c>denominator</c> to a fixed value.</summary>
    public AnyDuration WithDenominator(long value) {
        return new AnyDuration(_numerator, new FixedValue<long>(value));
    }

    /// <summary>Draws <c>denominator</c> from <c>AnyValidDenominator()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDuration WithDenominator(Func<AnyInt64, AnyInt64> configure) {
        return new AnyDuration(_numerator, configure(AnyValidDenominator()));
    }

    /// <summary>Produces one arbitrary <see cref="Duration" />.</summary>
    public Duration Generate() {
        return new Duration(_numerator.Generate(),
                            _denominator.Generate());
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
