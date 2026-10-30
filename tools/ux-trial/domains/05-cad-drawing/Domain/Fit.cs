namespace Drafting.Domain;

/// <summary>A hole and a shaft of the same nominal size, e.g. Ø25 H7/h6.</summary>
public sealed class Fit {

    public Fit(Dimension hole, Dimension shaft) {
        ArgumentNullException.ThrowIfNull(hole);
        ArgumentNullException.ThrowIfNull(shaft);
        if (hole.Tolerance is not { IsHole: true }) {
            throw new ArgumentException("The hole must carry a hole tolerance (upper-case zone).", nameof(hole));
        }
        if (shaft.Tolerance is not { IsHole: false }) {
            throw new ArgumentException("The shaft must carry a shaft tolerance (lower-case zone).", nameof(shaft));
        }
        if (Math.Abs(hole.NominalInMillimetres - shaft.NominalInMillimetres) > 1e-9) {
            throw new ArgumentException("A fit pairs two features of the same nominal size.", nameof(shaft));
        }

        Hole  = hole;
        Shaft = shaft;
    }

    public Dimension Hole  { get; }
    public Dimension Shaft { get; }

    public double MaximumClearanceInMillimetres => Hole.UpperLimitInMillimetres - Shaft.LowerLimitInMillimetres;
    public double MinimumClearanceInMillimetres => Hole.LowerLimitInMillimetres - Shaft.UpperLimitInMillimetres;

    public FitKind Kind {
        get {
            if (MinimumClearanceInMillimetres >= 0) {
                return FitKind.Clearance;
            }

            return MaximumClearanceInMillimetres <= 0 ? FitKind.Interference : FitKind.Transition;
        }
    }

}
