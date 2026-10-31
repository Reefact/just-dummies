namespace PulmonaryNodules.Domain;

/// <summary>How a nodule's volume evolved between two exams, once segmentation uncertainty is accounted for.</summary>
public enum VolumeTrend
{
    /// <summary>The difference lies within the combined measurement uncertainty or below the clinical threshold.</summary>
    Stable,
    Growth,
    Regression,
}

/// <summary>The comparison of two measured volumes of the same nodule over an interval.</summary>
public sealed class VolumeChange
{
    /// <summary>The relative change below which a difference is not considered clinically real (25 %).</summary>
    public const double SignificantRelativeChange = 0.25;

    public VolumeChange(MeasuredVolume before, MeasuredVolume after, int intervalDays)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (intervalDays < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalDays), intervalDays, "Two exams compared must be at least one day apart.");
        }

        Before = before;
        After = after;
        IntervalDays = intervalDays;
    }

    public MeasuredVolume Before { get; }

    public MeasuredVolume After { get; }

    public int IntervalDays { get; }

    public double DeltaMm3 => After.VolumeMm3 - Before.VolumeMm3;

    public double RelativeChange => DeltaMm3 / Before.VolumeMm3;

    /// <summary>The uncertainty of the difference: the two independent uncertainties added in quadrature.</summary>
    public double CombinedUncertaintyMm3 => Math.Sqrt((Before.UncertaintyMm3 * Before.UncertaintyMm3) + (After.UncertaintyMm3 * After.UncertaintyMm3));

    /// <summary>
    /// A change is real only when it exceeds both the combined segmentation uncertainty
    /// and the clinical threshold of <see cref="SignificantRelativeChange"/>.
    /// </summary>
    public VolumeTrend Trend
    {
        get
        {
            if (Math.Abs(DeltaMm3) <= CombinedUncertaintyMm3 || Math.Abs(RelativeChange) < SignificantRelativeChange)
            {
                return VolumeTrend.Stable;
            }

            return DeltaMm3 > 0.0 ? VolumeTrend.Growth : VolumeTrend.Regression;
        }
    }

    /// <summary>The volume doubling time in days, defined only for real growth.</summary>
    public double? VolumeDoublingTimeDays =>
        Trend == VolumeTrend.Growth
            ? IntervalDays * Math.Log(2.0) / Math.Log(After.VolumeMm3 / Before.VolumeMm3)
            : null;
}
