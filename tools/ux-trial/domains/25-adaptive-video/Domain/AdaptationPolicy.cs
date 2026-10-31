namespace AdaptiveVideo.Domain;

/// <summary>Tuning of the hybrid throughput/buffer adaptation rule.</summary>
public sealed class AdaptationPolicy
{
    public AdaptationPolicy(double safetyFactor, double panicBufferSeconds, double comfortBufferSeconds)
    {
        if (double.IsNaN(safetyFactor) || safetyFactor < 0.1 || safetyFactor > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(safetyFactor), safetyFactor, "The safety factor must be between 0.1 and 1.");
        }

        if (double.IsNaN(panicBufferSeconds) || panicBufferSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(panicBufferSeconds), panicBufferSeconds, "The panic threshold cannot be negative.");
        }

        if (double.IsNaN(comfortBufferSeconds) || comfortBufferSeconds <= panicBufferSeconds)
        {
            throw new ArgumentException(
                $"The comfort threshold ({comfortBufferSeconds}s) must be above the panic threshold ({panicBufferSeconds}s).",
                nameof(comfortBufferSeconds));
        }

        SafetyFactor = safetyFactor;
        PanicBufferSeconds = panicBufferSeconds;
        ComfortBufferSeconds = comfortBufferSeconds;
    }

    /// <summary>Fraction of the estimated throughput the policy is willing to spend.</summary>
    public double SafetyFactor { get; }

    /// <summary>Below this level the policy drops straight to the lowest variant.</summary>
    public double PanicBufferSeconds { get; }

    /// <summary>At or above this level the policy may climb one rung.</summary>
    public double ComfortBufferSeconds { get; }
}
