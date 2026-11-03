using JustDummies;

using RoboticMilking.Domain;

namespace RoboticMilking.Tests.Expert;

/// <summary>
///     Drives a session through the protocol the way the box would, reading every duration from the session's own
///     settings: a test that is not about those durations stays independent of whatever was drawn for them.
/// </summary>
internal static class Milking {

    public static List<UdderQuarter> MilkableQuartersOf(MilkingSession session) {
        return session.Quarters.Select(milking => milking.Quarter).ToList();
    }

    /// <summary>Brushes every milkable teat in turn, each for exactly the cleaning time. Returns when the last brushing ended.</summary>
    public static TimeSpan PrepareEveryTeat(MilkingSession session) {
        TimeSpan clock = TimeSpan.Zero;

        foreach (UdderQuarter quarter in MilkableQuartersOf(session)) {
            clock += session.Settings.CleaningTime;
            session.PrepareTeat(quarter, session.Settings.CleaningTime, clock);
        }

        return clock;
    }

    /// <summary>Prepares every teat, waits out the let-down delay, and attaches every cup at once. Returns the moment of attachment.</summary>
    public static TimeSpan AttachEveryCup(MilkingSession session) {
        TimeSpan attachedAt = PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        foreach (UdderQuarter quarter in MilkableQuartersOf(session)) {
            session.AttachCup(quarter, succeeded: true, attachedAt);
        }

        return attachedAt;
    }

    /// <summary>A sample from a healthy udder: no blood, conductivity at or under the mastitis threshold.</summary>
    public static MilkFlowReading HealthyReading(UdderQuarter quarter, TimeSpan elapsed, decimal flowKgPerMinute) {
        return Any.MilkFlowReading()
                  .WithQuarter(quarter)
                  .WithElapsed(elapsed)
                  .WithFlowKgPerMinute(flowKgPerMinute)
                  .WithConductivityMsPerCm(c => c.LessThanOrEqualTo(MilkFlowReading.MastitisConductivityThreshold))
                  .WithBloodDetected(false)
                  .Generate();
    }

    /// <summary>Milks one quarter: its flow rises to <paramref name="peak"/>, then falls to nothing, half a minute apart.</summary>
    
    public static void MilkToCompletion(MilkingSession session, UdderQuarter quarter, TimeSpan attachedAt, decimal peak) {
        session.RecordFlow(HealthyReading(quarter, attachedAt + TimeSpan.FromSeconds(30), peak));
        session.RecordFlow(HealthyReading(quarter, attachedAt + TimeSpan.FromSeconds(60), 0m));
    }

    /// <summary>A sample whose conductivity and blood flag the test chooses: it is about how the quarter's milk is judged.</summary>
    public static MilkFlowReading ReadingOfMilk(UdderQuarter quarter, TimeSpan elapsed, decimal flowKgPerMinute, decimal conductivityMsPerCm, bool bloodDetected) {
        return Any.MilkFlowReading()
                  .WithQuarter(quarter)
                  .WithElapsed(elapsed)
                  .WithFlowKgPerMinute(flowKgPerMinute)
                  .WithConductivityMsPerCm(conductivityMsPerCm)
                  .WithBloodDetected(bloodDetected)
                  .Generate();
    }

    /// <summary>A generator of sessions whose cow has no blind quarter: four quarters, whatever else is drawn.</summary>
    public static AnyMilkingSession SessionOfAFullUdder() {
        return Any.MilkingSession().WithAnimal(cow => cow.WithBlindQuarters(blind => blind.Empty()));
    }

    /// <summary>Takes a session of a full udder through the protocol up to the given phase. Returns the moment its cups were attached, if they were.</summary>
    public static TimeSpan DriveTo(MilkingSession session, SessionPhase phase) {
        List<UdderQuarter> quarters = MilkableQuartersOf(session);

        switch (phase) {
            case SessionPhase.Identified:
                return TimeSpan.Zero;
            case SessionPhase.Preparing:
                session.PrepareTeat(quarters[0], session.Settings.CleaningTime, session.Settings.CleaningTime);
                return TimeSpan.Zero;
            case SessionPhase.Attaching:
                PrepareEveryTeat(session);
                return TimeSpan.Zero;
            case SessionPhase.Milking: {
                TimeSpan attachedAt = AttachEveryCup(session);
                session.RecordFlow(HealthyReading(quarters[0], attachedAt + TimeSpan.FromSeconds(30), 4m));
                return attachedAt;
            }
            case SessionPhase.Completed: {
                TimeSpan attachedAt = AttachEveryCup(session);
                foreach (UdderQuarter quarter in quarters) {
                    MilkToCompletion(session, quarter, attachedAt, 4m);
                }

                return attachedAt;
            }
            case SessionPhase.SafeState:
                session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());
                return TimeSpan.Zero;
            default:
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Released is reached by releasing a session, not by driving it.");
        }
    }

}
