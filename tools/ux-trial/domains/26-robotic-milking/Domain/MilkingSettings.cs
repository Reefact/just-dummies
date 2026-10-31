namespace RoboticMilking.Domain;

/// <summary>The robot's milking configuration for a herd.</summary>
public sealed class MilkingSettings
{
    /// <summary>Teat preparation must stimulate long enough for milk let-down (oxytocin release).</summary>
    public static readonly TimeSpan MinimumLetDownStimulation = TimeSpan.FromSeconds(30);

    /// <param name="cleaningTime">Minimum brushing time per teat, 5 to 30 seconds.</param>
    /// <param name="letDownDelay">Wait between the end of preparation and the first attachment, 0 to 90 seconds.</param>
    /// <param name="vacuumKpa">Claw vacuum, 38 to 50 kPa.</param>
    /// <param name="detachFlowThreshold">Flow (kg/min) below which a quarter's cup is removed, 0.1 to 0.5.</param>
    /// <param name="maxAttachAttempts">Attachment attempts per quarter before giving up, 1 to 5.</param>
    /// <param name="minimumInterval">Minimum time between two sessions of the same cow, 4 to 12 hours.</param>
    public MilkingSettings(
        TimeSpan cleaningTime,
        TimeSpan letDownDelay,
        decimal vacuumKpa,
        decimal detachFlowThreshold,
        int maxAttachAttempts,
        TimeSpan minimumInterval)
    {
        if (cleaningTime < TimeSpan.FromSeconds(5) || cleaningTime > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(nameof(cleaningTime), cleaningTime, "Cleaning time is 5 to 30 seconds.");
        }

        if (letDownDelay < TimeSpan.Zero || letDownDelay > TimeSpan.FromSeconds(90))
        {
            throw new ArgumentOutOfRangeException(nameof(letDownDelay), letDownDelay, "Let-down delay is 0 to 90 seconds.");
        }

        // Four teats are brushed in sequence; the cow is stimulated for the whole of it plus the delay.
        if ((cleaningTime * 4) + letDownDelay < MinimumLetDownStimulation)
        {
            throw new ArgumentException(
                $"Four teats at {cleaningTime.TotalSeconds} s plus a {letDownDelay.TotalSeconds} s delay stimulate for less than "
                + $"{MinimumLetDownStimulation.TotalSeconds} s, too short for milk let-down.",
                nameof(letDownDelay));
        }

        if (vacuumKpa < 38m || vacuumKpa > 50m)
        {
            throw new ArgumentOutOfRangeException(nameof(vacuumKpa), vacuumKpa, "Vacuum is 38 to 50 kPa.");
        }

        if (detachFlowThreshold < 0.1m || detachFlowThreshold > 0.5m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(detachFlowThreshold), detachFlowThreshold, "Detach flow threshold is 0.1 to 0.5 kg/min.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttachAttempts, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxAttachAttempts, 5);

        if (minimumInterval < TimeSpan.FromHours(4) || minimumInterval > TimeSpan.FromHours(12))
        {
            throw new ArgumentOutOfRangeException(nameof(minimumInterval), minimumInterval, "Minimum interval is 4 to 12 hours.");
        }

        CleaningTime = cleaningTime;
        LetDownDelay = letDownDelay;
        VacuumKpa = vacuumKpa;
        DetachFlowThreshold = detachFlowThreshold;
        MaxAttachAttempts = maxAttachAttempts;
        MinimumInterval = minimumInterval;
    }

    public TimeSpan CleaningTime { get; }

    public TimeSpan LetDownDelay { get; }

    public decimal VacuumKpa { get; }

    public decimal DetachFlowThreshold { get; }

    public int MaxAttachAttempts { get; }

    public TimeSpan MinimumInterval { get; }
}
