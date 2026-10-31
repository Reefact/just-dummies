using RoboticMilking.Domain;

namespace RoboticMilking.Tests.Regular;

/// <summary>Drives a session through the steps a test is not about, following the protocol.</summary>
internal static class SessionSteps
{
    /// <summary>Brushes every milkable teat in turn, each for the cleaning time; returns when the last brushing ended.</summary>
    public static TimeSpan PrepareEveryTeat(MilkingSession session)
    {
        TimeSpan endedAt = TimeSpan.Zero;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters)
        {
            endedAt += session.Settings.CleaningTime;
            session.PrepareTeat(quarter, session.Settings.CleaningTime, endedAt);
        }

        return endedAt;
    }

    /// <summary>Prepares every teat, then attaches every cup at the first moment let-down allows; returns that moment.</summary>
    public static TimeSpan AttachEveryCup(MilkingSession session)
    {
        TimeSpan attachedAt = PrepareEveryTeat(session) + session.Settings.LetDownDelay;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters)
        {
            session.AttachCup(quarter, succeeded: true, attachedAt);
        }

        return attachedAt;
    }
}
