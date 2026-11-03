using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using static RoboticMilking.Tests.Regular.MilkingSessionSupport;

namespace RoboticMilking.Tests.Regular;

[Reproducible]
public sealed class MilkingSessionFlowTests {

    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    // A detach threshold the tests can reason with: 0.2 kg/min.
    private static AnyMilkingSession WithThreshold(decimal threshold) {
        return FourQuarterSession().WithSettings(s => s.WithDetachFlowThreshold(threshold));
    }

    [Fact]
    public void The_first_reading_after_attachment_starts_the_milking() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 1.5m));

        // Assert
        Assert.Equal(QuarterState.Milking, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(SessionPhase.Milking, session.Phase);
    }

    [Fact]
    public void The_yield_is_the_area_under_the_flow_curve() {
        // Arrange: a threshold of 0.2 kg/min is never crossed downwards here.
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act: 0 -> 2 over a minute = 1 kg; 2 -> 4 over a minute = 3 kg; 4 -> 4 over half a minute = 2 kg.
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 4m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2 + TimeSpan.FromSeconds(30), 4m));

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(6m, quarter.YieldKg);
        Assert.Equal(4m, quarter.PeakFlowKgPerMinute);
        Assert.Equal(6m, session.TotalYieldKg);
    }

    [Fact]
    public void The_total_yield_adds_up_the_quarters() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act: each quarter gets one minute from 0 up to its own flow.
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));    // 1 kg
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute, 4m));   // 2 kg
        session.RecordFlow(HealthyReading(UdderQuarter.LeftRear, attachedAt + OneMinute, 1m));     // 0.5 kg
        session.RecordFlow(HealthyReading(UdderQuarter.RightRear, attachedAt + OneMinute, 3m));    // 1.5 kg

        // Assert
        Assert.Equal(5m, session.TotalYieldKg);
        Assert.Equal(5m, session.SaleableYieldKg);
    }

    [Fact]
    public void Quarters_are_milked_in_parallel_with_their_own_clock() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act: the right front is read later than the left front, then the left front again.
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 3, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 4, 2m));

        // Assert
        Assert.Equal(3m + 2m, session.QuarterOf(UdderQuarter.LeftFront).YieldKg);
        Assert.Equal(1m, session.QuarterOf(UdderQuarter.RightFront).YieldKg);
    }

    [Fact]
    public void A_reading_at_the_very_instant_of_attachment_is_not_after_it() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt, 1m)));

        Assert.Contains("not after", refusal.Message);
        Assert.Equal(0m, session.QuarterOf(UdderQuarter.LeftFront).YieldKg);
    }

    [Fact]
    public void Readings_of_a_quarter_must_strictly_increase_in_time() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 3m));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 3m)));
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 3m)));
        Assert.Equal(3m, session.QuarterOf(UdderQuarter.LeftFront).YieldKg);
    }

    [Fact]
    public void A_quarter_without_a_cup_yields_no_reading() {
        // Arrange: all prepared, none attached.
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       preparedAt = PrepareAll(session);

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, preparedAt + session.Settings.LetDownDelay + OneMinute, 1m)));

        Assert.Contains("no cup", refusal.Message);
    }

    [Fact]
    public void A_quarter_that_gave_up_attaching_yields_no_reading() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachAt + OneMinute, 1m)));
    }

    [Fact]
    public void A_blind_quarter_yields_no_reading() {
        // Arrange
        MilkingSession session    = SessionWithBlind(UdderQuarter.RightRear).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.RightRear, attachedAt + OneMinute, 1m)));
    }

    [Fact]
    public void A_missing_reading_is_refused() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySession.RecordFlow(null!));
    }

    [Fact]
    public void The_cup_comes_off_when_the_flow_falls_below_the_threshold_after_having_reached_it() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 3m));

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0.19m));

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(QuarterState.Completed, quarter.State);
        Assert.True(quarter.IsFinished);
        Assert.False(quarter.IsCupOn);
    }

    [Fact]
    public void A_flow_equal_to_the_threshold_does_not_detach() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 3m));

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0.2m));

        // Assert
        Assert.Equal(QuarterState.Milking, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_flow_that_never_reached_the_threshold_does_not_detach_the_cup() {
        // Arrange: a slow start below the threshold is not the end of the milking.
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 0.05m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0m));

        // Assert
        Assert.Equal(QuarterState.Milking, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_flow_exactly_reaching_the_threshold_arms_the_detachment() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 0.2m));

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0.1m));

        // Assert
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_detached_quarter_takes_no_further_reading_and_keeps_its_yield() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0m));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 3, 1m)));
        Assert.Equal(2m, session.QuarterOf(UdderQuarter.LeftFront).YieldKg);
    }

    [Fact]
    public void One_quarter_finishing_does_not_stop_the_others() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute, 2m));

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0m));
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute * 2, 1m));

        // Assert
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Milking, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(SessionPhase.Milking, session.Phase);
    }

    [Fact]
    public void The_session_is_completed_once_every_quarter_is_milked_down() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();

        // Act
        MilkEverythingDown(session);

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.False(session.IsIncomplete);
        Assert.Equal(8m, session.TotalYieldKg);   // 2 kg per quarter: (0+2)/2 x 1 min, then (2+0)/2 x 1 min
    }

    [Fact]
    public void A_cow_with_blind_quarters_is_completed_without_them() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.LeftFront, UdderQuarter.RightRear).Generate();

        // Act
        MilkEverythingDown(session);

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.Equal(4m, session.TotalYieldKg);   // two milkable quarters, 2 kg each
    }

    [Fact]
    public void A_session_where_some_quarters_gave_up_attaching_is_incomplete_but_completed() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);
        foreach (UdderQuarter quarter in new[] { UdderQuarter.RightFront, UdderQuarter.LeftRear, UdderQuarter.RightRear }) {
            session.AttachCup(quarter, true, attachAt);
            session.RecordFlow(HealthyReading(quarter, attachAt + OneMinute, 2m));
            session.RecordFlow(HealthyReading(quarter, attachAt + OneMinute * 2, 0m));
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.True(session.IsIncomplete);
        Assert.Equal(6m, session.TotalYieldKg);
    }

    [Fact]
    public void Conductivity_above_the_mastitis_threshold_diverts_the_quarter_and_records_an_anomaly() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);
        TimeSpan       readingAt  = attachedAt + OneMinute;

        // Act
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.LeftRear)
                              .WithElapsed(readingAt)
                              .WithFlowKgPerMinute(2m)
                              .WithConductivityMsPerCm(7.6m)
                              .WithBloodDetected(false)
                              .Generate());

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.LeftRear).IsDiverted);
        Assert.False(session.QuarterOf(UdderQuarter.LeftFront).IsDiverted);
        MilkingAnomaly anomaly = Assert.Single(session.Anomalies);
        Assert.Equal(AnomalyKind.HighConductivity, anomaly.Kind);
        Assert.Equal(UdderQuarter.LeftRear, anomaly.Quarter);
        Assert.Equal(readingAt, anomaly.Elapsed);
    }

    [Fact]
    public void Conductivity_at_the_mastitis_threshold_is_not_an_anomaly() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.LeftRear)
                              .WithElapsed(attachedAt + OneMinute)
                              .WithConductivityMsPerCm(7.5m)
                              .WithBloodDetected(false)
                              .Generate());

        // Assert
        Assert.False(session.QuarterOf(UdderQuarter.LeftRear).IsDiverted);
        Assert.Empty(session.Anomalies);
    }

    [Fact]
    public void Blood_in_the_milk_diverts_the_quarter_and_records_an_anomaly() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.RightFront)
                              .WithElapsed(attachedAt + OneMinute)
                              .WithConductivityMsPerCm(c => c.LessThanOrEqualTo(7.5m))
                              .WithBloodDetected(true)
                              .Generate());

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.RightFront).IsDiverted);
        MilkingAnomaly anomaly = Assert.Single(session.Anomalies);
        Assert.Equal(AnomalyKind.BloodInMilk, anomaly.Kind);
        Assert.Equal(UdderQuarter.RightFront, anomaly.Quarter);
    }

    [Fact]
    public void A_reading_with_both_signals_records_both_anomalies() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.RightFront)
                              .WithElapsed(attachedAt + OneMinute)
                              .WithConductivityMsPerCm(9m)
                              .WithBloodDetected(true)
                              .Generate());

        // Assert
        Assert.Equal(new[] { AnomalyKind.HighConductivity, AnomalyKind.BloodInMilk }, session.Anomalies.Select(a => a.Kind));
    }

    [Fact]
    public void A_diverted_quarters_milk_is_yielded_but_not_saleable() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act: the left front is diverted, the others are healthy; 1 kg each.
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.LeftFront)
                              .WithElapsed(attachedAt + OneMinute)
                              .WithFlowKgPerMinute(2m)
                              .WithConductivityMsPerCm(10m)
                              .WithBloodDetected(false)
                              .Generate());
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftRear, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.RightRear, attachedAt + OneMinute, 2m));

        // Assert
        Assert.Equal(4m, session.TotalYieldKg);
        Assert.Equal(3m, session.SaleableYieldKg);
    }

    [Fact]
    public void The_whole_quarter_is_diverted_not_only_the_milk_after_the_signal() {
        // Arrange: 1 kg healthy, then 1 kg after the conductivity rose.
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));

        // Act
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.LeftFront)
                              .WithElapsed(attachedAt + OneMinute * 2)
                              .WithFlowKgPerMinute(2m)
                              .WithConductivityMsPerCm(12m)
                              .WithBloodDetected(false)
                              .Generate());

        // Assert
        Assert.Equal(3m, session.TotalYieldKg);
        Assert.Equal(0m, session.SaleableYieldKg);
    }

    [Fact]
    public void A_quarter_stays_diverted_when_its_conductivity_comes_back_down() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(Any.MilkFlowReading()
                              .WithQuarter(UdderQuarter.LeftFront)
                              .WithElapsed(attachedAt + OneMinute)
                              .WithConductivityMsPerCm(9m)
                              .WithBloodDetected(false)
                              .Generate());

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 2m));

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.LeftFront).IsDiverted);
    }

    [Fact]
    public void Mastitis_is_recorded_once_per_quarter_however_many_readings_show_it() {
        // Arrange
        MilkingSession session    = WithThreshold(0.2m).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        for (int minute = 1; minute <= 3; minute++) {
            session.RecordFlow(Any.MilkFlowReading()
                                  .WithQuarter(UdderQuarter.LeftFront)
                                  .WithElapsed(attachedAt + OneMinute * minute)
                                  .WithFlowKgPerMinute(2m)
                                  .WithConductivityMsPerCm(9m)
                                  .WithBloodDetected(false)
                                  .Generate());
        }

        // Assert
        Assert.Single(session.Anomalies);
    }

}
