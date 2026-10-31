namespace RoboticMilking.Tests.Expert;

/// <summary>Reusable recipes: what a reading, a brushing or a wait looks like when a test does not care.</summary>
internal static class Recipes {

    /// <summary>A sensor sample with no mastitis or blood signal; everything else arbitrary.</summary>
    public static AnyMilkFlowReading HealthyReading() {
        return Any.MilkFlowReading()
                  .WithConductivityMsPerCm(c => c.LessThanOrEqualTo(MilkFlowReading.MastitisConductivityThreshold))
                  .WithBloodDetected(false);
    }

    /// <summary>A healthy sample for one quarter, at one moment, with one flow: the three things a flow test is about.</summary>
    public static MilkFlowReading HealthyReading(UdderQuarter quarter, TimeSpan elapsed, decimal flowKgPerMinute) {
        return HealthyReading().WithQuarter(quarter).WithElapsed(elapsed).WithFlowKgPerMinute(flowKgPerMinute).Generate();
    }

    /// <summary>When a cow enters a box: any second of the trial year.</summary>
    public static AnyDateTimeOffset AnySessionStart() {
        return Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero))
                  .WithGranularity(TimeSpan.FromSeconds(1));
    }

    /// <summary>A short, arbitrary wait — a pause between two physical events whose length the rule ignores.</summary>
    public static AnyTimeSpan ShortWait() {
        return Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromSeconds(10)).WithGranularity(TimeSpan.FromSeconds(1));
    }

    /// <summary>A brushing long enough for the protocol, by an arbitrary margin.</summary>
    public static AnyTimeSpan CompliantBrushing(MilkingSettings settings) {
        return Any.TimeSpan()
                  .Between(settings.CleaningTime, settings.CleaningTime + TimeSpan.FromSeconds(10))
                  .WithGranularity(TimeSpan.FromSeconds(1));
    }

    /// <summary>The cow's milkable quarters in an arbitrary order: preparation is sequential, the order is free.</summary>
    public static IReadOnlyList<UdderQuarter> AnyPreparationOrder(Animal animal) {
        IReadOnlyList<UdderQuarter> milkable = animal.MilkableQuarters;

        // No permutation generator: n distinct elements out of a pool of n is one.
        return Any.ListOf(Any.ElementOf(milkable)).WithCount(milkable.Count).Distinct().Generate();
    }

}

/// <summary>Drives a session through the physical steps a test needs behind it, with arbitrary timings.</summary>
internal static class SessionDriver {

    /// <summary>Brushes every milkable teat, one after the other; returns when the last brushing ended.</summary>
    public static TimeSpan PrepareEveryTeat(this MilkingSession session) {
        TimeSpan end = TimeSpan.Zero;
        foreach (UdderQuarter quarter in Recipes.AnyPreparationOrder(session.Animal)) {
            TimeSpan anyBrushing = Recipes.CompliantBrushing(session.Settings).Generate();
            end += Recipes.ShortWait().Generate() + anyBrushing;
            session.PrepareTeat(quarter, anyBrushing, end);
        }

        return end;
    }

    /// <summary>Prepares every teat, waits for let-down, attaches every cup at the first attempt; returns the attachment time.</summary>
    public static TimeSpan AttachEveryCup(this MilkingSession session) {
        TimeSpan preparedAt = session.PrepareEveryTeat();
        TimeSpan attachedAt = preparedAt + session.Settings.LetDownDelay + Recipes.ShortWait().Generate();
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, succeeded: true, attachedAt);
        }

        return attachedAt;
    }

    /// <summary>Milks one attached quarter to the end: flow rises to <paramref name="peak" />, then falls to zero.</summary>
    public static TimeSpan MilkOut(this MilkingSession session, UdderQuarter quarter, TimeSpan from, decimal peak) {
        session.RecordFlow(Recipes.HealthyReading(quarter, from + TimeSpan.FromMinutes(1), peak));
        session.RecordFlow(Recipes.HealthyReading(quarter, from + TimeSpan.FromMinutes(2), 0m));

        return from + TimeSpan.FromMinutes(2);
    }

    public static UdderQuarter AnyMilkableQuarter(this MilkingSession session) {
        return Any.ElementOf(session.Animal.MilkableQuarters).Generate();
    }

}
