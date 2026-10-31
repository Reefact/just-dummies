using System.Numerics;

namespace GridLoadFlow.Domain;

/// <summary>Power flowing into a branch at each of its ends, in MVA (P + jQ).</summary>
public sealed class BranchFlow
{
    public BranchFlow(string branchId, BusId fromBus, BusId toBus, Complex fromEndMva, Complex toEndMva, double ratingMva)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchId);
        ArgumentNullException.ThrowIfNull(fromBus);
        ArgumentNullException.ThrowIfNull(toBus);
        if (fromBus.Equals(toBus))
        {
            throw new ArgumentException("A branch flow joins two distinct buses.", nameof(toBus));
        }

        if (!double.IsFinite(fromEndMva.Real) || !double.IsFinite(fromEndMva.Imaginary) || !double.IsFinite(toEndMva.Real) || !double.IsFinite(toEndMva.Imaginary))
        {
            throw new ArgumentException("Branch flows must be finite.", nameof(fromEndMva));
        }

        if (!double.IsFinite(ratingMva) || ratingMva <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(ratingMva), ratingMva, "Rating must be positive.");
        }

        BranchId = branchId;
        FromBus = fromBus;
        ToBus = toBus;
        FromEndMva = fromEndMva;
        ToEndMva = toEndMva;
        RatingMva = ratingMva;
    }

    public string BranchId { get; }

    public BusId FromBus { get; }

    public BusId ToBus { get; }

    public Complex FromEndMva { get; }

    public Complex ToEndMva { get; }

    public double RatingMva { get; }

    /// <summary>Active losses: what enters at both ends and does not come out.</summary>
    public double LossesMw => FromEndMva.Real + ToEndMva.Real;

    public double LoadingPercent => Math.Max(FromEndMva.Magnitude, ToEndMva.Magnitude) / RatingMva * 100.0;

    public bool IsOverloaded => LoadingPercent > 100.0;
}
