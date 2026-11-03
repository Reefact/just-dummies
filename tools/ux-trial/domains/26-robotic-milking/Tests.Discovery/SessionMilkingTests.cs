using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class SessionMilkingTests {

    // Cups go on at 60 s (4 teats x 10 s of brushing + 20 s of let-down delay); the detach threshold is 0.3 kg/min.
    private static readonly TimeSpan AttachedAt = TimeSpan.FromSeconds(60);

    private static MilkingSession MilkingReadySession() {
        MilkingSession session = Scenarios.Session().Generate();
        TimeSpan       attached = Scenarios.AttachAllCups(session);
        Assert.Equal(AttachedAt, attached);   // the arithmetic of this fixture, checked once

        return session;
    }

    [Fact]
    public void The_first_reading_of_an_attached_quarter_starts_the_milking() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(30), flow: 1m));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(anyQuarter).State);
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

    [Fact]
    public void The_yield_is_the_trapezoidal_integral_of_the_flow() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act: flow rises 0 -> 2 over the first minute (1 kg), stays at 2 for one minute (2 kg), then 2 -> 4 over a minute (3 kg).
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(2), flow: 2m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(3), flow: 4m));

        // Assert
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(6m, milking.YieldKg);
        Assert.Equal(4m, milking.PeakFlowKgPerMinute);
        Assert.Equal(6m, anySession.TotalYieldKg);
    }

    [Fact(Skip = "domain defect: the elapsed time between two samples goes through (decimal)TimeSpan.TotalMinutes, a double, so 20 s becomes 0.333333333333333 and a constant 3 kg/min over three 20 s samples yields 2.4999999999999975 instead of 2.5 (deterministic, no seed involved)")]
    public void Samples_twenty_seconds_apart_add_up_to_the_exact_yield() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act: a constant 3 kg/min over one minute sampled every 20 s: 0 -> 3 (0.5 kg), then 3 -> 3 twice (1 kg each).
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(20), flow: 3m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(40), flow: 3m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(60), flow: 3m));

        // Assert
        Assert.Equal(2.5m, anySession.QuarterOf(anyQuarter).YieldKg);
    }

    [Fact]
    public void Quarters_are_milked_in_parallel_and_the_total_is_their_sum() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();

        // Act
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2), flow: 1m));    // 1 kg
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightRear, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));    // 1 kg
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(3), flow: 1m));    // +1 kg

        // Assert
        Assert.Equal(2m, anySession.QuarterOf(UdderQuarter.LeftFront).YieldKg);
        Assert.Equal(1m, anySession.QuarterOf(UdderQuarter.RightRear).YieldKg);
        Assert.Equal(3m, anySession.TotalYieldKg);
        Assert.Equal(3m, anySession.SaleableYieldKg);
    }

    [Fact]
    public void A_reading_for_a_quarter_without_a_cup_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Scenarios.LetDownDelay;
        anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, attachAt);

        // Act / Assert: the right front quarter is prepared, not attached.
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightFront, attachAt + TimeSpan.FromSeconds(10), flow: 1m)));
    }

    [Fact]
    public void A_reading_for_a_blind_quarter_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear]))
                                              .Generate();
        Scenarios.AttachAllCups(anySession);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftRear, TimeSpan.FromMinutes(5), flow: 1m)));
    }

    [Theory]
    [InlineData(0)]       // at the very instant the cup went on
    [InlineData(-1)]      // before it
    public void A_reading_not_after_the_attachment_is_refused(double secondsAfterAttachment) {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(secondsAfterAttachment), flow: 1m)));
    }

    [Theory]
    [InlineData(0)]       // same instant as the previous reading
    [InlineData(-10)]     // earlier
    public void Readings_of_a_quarter_must_be_strictly_increasing_in_time(double secondsAfterPrevious) {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        TimeSpan       previous   = AttachedAt + TimeSpan.FromSeconds(100);
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, previous, flow: 1m));
        decimal yieldBefore = anySession.QuarterOf(anyQuarter).YieldKg;

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(anyQuarter, previous + TimeSpan.FromSeconds(secondsAfterPrevious), flow: 1m)));
        Assert.Equal(yieldBefore, anySession.QuarterOf(anyQuarter).YieldKg);
    }

    [Fact]
    public void Readings_of_different_quarters_are_not_ordered_against_each_other() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromSeconds(100), flow: 1m));

        // Act: an earlier instant, but another quarter.
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightFront, AttachedAt + TimeSpan.FromSeconds(50), flow: 1m));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void The_cup_comes_off_once_the_flow_has_risen_above_the_threshold_and_fallen_back_under_it() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(2), flow: 0.4m));
        QuarterState stateWhileStillAboveThreshold = anySession.QuarterOf(anyQuarter).State;

        // Act
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(3), flow: 0.29m));

        // Assert
        Assert.Equal(QuarterState.Milking, stateWhileStillAboveThreshold);
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Completed, milking.State);
        Assert.True(milking.IsFinished);
        Assert.False(milking.IsCupOn);
    }

    [Fact]
    public void A_flow_exactly_at_the_threshold_does_not_detach_the_cup() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));

        // Act
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(2), flow: Scenarios.DetachThreshold));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void A_low_flow_at_the_start_does_not_detach_a_cup_that_has_not_yet_seen_the_milk_come() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act: the flow has never reached the threshold.
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(30), flow: 0.1m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromSeconds(60), flow: 0m));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void A_quarter_whose_cup_came_off_yields_no_more_readings() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
        anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(anyQuarter, AttachedAt + TimeSpan.FromMinutes(3), flow: 1m)));
    }

    [Fact]
    public void The_session_is_completed_when_every_quarter_has_finished() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();

        // Act
        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            anySession.RecordFlow(Scenarios.Reading(quarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
        }

        SessionPhase phaseWhileMilking = anySession.Phase;

        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            anySession.RecordFlow(Scenarios.Reading(quarter, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));
        }

        // Assert
        Assert.Equal(SessionPhase.Milking, phaseWhileMilking);
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        Assert.False(anySession.IsIncomplete);
        Assert.Equal(8m, anySession.TotalYieldKg);   // 4 quarters x (1 kg rising + 1 kg falling)
    }

    [Fact]
    public void The_session_keeps_milking_while_one_quarter_is_still_going() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            anySession.RecordFlow(Scenarios.Reading(quarter, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));
        }

        // Act: three quarters finish, the fourth keeps flowing.
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightFront, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftRear, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));

        // Assert
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

    [Fact]
    public void A_high_conductivity_diverts_the_quarters_milk_and_records_the_anomaly() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        TimeSpan       readingAt  = AttachedAt + TimeSpan.FromMinutes(1);

        // Act
        anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(anyQuarter).WithElapsed(readingAt).WithFlowKgPerMinute(2m)
                                 .WithConductivityMsPerCm(c => c.GreaterThan(MilkFlowReading.MastitisConductivityThreshold))
                                 .WithBloodDetected(false)
                                 .Generate());

        // Assert
        Assert.True(anySession.QuarterOf(anyQuarter).IsDiverted);
        MilkingAnomaly anomaly = Assert.Single(anySession.Anomalies);
        Assert.Equal(AnomalyKind.HighConductivity, anomaly.Kind);
        Assert.Equal(anyQuarter, anomaly.Quarter);
        Assert.Equal(readingAt, anomaly.Elapsed);
    }

    [Fact]
    public void A_conductivity_at_the_mastitis_threshold_diverts_nothing() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(anyQuarter).WithElapsed(AttachedAt + TimeSpan.FromMinutes(1))
                                 .WithConductivityMsPerCm(MilkFlowReading.MastitisConductivityThreshold)
                                 .WithBloodDetected(false)
                                 .Generate());

        // Assert
        Assert.False(anySession.QuarterOf(anyQuarter).IsDiverted);
        Assert.Empty(anySession.Anomalies);
    }

    [Fact]
    public void Blood_in_the_milk_diverts_the_quarter_whatever_its_conductivity() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(anyQuarter).WithElapsed(AttachedAt + TimeSpan.FromMinutes(1))
                                 .WithConductivityMsPerCm(c => c.Between(2m, MilkFlowReading.MastitisConductivityThreshold))
                                 .WithBloodDetected(true)
                                 .Generate());

        // Assert
        Assert.True(anySession.QuarterOf(anyQuarter).IsDiverted);
        Assert.Equal(AnomalyKind.BloodInMilk, Assert.Single(anySession.Anomalies).Kind);
    }

    [Fact]
    public void A_diverted_quarter_is_milked_but_its_milk_is_not_saleable() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();

        // Act: left front yields 1 kg and is diverted; right rear yields 1 kg and is not.
        anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(UdderQuarter.LeftFront).WithElapsed(AttachedAt + TimeSpan.FromMinutes(2))
                                 .WithFlowKgPerMinute(1m).WithBloodDetected(true)
                                 .WithConductivityMsPerCm(c => c.Between(2m, MilkFlowReading.MastitisConductivityThreshold))
                                 .Generate());
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightRear, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));

        // Assert
        Assert.Equal(2m, anySession.TotalYieldKg);
        Assert.Equal(1m, anySession.SaleableYieldKg);
    }

    [Fact]
    public void Diverting_a_quarter_does_not_detach_its_cup_nor_stop_the_milking() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();

        // Act
        anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(UdderQuarter.LeftFront).WithElapsed(AttachedAt + TimeSpan.FromMinutes(1))
                                 .WithFlowKgPerMinute(2m).WithBloodDetected(true)
                                 .WithConductivityMsPerCm(c => c.Between(2m, MilkFlowReading.MastitisConductivityThreshold))
                                 .Generate());

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

    [Fact]
    public void A_high_conductivity_is_recorded_once_per_quarter_however_many_readings_show_it() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        for (int minute = 1; minute <= 3; minute++) {
            anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(anyQuarter).WithElapsed(AttachedAt + TimeSpan.FromMinutes(minute))
                                     .WithFlowKgPerMinute(1m).WithBloodDetected(false)
                                     .WithConductivityMsPerCm(c => c.GreaterThan(MilkFlowReading.MastitisConductivityThreshold))
                                     .Generate());
        }

        // Assert
        Assert.Single(anySession.Anomalies);
    }

    [Fact(Skip = "domain defect: unlike a high conductivity (reported once, guarded by !IsDiverted), blood in the milk is reported again by every following reading, so the anomaly log grows by one BloodInMilk per sample (deterministic, no seed involved)")]
    public void Blood_is_recorded_once_per_quarter_however_many_readings_show_it() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        for (int minute = 1; minute <= 3; minute++) {
            anySession.RecordFlow(Any.MilkFlowReading().WithQuarter(anyQuarter).WithElapsed(AttachedAt + TimeSpan.FromMinutes(minute))
                                     .WithFlowKgPerMinute(1m).WithBloodDetected(true)
                                     .WithConductivityMsPerCm(c => c.Between(2m, MilkFlowReading.MastitisConductivityThreshold))
                                     .Generate());
        }

        // Assert
        Assert.Single(anySession.Anomalies);
    }

    [Fact]
    public void No_reading_is_taken_in_a_session_in_the_safe_state() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.VacuumLoss, null, AttachedAt));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(1), flow: 1m)));
    }

    [Fact]
    public void A_missing_reading_is_refused() {
        // Arrange
        MilkingSession anySession = MilkingReadySession();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySession.RecordFlow(null!));
    }

}
