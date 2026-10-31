namespace OrbitalMicrobiology.Domain;

/// <summary>
/// One species living in a culture: its current modelled concentration, how uncertain that is,
/// how long it has been in the chamber, and the growth phase it is in. A population is changed
/// only by the culture that holds it.
/// </summary>
public sealed class MicrobialPopulation
{
    public MicrobialPopulation(Species species, Concentration concentration, double relativeUncertainty, double ageHours)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(concentration);
        if (!double.IsFinite(relativeUncertainty) || relativeUncertainty <= 0 || relativeUncertainty > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeUncertainty), relativeUncertainty, "A relative uncertainty lies in (0, 1].");
        }

        if (!double.IsFinite(ageHours) || ageHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ageHours), ageHours, "A population's age is finite and non-negative.");
        }

        Species = species;
        Concentration = concentration;
        RelativeUncertainty = relativeUncertainty;
        AgeHours = ageHours;
        Phase = ageHours < species.LagHours ? GrowthPhase.Lag : GrowthPhase.Exponential;
    }

    /// <summary>Uncertainty of a model state grows by this much per hour without an observation.</summary>
    public const double UncertaintyDriftPerHour = 0.02;

    public Species Species { get; }

    public Concentration Concentration { get; private set; }

    public double RelativeUncertainty { get; private set; }

    public double AgeHours { get; private set; }

    public GrowthPhase Phase { get; private set; }

    public bool IsExtinct => Concentration.CellsPerMillilitre == 0;

    /// <summary>Generations elapsed since the given starting concentration (log2 fold change).</summary>
    public double GenerationsSince(Concentration inoculum)
    {
        ArgumentNullException.ThrowIfNull(inoculum);
        return inoculum.GenerationsTo(Concentration);
    }

    internal MicrobialPopulation Copy()
    {
        MicrobialPopulation copy = new(Species, Concentration, RelativeUncertainty, AgeHours);
        copy.Phase = Phase;
        return copy;
    }

    internal void Step(double cellsPerMillilitre, GrowthPhase phase, double hours)
    {
        double bounded = Math.Clamp(cellsPerMillilitre, 0, Concentration.MaxCellsPerMillilitre);
        Concentration = new Concentration(bounded < 1 ? 0 : bounded);
        Phase = Concentration.CellsPerMillilitre == 0 ? GrowthPhase.Death : phase;
        AgeHours += hours;
        RelativeUncertainty = Math.Min(1, RelativeUncertainty + (UncertaintyDriftPerHour * hours));
    }

    /// <summary>Inverse-variance blend of the model state with a measurement.</summary>
    internal void Assimilate(Observation observation)
    {
        double modelWeight = 1 / Math.Pow(RelativeUncertainty, 2);
        double observationWeight = 1 / Math.Pow(observation.RelativeUncertainty, 2);
        double blended = ((modelWeight * Concentration.CellsPerMillilitre) + (observationWeight * observation.Measured.CellsPerMillilitre))
                         / (modelWeight + observationWeight);
        Concentration = new Concentration(blended);
        RelativeUncertainty = 1 / Math.Sqrt(modelWeight + observationWeight);
    }
}
