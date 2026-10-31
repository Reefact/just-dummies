// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ExperimentalConditions --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ExperimentalConditions" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyExperimentalConditions : IAny<ExperimentalConditions> {

    private readonly IAny<double> _gravityG;
    private readonly IAny<double> _temperatureCelsius;
    private readonly IAny<double> _nutrientGramsPerLitre;
    private readonly IAny<double> _volumeMillilitres;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyExperimentalConditions()
        : this(gravityG:              AnyValidGravityG(),
               temperatureCelsius:    AnyValidTemperatureCelsius(),
               nutrientGramsPerLitre: AnyValidNutrientGramsPerLitre(),
               volumeMillilitres:     AnyValidVolumeMillilitres()) { }

    private static AnyDouble AnyValidGravityG() {
        return Any.Double().Between(0, ExperimentalConditions.MaxGravityG);
    }

    private static AnyDouble AnyValidTemperatureCelsius() {
        return Any.Double().Between(ExperimentalConditions.MinTemperatureCelsius, ExperimentalConditions.MaxTemperatureCelsius);
    }

    private static AnyDouble AnyValidNutrientGramsPerLitre() {
        return Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxNutrientGramsPerLitre);
    }

    private static AnyDouble AnyValidVolumeMillilitres() {
        // A centrifuged chamber (>= 0.5 g) holds at most 10 mL; every chamber accepts that volume,
        // whatever gravity is drawn.
        return Any.Double().Positive().LessThanOrEqualTo(ExperimentalConditions.MaxCentrifugeVolumeMillilitres);
    }

    private AnyExperimentalConditions(IAny<double> gravityG,
                                      IAny<double> temperatureCelsius,
                                      IAny<double> nutrientGramsPerLitre,
                                      IAny<double> volumeMillilitres) {
        _gravityG              = gravityG;
        _temperatureCelsius    = temperatureCelsius;
        _nutrientGramsPerLitre = nutrientGramsPerLitre;
        _volumeMillilitres     = volumeMillilitres;
    }

    /// <summary>Pins <c>gravityG</c> to a fixed value.</summary>
    public AnyExperimentalConditions WithGravityG(double value) {
        return new AnyExperimentalConditions(new FixedValue<double>(value), _temperatureCelsius, _nutrientGramsPerLitre, _volumeMillilitres);
    }

    /// <summary>Draws <c>gravityG</c> from <c>AnyValidGravityG()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExperimentalConditions WithGravityG(Func<AnyDouble, AnyDouble> configure) {
        return new AnyExperimentalConditions(configure(AnyValidGravityG()), _temperatureCelsius, _nutrientGramsPerLitre, _volumeMillilitres);
    }

    /// <summary>Pins <c>temperatureCelsius</c> to a fixed value.</summary>
    public AnyExperimentalConditions WithTemperatureCelsius(double value) {
        return new AnyExperimentalConditions(_gravityG, new FixedValue<double>(value), _nutrientGramsPerLitre, _volumeMillilitres);
    }

    /// <summary>Draws <c>temperatureCelsius</c> from <c>AnyValidTemperatureCelsius()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExperimentalConditions WithTemperatureCelsius(Func<AnyDouble, AnyDouble> configure) {
        return new AnyExperimentalConditions(_gravityG, configure(AnyValidTemperatureCelsius()), _nutrientGramsPerLitre, _volumeMillilitres);
    }

    /// <summary>Pins <c>nutrientGramsPerLitre</c> to a fixed value.</summary>
    public AnyExperimentalConditions WithNutrientGramsPerLitre(double value) {
        return new AnyExperimentalConditions(_gravityG, _temperatureCelsius, new FixedValue<double>(value), _volumeMillilitres);
    }

    /// <summary>Draws <c>nutrientGramsPerLitre</c> from <c>AnyValidNutrientGramsPerLitre()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExperimentalConditions WithNutrientGramsPerLitre(Func<AnyDouble, AnyDouble> configure) {
        return new AnyExperimentalConditions(_gravityG, _temperatureCelsius, configure(AnyValidNutrientGramsPerLitre()), _volumeMillilitres);
    }

    /// <summary>Pins <c>volumeMillilitres</c> to a fixed value.</summary>
    public AnyExperimentalConditions WithVolumeMillilitres(double value) {
        return new AnyExperimentalConditions(_gravityG, _temperatureCelsius, _nutrientGramsPerLitre, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>volumeMillilitres</c> from <c>AnyValidVolumeMillilitres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyExperimentalConditions WithVolumeMillilitres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyExperimentalConditions(_gravityG, _temperatureCelsius, _nutrientGramsPerLitre, configure(AnyValidVolumeMillilitres()));
    }

    /// <summary>Produces one arbitrary <see cref="ExperimentalConditions" />.</summary>
    public ExperimentalConditions Generate() {
        return new ExperimentalConditions(_gravityG.Generate(),
                                          _temperatureCelsius.Generate(),
                                          _nutrientGramsPerLitre.Generate(),
                                          _volumeMillilitres.Generate());
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
