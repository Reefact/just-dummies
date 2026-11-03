using JustDummies;

using RoboticMilking.Domain;

namespace RoboticMilking.Tests.Discovery;

/// <summary>
///     Shared set-ups for the session tests. The protocol numbers the session tests reason about are literals
///     (10 s of brushing, 20 s of let-down delay, a 0.3 kg/min detach threshold, three attempts); everything
///     else about the cow, the box and the settings is arbitrary.
/// </summary>
internal static class Scenarios {

    public static readonly TimeSpan CleaningTime  = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan LetDownDelay  = TimeSpan.FromSeconds(20);
    public const           decimal  DetachThreshold = 0.3m;
    public const           int      MaxAttempts   = 3;

    /// <summary>A session whose protocol numbers are the literals above; a sound cow unless a test says otherwise.</summary>
    public static AnyMilkingSession Session() {
        return Any.MilkingSession()
                  .WithAnimal(a => a.WithBlindQuarters(Array.Empty<UdderQuarter>()))
                  .WithSettings(s => s.WithCleaningTime(CleaningTime)
                                      .WithLetDownDelay(LetDownDelay)
                                      .WithDetachFlowThreshold(DetachThreshold)
                                      .WithMaxAttachAttempts(MaxAttempts));
    }

    /// <summary>Brushes every milkable teat in turn, back to back; returns when the last brushing ended.</summary>
    public static TimeSpan PrepareAllTeats(MilkingSession session) {
        TimeSpan now = TimeSpan.Zero;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            now += session.Settings.CleaningTime;
            session.PrepareTeat(quarter, session.Settings.CleaningTime, now);
        }

        return now;
    }

    /// <summary>Prepares every teat then attaches every cup at the first moment the let-down delay allows; returns that moment.</summary>
    public static TimeSpan AttachAllCups(MilkingSession session) {
        TimeSpan attachedAt = PrepareAllTeats(session) + session.Settings.LetDownDelay;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, succeeded: true, attachedAt);
        }

        return attachedAt;
    }

    /// <summary>A reading of the given quarter with the given time and flow; the rest of the sample is arbitrary and unremarkable.</summary>
    public static MilkFlowReading Reading(UdderQuarter quarter, TimeSpan elapsed, decimal flow) {
        return Any.MilkFlowReading()
                  .WithQuarter(quarter)
                  .WithElapsed(elapsed)
                  .WithFlowKgPerMinute(flow)
                  .WithConductivityMsPerCm(c => c.Between(2m, MilkFlowReading.MastitisConductivityThreshold))
                  .WithBloodDetected(false)
                  .Generate();
    }

}
