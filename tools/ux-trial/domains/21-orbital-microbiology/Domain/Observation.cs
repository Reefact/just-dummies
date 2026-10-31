namespace OrbitalMicrobiology.Domain;

/// <summary>
/// A measurement of one species' concentration in a culture, taken at an elapsed time.
/// It carries its own relative uncertainty (one standard deviation, as a fraction of the value).
/// Optical density is indirect (converted from turbidity), so it cannot claim better than
/// <see cref="MinIndirectRelativeUncertainty"/>.
/// </summary>
public sealed class Observation
{
    public const double MinIndirectRelativeUncertainty = 0.10;

    public Observation(
        CultureId cultureId,
        Species species,
        double elapsedHours,
        Concentration measured,
        double relativeUncertainty,
        ObservationMethod method)
    {
        ArgumentNullException.ThrowIfNull(cultureId);
        ArgumentNullException.ThrowIfNull(species);
        ArgumentNullException.ThrowIfNull(measured);
        if (!double.IsFinite(elapsedHours) || elapsedHours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedHours), elapsedHours, "An observation is taken at a non-negative elapsed time.");
        }

        if (!double.IsFinite(relativeUncertainty) || relativeUncertainty <= 0 || relativeUncertainty > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeUncertainty), relativeUncertainty, "A relative uncertainty lies in (0, 1].");
        }

        if (!Enum.IsDefined(method))
        {
            throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown observation method.");
        }

        if (method == ObservationMethod.OpticalDensity && relativeUncertainty < MinIndirectRelativeUncertainty)
        {
            throw new ArgumentException(
                $"An optical-density reading is indirect and cannot claim an uncertainty below {MinIndirectRelativeUncertainty:P0}, got {relativeUncertainty:P1}.",
                nameof(relativeUncertainty));
        }

        CultureId = cultureId;
        Species = species;
        ElapsedHours = elapsedHours;
        Measured = measured;
        RelativeUncertainty = relativeUncertainty;
        Method = method;
    }

    public CultureId CultureId { get; }

    public Species Species { get; }

    public double ElapsedHours { get; }

    public Concentration Measured { get; }

    public double RelativeUncertainty { get; }

    public ObservationMethod Method { get; }

    public bool IsIndirect => Method == ObservationMethod.OpticalDensity;

    public double LowerBoundCellsPerMillilitre => Measured.CellsPerMillilitre * (1 - RelativeUncertainty);

    public double UpperBoundCellsPerMillilitre => Measured.CellsPerMillilitre * (1 + RelativeUncertainty);
}
