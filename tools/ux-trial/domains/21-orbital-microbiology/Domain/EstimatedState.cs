namespace OrbitalMicrobiology.Domain;

/// <summary>
/// The culture's best belief about a species' concentration now: the model state, corrected by
/// the observations assimilated so far. Not a measurement; never in the future.
/// </summary>
public sealed class EstimatedState
{
    public EstimatedState(Species species, double elapsedHours, Concentration concentration, double relativeUncertainty, GrowthPhase phase, int observationCount)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(concentration);
        if (!double.IsFinite(elapsedHours) || elapsedHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedHours), elapsedHours, "An estimate is made at a non-negative elapsed time.");
        }

        if (!double.IsFinite(relativeUncertainty) || relativeUncertainty <= 0 || relativeUncertainty > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeUncertainty), relativeUncertainty, "A relative uncertainty lies in (0, 1].");
        }

        if (!Enum.IsDefined(phase))
        {
            throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown growth phase.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(observationCount);

        Species = species;
        ElapsedHours = elapsedHours;
        Concentration = concentration;
        RelativeUncertainty = relativeUncertainty;
        Phase = phase;
        ObservationCount = observationCount;
    }

    public Species Species { get; }

    public double ElapsedHours { get; }

    public Concentration Concentration { get; }

    public double RelativeUncertainty { get; }

    public GrowthPhase Phase { get; }

    /// <summary>How many observations of this species the estimate has absorbed; 0 means pure model.</summary>
    public int ObservationCount { get; }

    public bool IsModelOnly => ObservationCount == 0;
}
