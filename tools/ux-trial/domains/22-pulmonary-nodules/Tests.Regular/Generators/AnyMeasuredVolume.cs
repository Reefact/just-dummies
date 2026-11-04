// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeasuredVolume --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeasuredVolume" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasuredVolume : IAny<MeasuredVolume> {


    private AnyDouble _volumeMm3Recipe;
    private AnyDouble _uncertaintyMm3Recipe;

    private double? _volumeMm3Pin;
    private double? _uncertaintyMm3Pin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasuredVolume()
        : this(volumeMm3:      AnyValidVolumeMm3(),
               uncertaintyMm3: AnyValidUncertaintyMm3()) { }

    private static AnyDouble AnyValidVolumeMm3() {
        return Any.Double().Positive().LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3);
    }

    private static AnyDouble AnyValidUncertaintyMm3() {
        return Any.Double().GreaterThanOrEqualTo(0d);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeasuredVolume(AnyDouble volumeMm3,
                              AnyDouble uncertaintyMm3) {
        _volumeMm3Recipe            = volumeMm3;
        _uncertaintyMm3Recipe       = uncertaintyMm3;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeasuredVolume(AnyMeasuredVolume from) {
        _volumeMm3Recipe            = from._volumeMm3Recipe;
        _volumeMm3Pin               = from._volumeMm3Pin;
        _uncertaintyMm3Recipe       = from._uncertaintyMm3Recipe;
        _uncertaintyMm3Pin          = from._uncertaintyMm3Pin;
    }

    /// <summary>Draws <c>uncertaintyMm3</c> and <c>volumeMm3</c>, <c>uncertaintyMm3</c> always before <c>volumeMm3</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (double UncertaintyMm3, double VolumeMm3) UncertaintyMm3AndVolumeMm3() {
        if (_uncertaintyMm3Pin is { } uncertaintyMm3) { return (uncertaintyMm3, _volumeMm3Pin ?? _volumeMm3Recipe.GreaterThan(uncertaintyMm3).Generate()); }
        if (_volumeMm3Pin is { } volumeMm3) { return (_uncertaintyMm3Recipe.LessThan(volumeMm3).Generate(), volumeMm3); }

        return Any.Pair(_uncertaintyMm3Recipe, _volumeMm3Recipe).Having.Item1LessThanItem2().Generate();
    }

    /// <summary>Pins <c>volumeMm3</c> to a fixed value, which bounds <c>uncertaintyMm3</c>; its recipe is kept for a later refinement.</summary>
    public AnyMeasuredVolume WithVolumeMm3(double value) {
        AnyMeasuredVolume copy = new AnyMeasuredVolume(this);

        copy._volumeMm3Pin               = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>volumeMm3</c> is drawn from — <c>AnyValidVolumeMm3()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>uncertaintyMm3</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasuredVolume WithVolumeMm3(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasuredVolume copy = new AnyMeasuredVolume(this);

        copy._volumeMm3Recipe            = configure(_volumeMm3Recipe);
        copy._volumeMm3Pin               = null;

        return copy;
    }

    /// <summary>Pins <c>uncertaintyMm3</c> to a fixed value, which bounds <c>volumeMm3</c>; its recipe is kept for a later refinement.</summary>
    public AnyMeasuredVolume WithUncertaintyMm3(double value) {
        AnyMeasuredVolume copy = new AnyMeasuredVolume(this);

        copy._uncertaintyMm3Pin          = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>uncertaintyMm3</c> is drawn from — <c>AnyValidUncertaintyMm3()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>volumeMm3</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasuredVolume WithUncertaintyMm3(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasuredVolume copy = new AnyMeasuredVolume(this);

        copy._uncertaintyMm3Recipe       = configure(_uncertaintyMm3Recipe);
        copy._uncertaintyMm3Pin          = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MeasuredVolume" />.</summary>
    public MeasuredVolume Generate() {
        (double uncertaintyMm3, double volumeMm3) = UncertaintyMm3AndVolumeMm3();

        return new MeasuredVolume(volumeMm3,
                                  uncertaintyMm3);
    }

}
