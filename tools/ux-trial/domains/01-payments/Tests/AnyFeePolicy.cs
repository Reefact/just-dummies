// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate FeePolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FeePolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFeePolicy : IAny<FeePolicy> {

    private readonly IAny<decimal> _instantFlatFee;
    private readonly IAny<decimal> _instantRate;
    private readonly IAny<decimal> _freeStandardThreshold;
    private readonly IAny<decimal> _standardRate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFeePolicy()
        : this(instantFlatFee:        AnyValidInstantFlatFee(),
               instantRate:           AnyValidInstantRate(),
               freeStandardThreshold: AnyValidFreeStandardThreshold(),
               standardRate:          AnyValidStandardRate()) { }

    private static AnyDecimal AnyValidInstantFlatFee() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidInstantRate() {
        return Any.Decimal().Between(0m, 0.05m);
    }

    private static AnyDecimal AnyValidFreeStandardThreshold() {
        return Any.Decimal().GreaterThanOrEqualTo(0m);
    }

    private static AnyDecimal AnyValidStandardRate() {
        return Any.Decimal().Between(0m, 0.05m);
    }

    private AnyFeePolicy(IAny<decimal> instantFlatFee,
                         IAny<decimal> instantRate,
                         IAny<decimal> freeStandardThreshold,
                         IAny<decimal> standardRate) {
        _instantFlatFee        = instantFlatFee;
        _instantRate           = instantRate;
        _freeStandardThreshold = freeStandardThreshold;
        _standardRate          = standardRate;
    }

    /// <summary>Pins <c>instantFlatFee</c> to a fixed value.</summary>
    public AnyFeePolicy WithInstantFlatFee(decimal value) {
        return new AnyFeePolicy(new FixedValue<decimal>(value), _instantRate, _freeStandardThreshold, _standardRate);
    }

    /// <summary>Draws <c>instantFlatFee</c> from <c>AnyValidInstantFlatFee()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFeePolicy WithInstantFlatFee(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyFeePolicy(configure(AnyValidInstantFlatFee()), _instantRate, _freeStandardThreshold, _standardRate);
    }

    /// <summary>Pins <c>instantRate</c> to a fixed value.</summary>
    public AnyFeePolicy WithInstantRate(decimal value) {
        return new AnyFeePolicy(_instantFlatFee, new FixedValue<decimal>(value), _freeStandardThreshold, _standardRate);
    }

    /// <summary>Draws <c>instantRate</c> from <c>AnyValidInstantRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFeePolicy WithInstantRate(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyFeePolicy(_instantFlatFee, configure(AnyValidInstantRate()), _freeStandardThreshold, _standardRate);
    }

    /// <summary>Pins <c>freeStandardThreshold</c> to a fixed value.</summary>
    public AnyFeePolicy WithFreeStandardThreshold(decimal value) {
        return new AnyFeePolicy(_instantFlatFee, _instantRate, new FixedValue<decimal>(value), _standardRate);
    }

    /// <summary>Draws <c>freeStandardThreshold</c> from <c>AnyValidFreeStandardThreshold()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFeePolicy WithFreeStandardThreshold(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyFeePolicy(_instantFlatFee, _instantRate, configure(AnyValidFreeStandardThreshold()), _standardRate);
    }

    /// <summary>Pins <c>standardRate</c> to a fixed value.</summary>
    public AnyFeePolicy WithStandardRate(decimal value) {
        return new AnyFeePolicy(_instantFlatFee, _instantRate, _freeStandardThreshold, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>standardRate</c> from <c>AnyValidStandardRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFeePolicy WithStandardRate(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyFeePolicy(_instantFlatFee, _instantRate, _freeStandardThreshold, configure(AnyValidStandardRate()));
    }

    /// <summary>Produces one arbitrary <see cref="FeePolicy" />.</summary>
    public FeePolicy Generate() {
        return new FeePolicy(_instantFlatFee.Generate(),
                             _instantRate.Generate(),
                             _freeStandardThreshold.Generate(),
                             _standardRate.Generate());
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
