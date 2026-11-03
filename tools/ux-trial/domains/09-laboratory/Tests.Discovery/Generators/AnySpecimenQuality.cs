// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpecimenQuality --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpecimenQuality" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecimenQuality : IAny<SpecimenQuality> {

    private IAny<double>   _volumeMillilitres;
    private IAny<int>      _hemolysisIndex;
    private IAny<TimeSpan> _maxTransit;

    private AnyDouble   _volumeMillilitresRecipe;
    private AnyInt32    _hemolysisIndexRecipe;
    private AnyTimeSpan _maxTransitRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpecimenQuality()
        : this(volumeMillilitres: AnyValidVolumeMillilitres(),
               hemolysisIndex:    AnyValidHemolysisIndex(),
               maxTransit:        AnyValidMaxTransit()) { }

    private static AnyDouble AnyValidVolumeMillilitres() {
        return Any.Double().Positive().LessThanOrEqualTo(50d);
    }

    private static AnyInt32 AnyValidHemolysisIndex() {
        return Any.Int32().Between(0, 500);
    }

    private static AnyTimeSpan AnyValidMaxTransit() {
        return Any.TimeSpan().GreaterThan(TimeSpan.Zero);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySpecimenQuality(AnyDouble   volumeMillilitres,
                               AnyInt32    hemolysisIndex,
                               AnyTimeSpan maxTransit) {
        _volumeMillilitres       = volumeMillilitres;
        _volumeMillilitresRecipe = volumeMillilitres;
        _hemolysisIndex          = hemolysisIndex;
        _hemolysisIndexRecipe    = hemolysisIndex;
        _maxTransit              = maxTransit;
        _maxTransitRecipe        = maxTransit;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySpecimenQuality(AnySpecimenQuality from) {
        _volumeMillilitres       = from._volumeMillilitres;
        _volumeMillilitresRecipe = from._volumeMillilitresRecipe;
        _hemolysisIndex          = from._hemolysisIndex;
        _hemolysisIndexRecipe    = from._hemolysisIndexRecipe;
        _maxTransit              = from._maxTransit;
        _maxTransitRecipe        = from._maxTransitRecipe;
    }

    /// <summary>Pins <c>volumeMillilitres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecimenQuality WithVolumeMillilitres(double value) {
        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._volumeMillilitres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>volumeMillilitres</c> is drawn from — <c>AnyValidVolumeMillilitres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecimenQuality WithVolumeMillilitres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._volumeMillilitresRecipe = configure(_volumeMillilitresRecipe);
        copy._volumeMillilitres       = copy._volumeMillilitresRecipe;

        return copy;
    }

    /// <summary>Pins <c>hemolysisIndex</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecimenQuality WithHemolysisIndex(int value) {
        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._hemolysisIndex = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>hemolysisIndex</c> is drawn from — <c>AnyValidHemolysisIndex()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecimenQuality WithHemolysisIndex(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._hemolysisIndexRecipe = configure(_hemolysisIndexRecipe);
        copy._hemolysisIndex       = copy._hemolysisIndexRecipe;

        return copy;
    }

    /// <summary>Pins <c>maxTransit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySpecimenQuality WithMaxTransit(TimeSpan value) {
        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._maxTransit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>maxTransit</c> is drawn from — <c>AnyValidMaxTransit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTimeSpan, AnyTimeSpan&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySpecimenQuality WithMaxTransit(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySpecimenQuality copy = new AnySpecimenQuality(this);

        copy._maxTransitRecipe = configure(_maxTransitRecipe);
        copy._maxTransit       = copy._maxTransitRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SpecimenQuality" />.</summary>
    public SpecimenQuality Generate() {
        return new SpecimenQuality(_volumeMillilitres.Generate(),
                                   _hemolysisIndex.Generate(),
                                   _maxTransit.Generate());
    }

}
