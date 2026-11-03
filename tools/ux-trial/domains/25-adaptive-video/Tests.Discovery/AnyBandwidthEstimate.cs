// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate BandwidthEstimate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BandwidthEstimate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBandwidthEstimate : IAny<BandwidthEstimate> {

    private IAny<double> _throughputKbps;
    private IAny<int>    _sampleCount;
    private IAny<double> _smoothingFactor;

    private AnyDouble _throughputKbpsRecipe;
    private AnyInt32  _sampleCountRecipe;
    private AnyDouble _smoothingFactorRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBandwidthEstimate()
        : this(throughputKbps:  AnyValidThroughputKbps(),
               sampleCount:     AnyValidSampleCount(),
               smoothingFactor: AnyValidSmoothingFactor()) { }

    private static AnyDouble AnyValidThroughputKbps() {
        return Any.Double().Positive().LessThanOrEqualTo(1000000d);
    }

    private static AnyInt32 AnyValidSampleCount() {
        return Any.Int32().Positive();
    }

    private static AnyDouble AnyValidSmoothingFactor() {
        return Any.Double().Positive().LessThanOrEqualTo(1d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBandwidthEstimate(AnyDouble throughputKbps,
                                 AnyInt32  sampleCount,
                                 AnyDouble smoothingFactor) {
        _throughputKbps        = throughputKbps;
        _throughputKbpsRecipe  = throughputKbps;
        _sampleCount           = sampleCount;
        _sampleCountRecipe     = sampleCount;
        _smoothingFactor       = smoothingFactor;
        _smoothingFactorRecipe = smoothingFactor;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBandwidthEstimate(AnyBandwidthEstimate from) {
        _throughputKbps        = from._throughputKbps;
        _throughputKbpsRecipe  = from._throughputKbpsRecipe;
        _sampleCount           = from._sampleCount;
        _sampleCountRecipe     = from._sampleCountRecipe;
        _smoothingFactor       = from._smoothingFactor;
        _smoothingFactorRecipe = from._smoothingFactorRecipe;
    }

    /// <summary>Pins <c>throughputKbps</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBandwidthEstimate WithThroughputKbps(double value) {
        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._throughputKbps = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>throughputKbps</c> is drawn from — <c>AnyValidThroughputKbps()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBandwidthEstimate WithThroughputKbps(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._throughputKbpsRecipe = configure(_throughputKbpsRecipe);
        copy._throughputKbps       = copy._throughputKbpsRecipe;

        return copy;
    }

    /// <summary>Pins <c>sampleCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBandwidthEstimate WithSampleCount(int value) {
        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._sampleCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sampleCount</c> is drawn from — <c>AnyValidSampleCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBandwidthEstimate WithSampleCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._sampleCountRecipe = configure(_sampleCountRecipe);
        copy._sampleCount       = copy._sampleCountRecipe;

        return copy;
    }

    /// <summary>Pins <c>smoothingFactor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBandwidthEstimate WithSmoothingFactor(double value) {
        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._smoothingFactor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>smoothingFactor</c> is drawn from — <c>AnyValidSmoothingFactor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBandwidthEstimate WithSmoothingFactor(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBandwidthEstimate copy = new AnyBandwidthEstimate(this);

        copy._smoothingFactorRecipe = configure(_smoothingFactorRecipe);
        copy._smoothingFactor       = copy._smoothingFactorRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="BandwidthEstimate" />.</summary>
    public BandwidthEstimate Generate() {
        return new BandwidthEstimate(_throughputKbps.Generate(),
                                     _sampleCount.Generate(),
                                     _smoothingFactor.Generate());
    }

}
