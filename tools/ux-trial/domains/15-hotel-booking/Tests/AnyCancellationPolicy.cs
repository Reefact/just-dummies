// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CancellationPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CancellationPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCancellationPolicy : IAny<CancellationPolicy> {

    private readonly IAny<int>     _freeCancellationDays;
    private readonly IAny<decimal> _penaltyRate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCancellationPolicy()
        : this(freeCancellationDays: AnyValidFreeCancellationDays(),
               penaltyRate:          AnyValidPenaltyRate()) { }

    private static AnyInt32 AnyValidFreeCancellationDays() {
        return Any.Int32().Between(0, 365);
    }

    private static AnyDecimal AnyValidPenaltyRate() {
        return Any.Decimal().Between(0m, 1m);
    }

    private AnyCancellationPolicy(IAny<int>     freeCancellationDays,
                                  IAny<decimal> penaltyRate) {
        _freeCancellationDays = freeCancellationDays;
        _penaltyRate          = penaltyRate;
    }

    /// <summary>Pins <c>freeCancellationDays</c> to a fixed value.</summary>
    public AnyCancellationPolicy WithFreeCancellationDays(int value) {
        return new AnyCancellationPolicy(new FixedValue<int>(value), _penaltyRate);
    }

    /// <summary>Draws <c>freeCancellationDays</c> from <c>AnyValidFreeCancellationDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCancellationPolicy WithFreeCancellationDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCancellationPolicy(configure(AnyValidFreeCancellationDays()), _penaltyRate);
    }

    /// <summary>Pins <c>penaltyRate</c> to a fixed value.</summary>
    public AnyCancellationPolicy WithPenaltyRate(decimal value) {
        return new AnyCancellationPolicy(_freeCancellationDays, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>penaltyRate</c> from <c>AnyValidPenaltyRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCancellationPolicy WithPenaltyRate(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyCancellationPolicy(_freeCancellationDays, configure(AnyValidPenaltyRate()));
    }

    /// <summary>Produces one arbitrary <see cref="CancellationPolicy" />.</summary>
    public CancellationPolicy Generate() {
        return new CancellationPolicy(_freeCancellationDays.Generate(),
                                      _penaltyRate.Generate());
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
