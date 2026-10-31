using JustDummies;

using RoboticMilking.Domain;

namespace RoboticMilking.Tests.Discovery.Tests;

/// <summary>Drives a session through its phases with arbitrary, protocol-respecting timings.</summary>
internal static class SessionSteps {

    /// <summary>
    ///     An instant a robot may read an ear tag at. Bounded so a test can step hours back or forward
    ///     from it: unconstrained, Any.DateTimeOffset() spans MinValue to MaxValue.
    /// </summary>
    public static AnyDateTimeOffset AnyMilkingInstant() {
        return Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>Brushes every milkable teat once, one after another; returns when the last brushing ended.</summary>
    public static TimeSpan PrepareEveryTeat(MilkingSession session) {
        TimeSpan endedAt = TimeSpan.Zero;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            TimeSpan anyBrushingTime = Any.TimeSpan()
                                          .Between(session.Settings.CleaningTime, session.Settings.CleaningTime + TimeSpan.FromSeconds(10))
                                          .Generate();
            endedAt += anyBrushingTime;
            session.PrepareTeat(quarter, anyBrushingTime, endedAt);
        }

        return endedAt;
    }

    /// <summary>The earliest moment a cup may go on, once every teat is prepared.</summary>
    public static TimeSpan LetDownReachedAfter(MilkingSession session, TimeSpan preparationEnd) {
        return preparationEnd + session.Settings.LetDownDelay;
    }

    /// <summary>Prepares every teat and attaches every cup at the first try; returns the attachment time.</summary>
    public static TimeSpan AttachEveryCup(MilkingSession session) {
        TimeSpan attachedAt = LetDownReachedAfter(session, PrepareEveryTeat(session));
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, succeeded: true, attachedAt);
        }

        return attachedAt;
    }

    /// <summary>A reading of healthy milk: no mastitis signal, no blood.</summary>
    public static AnyMilkFlowReading HealthyReading() {
        return Any.MilkFlowReading()
                  .WithConductivityMsPerCm(c => c.LessThanOrEqualTo(MilkFlowReading.MastitisConductivityThreshold))
                  .WithBloodDetected(false);
    }

}
