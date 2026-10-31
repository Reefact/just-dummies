// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Duration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using MusicScore.Domain;

namespace MusicScore.Tests.Expert.Arbitrary;

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
        // Ordinary musical lengths: the domain's checked arithmetic is exact, not unbounded.
        return Any.Int64().Between(0, 64);
    }

    private static AnyInt64 AnyValidDenominator() {
        // Normalising only lowers the denominator, so any denominator up to the cap stays notatable.
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
