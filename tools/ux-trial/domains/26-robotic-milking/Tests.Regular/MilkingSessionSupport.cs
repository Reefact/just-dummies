using JustDummies;

using RoboticMilking.Domain;

namespace RoboticMilking.Tests.Regular;

/// <summary>Arrange helpers shared by the session tests: they walk a session through its stages.</summary>
internal static class MilkingSessionSupport {

    public static readonly UdderQuarter[] NoBlindQuarter = Array.Empty<UdderQuarter>();

    /// <summary>A session of a cow with no blind quarter, so that all four teats are milked.</summary>
    public static AnyMilkingSession FourQuarterSession() {
        return Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(NoBlindQuarter));
    }

    /// <summary>A session for a cow with the given quarters blind.</summary>
    public static AnyMilkingSession SessionWithBlind(params UdderQuarter[] blind) {
        return Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(blind));
    }

    /// <summary>Brushes every milkable teat back to back, each for exactly the cleaning time; returns when the last brushing ended.</summary>
    public static TimeSpan PrepareAll(MilkingSession session) {
        TimeSpan endedAt = TimeSpan.Zero;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            endedAt += session.Settings.CleaningTime;
            session.PrepareTeat(quarter, session.Settings.CleaningTime, endedAt);
        }

        return endedAt;
    }

    /// <summary>Attaches every milkable cup at the first allowed instant; returns that instant.</summary>
    public static TimeSpan AttachAll(MilkingSession session) {
        TimeSpan preparedAt = PrepareAll(session);
        TimeSpan attachedAt = preparedAt + session.Settings.LetDownDelay;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, true, attachedAt);
        }

        return attachedAt;
    }

    /// <summary>A healthy sample: conductivity under the mastitis threshold, no blood.</summary>
    public static MilkFlowReading HealthyReading(UdderQuarter quarter, TimeSpan elapsed, decimal flow) {
        return Any.MilkFlowReading()
                  .WithQuarter(quarter)
                  .WithElapsed(elapsed)
                  .WithFlowKgPerMinute(flow)
                  .WithConductivityMsPerCm(c => c.LessThanOrEqualTo(MilkFlowReading.MastitisConductivityThreshold))
                  .WithBloodDetected(false)
                  .Generate();
    }

    /// <summary>Drives a session to the Completed phase: every cup attached, then every quarter milked down below the threshold.</summary>
    public static TimeSpan MilkEverythingDown(MilkingSession session) {
        TimeSpan attachedAt = AttachAll(session);
        TimeSpan peakAt     = attachedAt + TimeSpan.FromMinutes(1);
        TimeSpan endAt      = attachedAt + TimeSpan.FromMinutes(2);
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.RecordFlow(HealthyReading(quarter, peakAt, 2m));
            session.RecordFlow(HealthyReading(quarter, endAt, 0m));
        }

        return endAt;
    }

}
