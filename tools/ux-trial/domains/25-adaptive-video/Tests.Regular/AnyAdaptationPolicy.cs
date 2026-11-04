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

    private IAny<double>           _safetyFactor;

    private AnyDouble _safetyFactorRecipe;
    private AnyDouble _panicBufferSecondsRecipe;
    private AnyDouble _comfortBufferSecondsRecipe;

    private double? _panicBufferSecondsPin;
    private double? _comfortBufferSecondsPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyAdaptationPolicy()
        : this(safetyFactor:         AnyValidSafetyFactor(),
               panicBufferSeconds:   AnyValidPanicBufferSeconds(),
               comfortBufferSeconds: AnyValidComfortBufferSeconds()) { }

    private static AnyDouble AnyValidSafetyFactor() {
        return Any.Double().Between(0.1d, 1d);
    }

    private static AnyDouble AnyValidPanicBufferSeconds() {
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    private static AnyDouble AnyValidComfortBufferSeconds() {
        return Any.Double();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyAdaptationPolicy(AnyDouble safetyFactor,
                                AnyDouble panicBufferSeconds,
                                AnyDouble comfortBufferSeconds) {
        _safetyFactor                              = safetyFactor;
        _safetyFactorRecipe                        = safetyFactor;
        _panicBufferSecondsRecipe                  = panicBufferSeconds;
        _comfortBufferSecondsRecipe                = comfortBufferSeconds;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyAdaptationPolicy(AnyAdaptationPolicy from) {
        _safetyFactor                              = from._safetyFactor;
        _safetyFactorRecipe                        = from._safetyFactorRecipe;
        _panicBufferSecondsRecipe                  = from._panicBufferSecondsRecipe;
        _panicBufferSecondsPin                     = from._panicBufferSecondsPin;
        _comfortBufferSecondsRecipe                = from._comfortBufferSecondsRecipe;
        _comfortBufferSecondsPin                   = from._comfortBufferSecondsPin;
    }

    /// <summary>Draws <c>panicBufferSeconds</c> and <c>comfortBufferSeconds</c>, <c>panicBufferSeconds</c> always before <c>comfortBufferSeconds</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (double PanicBufferSeconds, double ComfortBufferSeconds) PanicBufferSecondsAndComfortBufferSeconds() {
        if (_panicBufferSecondsPin is { } panicBufferSeconds) { return (panicBufferSeconds, _comfortBufferSecondsPin ?? _comfortBufferSecondsRecipe.GreaterThan(panicBufferSeconds).Generate()); }
        if (_comfortBufferSecondsPin is { } comfortBufferSeconds) { return (_panicBufferSecondsRecipe.LessThan(comfortBufferSeconds).Generate(), comfortBufferSeconds); }

        return Any.Pair(_panicBufferSecondsRecipe, _comfortBufferSecondsRecipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Pins <c>safetyFactor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationPolicy WithSafetyFactor(double value) {
        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._safetyFactor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>safetyFactor</c> is drawn from — <c>AnyValidSafetyFactor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationPolicy WithSafetyFactor(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._safetyFactorRecipe = configure(_safetyFactorRecipe);
        copy._safetyFactor       = copy._safetyFactorRecipe;

        return copy;
    }

    /// <summary>Pins <c>panicBufferSeconds</c> to a fixed value, which bounds <c>comfortBufferSeconds</c>; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationPolicy WithPanicBufferSeconds(double value) {
        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._panicBufferSecondsPin                     = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>panicBufferSeconds</c> is drawn from — <c>AnyValidPanicBufferSeconds()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>comfortBufferSeconds</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationPolicy WithPanicBufferSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._panicBufferSecondsRecipe                  = configure(_panicBufferSecondsRecipe);
        copy._panicBufferSecondsPin                     = null;

        return copy;
    }

    /// <summary>Pins <c>comfortBufferSeconds</c> to a fixed value, which bounds <c>panicBufferSeconds</c>; its recipe is kept for a later refinement.</summary>
    public AnyAdaptationPolicy WithComfortBufferSeconds(double value) {
        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._comfortBufferSecondsPin                   = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>comfortBufferSeconds</c> is drawn from — <c>AnyValidComfortBufferSeconds()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>panicBufferSeconds</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyAdaptationPolicy WithComfortBufferSeconds(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyAdaptationPolicy copy = new AnyAdaptationPolicy(this);

        copy._comfortBufferSecondsRecipe                = configure(_comfortBufferSecondsRecipe);
        copy._comfortBufferSecondsPin                   = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="AdaptationPolicy" />.</summary>
    public AdaptationPolicy Generate() {
        (double panicBufferSeconds, double comfortBufferSeconds) = PanicBufferSecondsAndComfortBufferSeconds();

        return new AdaptationPolicy(_safetyFactor.Generate(),
                                    panicBufferSeconds,
                                    comfortBufferSeconds);
    }

}
