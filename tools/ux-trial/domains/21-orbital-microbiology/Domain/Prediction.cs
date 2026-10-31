namespace OrbitalMicrobiology.Domain;

/// <summary>
/// A forecast of a species' concentration at a horizon after the moment it was made. Its
/// uncertainty is never below that of the estimate it started from.
/// </summary>
public sealed class Prediction
{
    public const double MaxHorizonHours = 720;

    public Prediction(Species species, double madeAtHours, double horizonHours, Concentration expected, double relativeUncertainty)
    {
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(expected);
        if (!double.IsFinite(madeAtHours) || madeAtHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(madeAtHours), madeAtHours, "A prediction is made at a non-negative elapsed time.");
        }

        if (!double.IsFinite(horizonHours) || horizonHours <= 0 || horizonHours > MaxHorizonHours)
        {
            throw new ArgumentOutOfRangeException(nameof(horizonHours), horizonHours, $"A prediction looks (0, {MaxHorizonHours}] hours ahead.");
        }

        if (!double.IsFinite(relativeUncertainty) || relativeUncertainty <= 0 || relativeUncertainty > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeUncertainty), relativeUncertainty, "A relative uncertainty lies in (0, 1].");
        }

        Species = species;
        MadeAtHours = madeAtHours;
        HorizonHours = horizonHours;
        Expected = expected;
        RelativeUncertainty = relativeUncertainty;
    }

    public Species Species { get; }

    public double MadeAtHours { get; }

    public double HorizonHours { get; }

    public double TargetHours => MadeAtHours + HorizonHours;

    public Concentration Expected { get; }

    public double RelativeUncertainty { get; }

    /// <summary>Whether an observation at the target time falls within one standard deviation of the forecast.</summary>
    public bool IsConfirmedBy(Observation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (!observation.Species.Equals(Species))
        {
            throw new ArgumentException($"The observation is of {observation.Species}, the prediction of {Species}.", nameof(observation));
        }

        if (Math.Abs(observation.ElapsedHours - TargetHours) > 0.5)
        {
            throw new ArgumentException(
                $"The observation was taken at {observation.ElapsedHours} h, the prediction targets {TargetHours} h.",
                nameof(observation));
        }

        double expected = Expected.CellsPerMillilitre;
        double tolerance = Math.Sqrt(Math.Pow(expected * RelativeUncertainty, 2) + Math.Pow(observation.Measured.CellsPerMillilitre * observation.RelativeUncertainty, 2));
        return Math.Abs(observation.Measured.CellsPerMillilitre - expected) <= tolerance;
    }
}
