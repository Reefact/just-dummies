// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExperimentalConditions --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ExperimentalConditions" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExperimentalConditions : IAny<ExperimentalConditions> {

    private IAny<double> _gravityG;
    private IAny<double> _temperatureCelsius;
    private IAny<double> _nutrientGramsPerLitre;
    private IAny<double> _volumeMillilitres;

    private AnyDouble _gravityGRecipe;
    private AnyDouble _temperatureCelsiusRecipe;
    private AnyDouble _nutrientGramsPerLitreRecipe;
    private AnyDouble _volumeMillilitresRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExperimentalConditions()
        : this(gravityG:              AnyValidGravityG(),
               temperatureCelsius:    AnyValidTemperatureCelsius(),
               nutrientGramsPerLitre: AnyValidNutrientGramsPerLitre(),
               volumeMillilitres:     AnyValidVolumeMillilitres()) { }

    private static AnyDouble AnyValidGravityG() {
        return Any.Double().Between(0d, ExperimentalConditions.MaxGravityG);
    }

    private static AnyDouble AnyValidTemperatureCelsius() {
        return Any.Double().Between(ExperimentalConditions.MinTemperatureCelsius, ExperimentalConditions.MaxTemperatureCelsius);
    }

    private static AnyDouble AnyValidNutrientGramsPerLitre() {
        return Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxNutrientGramsPerLitre);
    }

    private static AnyDouble AnyValidVolumeMillilitres() {
        return Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxVolumeMillilitres);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyExperimentalConditions(AnyDouble gravityG,
                                      AnyDouble temperatureCelsius,
                                      AnyDouble nutrientGramsPerLitre,
                                      AnyDouble volumeMillilitres) {
        _gravityG                    = gravityG;
        _gravityGRecipe              = gravityG;
        _temperatureCelsius          = temperatureCelsius;
        _temperatureCelsiusRecipe    = temperatureCelsius;
        _nutrientGramsPerLitre       = nutrientGramsPerLitre;
        _nutrientGramsPerLitreRecipe = nutrientGramsPerLitre;
        _volumeMillilitres           = volumeMillilitres;
        _volumeMillilitresRecipe     = volumeMillilitres;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyExperimentalConditions(AnyExperimentalConditions from) {
        _gravityG                    = from._gravityG;
        _gravityGRecipe              = from._gravityGRecipe;
        _temperatureCelsius          = from._temperatureCelsius;
        _temperatureCelsiusRecipe    = from._temperatureCelsiusRecipe;
        _nutrientGramsPerLitre       = from._nutrientGramsPerLitre;
        _nutrientGramsPerLitreRecipe = from._nutrientGramsPerLitreRecipe;
        _volumeMillilitres           = from._volumeMillilitres;
        _volumeMillilitresRecipe     = from._volumeMillilitresRecipe;
    }

    /// <summary>Pins <c>gravityG</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExperimentalConditions WithGravityG(double value) {
        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._gravityG = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gravityG</c> is drawn from — <c>AnyValidGravityG()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExperimentalConditions WithGravityG(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._gravityGRecipe = configure(_gravityGRecipe);
        copy._gravityG       = copy._gravityGRecipe;

        return copy;
    }

    /// <summary>Pins <c>temperatureCelsius</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExperimentalConditions WithTemperatureCelsius(double value) {
        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._temperatureCelsius = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>temperatureCelsius</c> is drawn from — <c>AnyValidTemperatureCelsius()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExperimentalConditions WithTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._temperatureCelsiusRecipe = configure(_temperatureCelsiusRecipe);
        copy._temperatureCelsius       = copy._temperatureCelsiusRecipe;

        return copy;
    }

    /// <summary>Pins <c>nutrientGramsPerLitre</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExperimentalConditions WithNutrientGramsPerLitre(double value) {
        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._nutrientGramsPerLitre = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nutrientGramsPerLitre</c> is drawn from — <c>AnyValidNutrientGramsPerLitre()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExperimentalConditions WithNutrientGramsPerLitre(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._nutrientGramsPerLitreRecipe = configure(_nutrientGramsPerLitreRecipe);
        copy._nutrientGramsPerLitre       = copy._nutrientGramsPerLitreRecipe;

        return copy;
    }

    /// <summary>Pins <c>volumeMillilitres</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyExperimentalConditions WithVolumeMillilitres(double value) {
        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._volumeMillilitres = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>volumeMillilitres</c> is drawn from — <c>AnyValidVolumeMillilitres()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyExperimentalConditions WithVolumeMillilitres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyExperimentalConditions copy = new AnyExperimentalConditions(this);

        copy._volumeMillilitresRecipe = configure(_volumeMillilitresRecipe);
        copy._volumeMillilitres       = copy._volumeMillilitresRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="ExperimentalConditions" />.</summary>
    public ExperimentalConditions Generate() {
        double gravity = _gravityG.Generate();
        double volume  = _volumeMillilitres.Generate();

        // A centrifuged chamber (>= 0.5 g) only takes small volumes: tie the two here, keeping a valid pin.
        if (gravity >= ExperimentalConditions.CentrifugeThresholdG && volume > ExperimentalConditions.MaxCentrifugeVolumeMillilitres) {
            volume = _volumeMillilitresRecipe.LessThanOrEqualTo(ExperimentalConditions.MaxCentrifugeVolumeMillilitres).Generate();
        }

        return new ExperimentalConditions(gravity,
                                          _temperatureCelsius.Generate(),
                                          _nutrientGramsPerLitre.Generate(),
                                          volume);
    }

}
