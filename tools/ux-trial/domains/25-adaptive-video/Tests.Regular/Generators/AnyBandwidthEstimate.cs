// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BandwidthEstimate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using AdaptiveVideo.Domain;
using JustDummies;

namespace AdaptiveVideo.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="BandwidthEstimate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBandwidthEstimate : IAny<BandwidthEstimate> {

    private readonly IAny<double> _throughputKbps;
    private readonly IAny<int>    _sampleCount;
    private readonly IAny<double> _smoothingFactor;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBandwidthEstimate()
        : this(throughputKbps:  AnyValidThroughputKbps(),
               sampleCount:     AnyValidSampleCount(),
               smoothingFactor: AnyValidSmoothingFactor()) { }

    private static AnyDouble AnyValidThroughputKbps() {
        return Any.Double().GreaterThan(0).LessThanOrEqualTo(1_000_000);
    }

    private static AnyInt32 AnyValidSampleCount() {
        return Any.Int32().Positive();
    }

    private static AnyDouble AnyValidSmoothingFactor() {
        return Any.Double().GreaterThan(0).LessThanOrEqualTo(1);
    }

    private AnyBandwidthEstimate(IAny<double> throughputKbps,
                                 IAny<int>    sampleCount,
                                 IAny<double> smoothingFactor) {
        _throughputKbps  = throughputKbps;
        _sampleCount     = sampleCount;
        _smoothingFactor = smoothingFactor;
    }

    /// <summary>Pins <c>throughputKbps</c> to a fixed value.</summary>
    public AnyBandwidthEstimate WithThroughputKbps(double value) {
        return new AnyBandwidthEstimate(new FixedValue<double>(value), _sampleCount, _smoothingFactor);
    }

    /// <summary>Draws <c>throughputKbps</c> from <c>AnyValidThroughputKbps()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBandwidthEstimate WithThroughputKbps(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBandwidthEstimate(configure(AnyValidThroughputKbps()), _sampleCount, _smoothingFactor);
    }

    /// <summary>Pins <c>sampleCount</c> to a fixed value.</summary>
    public AnyBandwidthEstimate WithSampleCount(int value) {
        return new AnyBandwidthEstimate(_throughputKbps, new FixedValue<int>(value), _smoothingFactor);
    }

    /// <summary>Draws <c>sampleCount</c> from <c>AnyValidSampleCount()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBandwidthEstimate WithSampleCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnyBandwidthEstimate(_throughputKbps, configure(AnyValidSampleCount()), _smoothingFactor);
    }

    /// <summary>Pins <c>smoothingFactor</c> to a fixed value.</summary>
    public AnyBandwidthEstimate WithSmoothingFactor(double value) {
        return new AnyBandwidthEstimate(_throughputKbps, _sampleCount, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>smoothingFactor</c> from <c>AnyValidSmoothingFactor()</c>, refined by <paramref name="configure" />.</summary>
    public AnyBandwidthEstimate WithSmoothingFactor(Func<AnyDouble, AnyDouble> configure) {
        return new AnyBandwidthEstimate(_throughputKbps, _sampleCount, configure(AnyValidSmoothingFactor()));
    }

    /// <summary>Produces one arbitrary <see cref="BandwidthEstimate" />.</summary>
    public BandwidthEstimate Generate() {
        return new BandwidthEstimate(_throughputKbps.Generate(),
                                     _sampleCount.Generate(),
                                     _smoothingFactor.Generate());
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
