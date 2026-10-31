// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate AdaptationPolicy --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="AdaptationPolicy" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyAdaptationPolicy : IAny<AdaptationPolicy> {

    private readonly IAny<double> _safetyFactor;
    private readonly IAny<double> _panicBufferSeconds;
    private readonly IAny<double> _comfortBufferSeconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdaptationPolicy()
        : this(safetyFactor:         AnyValidSafetyFactor(),
               panicBufferSeconds:   AnyValidPanicBufferSeconds(),
               comfortBufferSeconds: AnyValidComfortBufferSeconds()) { }

    private static AnyDouble AnyValidSafetyFactor() {
        // Fraction of the throughput the policy spends: 0.1..1.
        return Any.Double().Between(0.1, 1);
    }

    private static AnyDouble AnyValidPanicBufferSeconds() {
        // comfort > panic ties the two thresholds. They are drawn from disjoint bands (panic 0..8 s,
        // comfort 10..30 s) so the rule holds by construction; a test pinning one keeps it inside its band.
        return Any.Double().Between(0, 8);
    }

    private static AnyDouble AnyValidComfortBufferSeconds() {
        // See AnyValidPanicBufferSeconds(): comfort stays at or below 30 s, the floor of a session buffer's room.
        return Any.Double().Between(10, 30);
    }

    private AnyAdaptationPolicy(IAny<double> safetyFactor,
                                IAny<double> panicBufferSeconds,
                                IAny<double> comfortBufferSeconds) {
        _safetyFactor         = safetyFactor;
        _panicBufferSeconds   = panicBufferSeconds;
        _comfortBufferSeconds = comfortBufferSeconds;
    }

    /// <summary>Pins <c>safetyFactor</c> to a fixed value.</summary>
    public AnyAdaptationPolicy WithSafetyFactor(double value) {
        return new AnyAdaptationPolicy(new FixedValue<double>(value), _panicBufferSeconds, _comfortBufferSeconds);
    }

    /// <summary>Draws <c>safetyFactor</c> from <c>AnyValidSafetyFactor()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAdaptationPolicy WithSafetyFactor(Func<AnyDouble, AnyDouble> configure) {
        return new AnyAdaptationPolicy(configure(AnyValidSafetyFactor()), _panicBufferSeconds, _comfortBufferSeconds);
    }

    /// <summary>Pins <c>panicBufferSeconds</c> to a fixed value.</summary>
    public AnyAdaptationPolicy WithPanicBufferSeconds(double value) {
        return new AnyAdaptationPolicy(_safetyFactor, new FixedValue<double>(value), _comfortBufferSeconds);
    }

    /// <summary>Draws <c>panicBufferSeconds</c> from <c>AnyValidPanicBufferSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAdaptationPolicy WithPanicBufferSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyAdaptationPolicy(_safetyFactor, configure(AnyValidPanicBufferSeconds()), _comfortBufferSeconds);
    }

    /// <summary>Pins <c>comfortBufferSeconds</c> to a fixed value.</summary>
    public AnyAdaptationPolicy WithComfortBufferSeconds(double value) {
        return new AnyAdaptationPolicy(_safetyFactor, _panicBufferSeconds, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>comfortBufferSeconds</c> from <c>AnyValidComfortBufferSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyAdaptationPolicy WithComfortBufferSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyAdaptationPolicy(_safetyFactor, _panicBufferSeconds, configure(AnyValidComfortBufferSeconds()));
    }

    /// <summary>Produces one arbitrary <see cref="AdaptationPolicy" />.</summary>
    public AdaptationPolicy Generate() {
        return new AdaptationPolicy(_safetyFactor.Generate(),
                                    _panicBufferSeconds.Generate(),
                                    _comfortBufferSeconds.Generate());
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
