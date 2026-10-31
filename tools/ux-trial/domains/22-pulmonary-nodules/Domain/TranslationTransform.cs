namespace PulmonaryNodules.Domain;

/// <summary>
/// A rigid translation mapping coordinates of a baseline exam into those of a follow-up exam,
/// with the residual error left after fitting it to matched landmarks.
/// </summary>
public sealed class TranslationTransform
{
    /// <summary>The largest patient shift between two acquisitions considered plausible.</summary>
    public const double MaxShiftMm = 300.0;

    /// <summary>A registration whose residual exceeds this is too poor to match nodules with.</summary>
    public const double MaxResidualErrorMm = 15.0;

    /// <summary>The fewest landmarks two exams must share to be registered.</summary>
    public const int MinimumCommonLandmarks = 3;

    public TranslationTransform(double dxMm, double dyMm, double dzMm, double residualErrorMm)
    {
        DxMm = GuardShift(dxMm, nameof(dxMm));
        DyMm = GuardShift(dyMm, nameof(dyMm));
        DzMm = GuardShift(dzMm, nameof(dzMm));
        if (double.IsNaN(residualErrorMm) || residualErrorMm < 0.0 || residualErrorMm > MaxResidualErrorMm)
        {
            throw new ArgumentOutOfRangeException(nameof(residualErrorMm), residualErrorMm, $"A registration residual must lie within [0, {MaxResidualErrorMm}] mm.");
        }

        ResidualErrorMm = residualErrorMm;
    }

    public static TranslationTransform Identity { get; } = new(0.0, 0.0, 0.0, 0.0);

    public double DxMm { get; }

    public double DyMm { get; }

    public double DzMm { get; }

    /// <summary>Root-mean-square distance between mapped baseline landmarks and their follow-up counterparts.</summary>
    public double ResidualErrorMm { get; }

    public SpatialPoint Apply(SpatialPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return new SpatialPoint(point.XMm + DxMm, point.YMm + DyMm, point.ZMm + DzMm);
    }

    public TranslationTransform Inverse() => new(-DxMm, -DyMm, -DzMm, ResidualErrorMm);

    /// <summary>
    /// Fits the translation from the landmarks both exams share: the mean displacement of the
    /// common landmarks. At least <see cref="MinimumCommonLandmarks"/> are required.
    /// </summary>
    public static TranslationTransform Estimate(IReadOnlyCollection<AnatomicalLandmark> baseline, IReadOnlyCollection<AnatomicalLandmark> followUp)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(followUp);
        Dictionary<LandmarkKind, SpatialPoint> target = followUp.ToDictionary(l => l.Kind, l => l.Position);
        List<(SpatialPoint From, SpatialPoint To)> pairs = baseline
            .Where(l => target.ContainsKey(l.Kind))
            .Select(l => (l.Position, target[l.Kind]))
            .ToList();
        if (pairs.Count < MinimumCommonLandmarks)
        {
            throw new RegistrationFailedException(
                $"Registration needs at least {MinimumCommonLandmarks} common landmarks; found {pairs.Count}.");
        }

        double dx = pairs.Average(p => p.To.XMm - p.From.XMm);
        double dy = pairs.Average(p => p.To.YMm - p.From.YMm);
        double dz = pairs.Average(p => p.To.ZMm - p.From.ZMm);
        double sumSquares = pairs.Sum(p =>
        {
            double ex = p.From.XMm + dx - p.To.XMm;
            double ey = p.From.YMm + dy - p.To.YMm;
            double ez = p.From.ZMm + dz - p.To.ZMm;
            return (ex * ex) + (ey * ey) + (ez * ez);
        });
        double residual = Math.Sqrt(sumSquares / pairs.Count);
        if (residual > MaxResidualErrorMm)
        {
            throw new RegistrationFailedException(
                $"Landmark residual {residual:0.0} mm exceeds the {MaxResidualErrorMm} mm tolerance: the exams cannot be registered reliably.");
        }

        if (Math.Abs(dx) > MaxShiftMm || Math.Abs(dy) > MaxShiftMm || Math.Abs(dz) > MaxShiftMm)
        {
            throw new RegistrationFailedException($"The estimated shift exceeds {MaxShiftMm} mm on at least one axis.");
        }

        return new TranslationTransform(dx, dy, dz, residual);
    }

    private static double GuardShift(double value, string name)
    {
        if (double.IsNaN(value) || Math.Abs(value) > MaxShiftMm)
        {
            throw new ArgumentOutOfRangeException(name, value, $"A shift must lie within ±{MaxShiftMm} mm.");
        }

        return value;
    }
}
