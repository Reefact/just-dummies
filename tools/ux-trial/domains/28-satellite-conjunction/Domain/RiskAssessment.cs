namespace SatelliteConjunction.Domain;

/// <summary>The outcome of assessing a conjunction at its time of closest approach.</summary>
public sealed class RiskAssessment
{
    public RiskAssessment(Epoch timeOfClosestApproach, double missDistanceKm, double relativeSpeedKmPerSecond, double combinedSigmaKm, CollisionProbability probability)
    {
        ArgumentNullException.ThrowIfNull(timeOfClosestApproach);
        ArgumentNullException.ThrowIfNull(probability);
        if (!double.IsFinite(missDistanceKm) || missDistanceKm < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(missDistanceKm), missDistanceKm, "A miss distance is a finite, non-negative number of km.");
        }

        if (!double.IsFinite(relativeSpeedKmPerSecond) || relativeSpeedKmPerSecond <= 0 || relativeSpeedKmPerSecond > 2 * OrbitalState.MaximumSpeedKmPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(relativeSpeedKmPerSecond), relativeSpeedKmPerSecond, "A relative speed is positive and at most twice the maximum orbital speed.");
        }

        if (!double.IsFinite(combinedSigmaKm) || combinedSigmaKm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(combinedSigmaKm), combinedSigmaKm, "A combined dispersion is positive.");
        }

        TimeOfClosestApproach = timeOfClosestApproach;
        MissDistanceKm = missDistanceKm;
        RelativeSpeedKmPerSecond = relativeSpeedKmPerSecond;
        CombinedSigmaKm = combinedSigmaKm;
        Probability = probability;
    }

    public Epoch TimeOfClosestApproach { get; }

    public double MissDistanceKm { get; }

    public double RelativeSpeedKmPerSecond { get; }

    public double CombinedSigmaKm { get; }

    public CollisionProbability Probability { get; }

    public RiskLevel Level => Probability.Level;
}
