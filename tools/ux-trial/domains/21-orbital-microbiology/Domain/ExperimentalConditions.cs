namespace OrbitalMicrobiology.Domain;

/// <summary>
/// The conditions a chamber holds a culture under: gravity level (in g; microgravity on the
/// station, or a centrifuge for the 1 g control), temperature, nutrient load and medium volume.
/// A centrifuge (0.5 g or more) only takes small chambers: at most <see cref="MaxCentrifugeVolumeMillilitres"/> mL.
/// </summary>
public sealed class ExperimentalConditions : IEquatable<ExperimentalConditions>
{
    public const double MaxGravityG = 2.0;
    public const double MicrogravityThresholdG = 0.01;
    public const double CentrifugeThresholdG = 0.5;
    public const double MinTemperatureCelsius = -20;
    public const double MaxTemperatureCelsius = 80;
    public const double MaxNutrientGramsPerLitre = 50;
    public const double MaxVolumeMillilitres = 50;
    public const double MaxCentrifugeVolumeMillilitres = 10;

    /// <summary>Monod half-saturation constant of the nutrient, g/L.</summary>
    public const double HalfSaturationGramsPerLitre = 0.5;

    public ExperimentalConditions(double gravityG, double temperatureCelsius, double nutrientGramsPerLitre, double volumeMillilitres)
    {
        if (!double.IsFinite(gravityG) || gravityG < 0 || gravityG > MaxGravityG)
        {
            throw new ArgumentOutOfRangeException(nameof(gravityG), gravityG, $"Gravity lies in [0, {MaxGravityG}] g.");
        }

        if (!double.IsFinite(temperatureCelsius) || temperatureCelsius < MinTemperatureCelsius || temperatureCelsius > MaxTemperatureCelsius)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temperatureCelsius),
                temperatureCelsius,
                $"A chamber holds [{MinTemperatureCelsius}, {MaxTemperatureCelsius}] °C.");
        }

        if (!double.IsFinite(nutrientGramsPerLitre) || nutrientGramsPerLitre <= 0 || nutrientGramsPerLitre > MaxNutrientGramsPerLitre)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nutrientGramsPerLitre),
                nutrientGramsPerLitre,
                $"The nutrient load lies in (0, {MaxNutrientGramsPerLitre}] g/L.");
        }

        if (!double.IsFinite(volumeMillilitres) || volumeMillilitres <= 0 || volumeMillilitres > MaxVolumeMillilitres)
        {
            throw new ArgumentOutOfRangeException(
                nameof(volumeMillilitres),
                volumeMillilitres,
                $"A chamber holds (0, {MaxVolumeMillilitres}] mL.");
        }

        if (gravityG >= CentrifugeThresholdG && volumeMillilitres > MaxCentrifugeVolumeMillilitres)
        {
            throw new ArgumentException(
                $"A centrifuged chamber ({gravityG} g) holds at most {MaxCentrifugeVolumeMillilitres} mL, got {volumeMillilitres} mL.",
                nameof(volumeMillilitres));
        }

        GravityG = gravityG;
        TemperatureCelsius = temperatureCelsius;
        NutrientGramsPerLitre = nutrientGramsPerLitre;
        VolumeMillilitres = volumeMillilitres;
    }

    public double GravityG { get; }

    public double TemperatureCelsius { get; }

    public double NutrientGramsPerLitre { get; }

    public double VolumeMillilitres { get; }

    public bool IsMicrogravity => GravityG < MicrogravityThresholdG;

    /// <summary>
    /// The specific growth rate the species reaches here: its maximum rate scaled by the
    /// temperature factor, the Monod nutrient term S / (Ks + S), and a microgravity boost of
    /// up to 20 % (reduced sedimentation keeps cells in the nutrient-rich medium).
    /// </summary>
    public GrowthRate EffectiveGrowthRate(Species species)
    {
        ArgumentNullException.ThrowIfNull(species);
        double temperature = species.TemperatureFactor(TemperatureCelsius);
        double nutrient = NutrientGramsPerLitre / (HalfSaturationGramsPerLitre + NutrientGramsPerLitre);
        double rate = species.MaxGrowthRate.PerHour * temperature * nutrient * GravityFactor(0.2);
        return new GrowthRate(Math.Min(rate, GrowthRate.MaxPerHour));
    }

    /// <summary>The maximum density the medium supports for the species: nutrient × yield, raised up to 30 % in microgravity.</summary>
    public Concentration CarryingCapacity(Species species)
    {
        ArgumentNullException.ThrowIfNull(species);
        double cells = NutrientGramsPerLitre / 1000 * species.YieldCellsPerGram * GravityFactor(0.3);
        return new Concentration(Math.Min(cells, Concentration.MaxCellsPerMillilitre));
    }

    /// <summary>
    /// How much faster the species grows here than under the reference conditions (a ratio of
    /// effective rates). Refused when the species does not grow at all under the reference.
    /// </summary>
    public double RelativeGrowthAgainst(ExperimentalConditions reference, Species species)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(species);
        double referenceRate = reference.EffectiveGrowthRate(species).PerHour;
        if (referenceRate == 0)
        {
            throw new InvalidOperationException($"{species} does not grow under the reference conditions; no ratio can be formed.");
        }

        return EffectiveGrowthRate(species).PerHour / referenceRate;
    }

    public bool Equals(ExperimentalConditions? other) =>
        other is not null
        && GravityG.Equals(other.GravityG)
        && TemperatureCelsius.Equals(other.TemperatureCelsius)
        && NutrientGramsPerLitre.Equals(other.NutrientGramsPerLitre)
        && VolumeMillilitres.Equals(other.VolumeMillilitres);

    public override bool Equals(object? obj) => Equals(obj as ExperimentalConditions);

    public override int GetHashCode() => HashCode.Combine(GravityG, TemperatureCelsius, NutrientGramsPerLitre, VolumeMillilitres);

    private double GravityFactor(double maxBoost) => 1 + (maxBoost * (1 - Math.Min(GravityG, 1)));
}
