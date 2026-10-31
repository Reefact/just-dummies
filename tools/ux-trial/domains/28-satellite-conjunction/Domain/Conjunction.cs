namespace SatelliteConjunction.Domain;

public enum ConjunctionStatus
{
    Screened,
    Assessed,
    ManeuverPlanned,
    Closed,
}

/// <summary>
/// A predicted close approach between an active satellite (the primary) and another catalogued object (the secondary).
/// Lifecycle: Screened → Assessed → (ManeuverPlanned) → Closed.
/// </summary>
public sealed class Conjunction
{
    private readonly List<RiskAssessment> history = [];

    public Conjunction(Guid id, Trajectory primary, Trajectory secondary, Epoch timeOfClosestApproach)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A conjunction needs a non-empty identifier.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        ArgumentNullException.ThrowIfNull(timeOfClosestApproach);

        if (!primary.SpaceObject.IsActive)
        {
            throw new ArgumentException("The primary of a conjunction must be an active satellite.", nameof(primary));
        }

        if (primary.SpaceObject.CatalogNumber == secondary.SpaceObject.CatalogNumber)
        {
            throw new ArgumentException("An object cannot be in conjunction with itself.", nameof(secondary));
        }

        if (!primary.Covers(timeOfClosestApproach) || !secondary.Covers(timeOfClosestApproach))
        {
            throw new ArgumentOutOfRangeException(nameof(timeOfClosestApproach), timeOfClosestApproach, "Both trajectories must cover the time of closest approach.");
        }

        Id = id;
        Primary = primary;
        Secondary = secondary;
        TimeOfClosestApproach = timeOfClosestApproach;
        Status = ConjunctionStatus.Screened;
    }

    public Guid Id { get; }

    public Trajectory Primary { get; }

    public Trajectory Secondary { get; }

    public Epoch TimeOfClosestApproach { get; }

    public ConjunctionStatus Status { get; private set; }

    public RiskAssessment? LatestAssessment => history.Count == 0 ? null : history[^1];

    public IReadOnlyList<RiskAssessment> History => history;

    public string? ClosureReason { get; private set; }

    /// <summary>
    /// Computes the miss distance and a 2D Gaussian collision probability in the encounter plane
    /// (the plane normal to the relative velocity), using the combined position covariance.
    /// May be repeated while the conjunction is open, as fresher trajectories arrive.
    /// </summary>
    public RiskAssessment Assess()
    {
        if (Status == ConjunctionStatus.Closed)
        {
            throw new InvalidOperationException("A closed conjunction cannot be assessed again.");
        }

        OrbitalState primaryState = Primary.StateAt(TimeOfClosestApproach);
        OrbitalState secondaryState = Secondary.StateAt(TimeOfClosestApproach);

        Vector3 relativePosition = secondaryState.PositionKm.Minus(primaryState.PositionKm);
        Vector3 relativeVelocity = secondaryState.VelocityKmPerSecond.Minus(primaryState.VelocityKmPerSecond);
        if (relativeVelocity.Magnitude < 1e-6)
        {
            throw new InvalidOperationException("The objects are co-moving: an encounter-plane assessment does not apply.");
        }

        PositionCovariance combined = primaryState.Covariance.Combine(secondaryState.Covariance);
        double hardBodyRadiusKm = (Primary.SpaceObject.HardBodyRadiusMeters + Secondary.SpaceObject.HardBodyRadiusMeters) / 1000;

        Vector3 along = relativeVelocity.Normalized();
        Vector3 helper = Math.Abs(along.Z) < 0.9 ? new Vector3(0, 0, 1) : new Vector3(1, 0, 0);
        Vector3 axisU = along.Cross(helper).Normalized();
        Vector3 axisV = along.Cross(axisU);

        double missU = relativePosition.Dot(axisU);
        double missV = relativePosition.Dot(axisV);
        double varianceU = combined.Project(axisU, axisU);
        double varianceV = combined.Project(axisV, axisV);
        double covarianceUv = combined.Project(axisU, axisV);
        double determinant = varianceU * varianceV - covarianceUv * covarianceUv;

        double mahalanobisSquared = (varianceV * missU * missU - 2 * covarianceUv * missU * missV + varianceU * missV * missV) / determinant;
        double probability = hardBodyRadiusKm * hardBodyRadiusKm / (2 * Math.Sqrt(determinant)) * Math.Exp(-mahalanobisSquared / 2);

        RiskAssessment assessment = new(
            TimeOfClosestApproach,
            relativePosition.Magnitude,
            relativeVelocity.Magnitude,
            combined.MeanSigmaKm,
            new CollisionProbability(Math.Min(probability, 1)));

        history.Add(assessment);
        if (Status == ConjunctionStatus.Screened)
        {
            Status = ConjunctionStatus.Assessed;
        }

        return assessment;
    }

    /// <summary>Plans an avoidance manoeuvre; refused unless the latest assessment is High risk.</summary>
    public void PlanManeuver()
    {
        if (Status != ConjunctionStatus.Assessed)
        {
            throw new InvalidOperationException($"A manoeuvre can only be planned for an assessed conjunction (status is {Status}).");
        }

        if (LatestAssessment!.Level != RiskLevel.High)
        {
            throw new InvalidOperationException($"A manoeuvre is only warranted for a High risk (latest is {LatestAssessment.Level}, Pc {LatestAssessment.Probability}).");
        }

        Status = ConjunctionStatus.ManeuverPlanned;
    }

    /// <summary>Closes the conjunction; a High risk can only be closed once a manoeuvre is planned.</summary>
    public void Close(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == ConjunctionStatus.Closed)
        {
            throw new InvalidOperationException("The conjunction is already closed.");
        }

        if (Status == ConjunctionStatus.Screened)
        {
            throw new InvalidOperationException("A conjunction must be assessed before it is closed.");
        }

        if (Status == ConjunctionStatus.Assessed && LatestAssessment!.Level == RiskLevel.High)
        {
            throw new InvalidOperationException("A High-risk conjunction cannot be closed without a planned manoeuvre.");
        }

        ClosureReason = reason;
        Status = ConjunctionStatus.Closed;
    }
}
