// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VolumeChange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VolumeChange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVolumeChange : IAny<VolumeChange> {

    private IAny<MeasuredVolume> _before;
    private IAny<MeasuredVolume> _after;
    private IAny<int>            _intervalDays;

    private AnyMeasuredVolume _beforeRecipe;
    private AnyMeasuredVolume _afterRecipe;
    private AnyInt32          _intervalDaysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVolumeChange()
        : this(before:       AnyValidBefore(),
               after:        AnyValidAfter(),
               intervalDays: AnyValidIntervalDays()) { }

    private static AnyMeasuredVolume AnyValidBefore() {
        return new AnyMeasuredVolume();
    }

    private static AnyMeasuredVolume AnyValidAfter() {
        return new AnyMeasuredVolume();
    }

    private static AnyInt32 AnyValidIntervalDays() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVolumeChange(AnyMeasuredVolume before,
                            AnyMeasuredVolume after,
                            AnyInt32          intervalDays) {
        _before             = before;
        _beforeRecipe       = before;
        _after              = after;
        _afterRecipe        = after;
        _intervalDays       = intervalDays;
        _intervalDaysRecipe = intervalDays;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVolumeChange(AnyVolumeChange from) {
        _before             = from._before;
        _beforeRecipe       = from._beforeRecipe;
        _after              = from._after;
        _afterRecipe        = from._afterRecipe;
        _intervalDays       = from._intervalDays;
        _intervalDaysRecipe = from._intervalDaysRecipe;
    }

    /// <summary>Pins <c>before</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVolumeChange WithBefore(MeasuredVolume value) {
        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._before = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>before</c> is drawn from — <c>AnyValidBefore()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeasuredVolume, AnyMeasuredVolume&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVolumeChange WithBefore(Func<AnyMeasuredVolume, AnyMeasuredVolume> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._beforeRecipe = configure(_beforeRecipe);
        copy._before       = copy._beforeRecipe;

        return copy;
    }

    /// <summary>Pins <c>after</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVolumeChange WithAfter(MeasuredVolume value) {
        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._after = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>after</c> is drawn from — <c>AnyValidAfter()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMeasuredVolume, AnyMeasuredVolume&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVolumeChange WithAfter(Func<AnyMeasuredVolume, AnyMeasuredVolume> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._afterRecipe = configure(_afterRecipe);
        copy._after       = copy._afterRecipe;

        return copy;
    }

    /// <summary>Pins <c>intervalDays</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVolumeChange WithIntervalDays(int value) {
        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._intervalDays = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>intervalDays</c> is drawn from — <c>AnyValidIntervalDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVolumeChange WithIntervalDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVolumeChange copy = new AnyVolumeChange(this);

        copy._intervalDaysRecipe = configure(_intervalDaysRecipe);
        copy._intervalDays       = copy._intervalDaysRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VolumeChange" />.</summary>
    public VolumeChange Generate() {
        return new VolumeChange(_before.Generate(),
                                _after.Generate(),
                                _intervalDays.Generate());
    }

}
