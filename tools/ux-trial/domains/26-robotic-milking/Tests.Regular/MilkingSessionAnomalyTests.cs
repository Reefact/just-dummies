using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using static RoboticMilking.Tests.Regular.MilkingSessionSupport;

namespace RoboticMilking.Tests.Regular;

[Reproducible]
public sealed class MilkingSessionAnomalyTests {

    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation)]
    [InlineData(AnomalyKind.VacuumLoss)]
    [InlineData(AnomalyKind.EmergencyStop)]
    public void A_critical_anomaly_puts_the_session_in_the_safe_state_at_once(AnomalyKind kind) {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));

        // Act
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(kind).Generate());

        // Assert
        Assert.Equal(SessionPhase.SafeState, session.Phase);
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
        Assert.All(session.Quarters, q => Assert.False(q.IsCupOn));
        Assert.True(session.IsIncomplete);
        Assert.Single(session.Anomalies);
    }

    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation)]
    [InlineData(AnomalyKind.VacuumLoss)]
    [InlineData(AnomalyKind.EmergencyStop)]
    public void A_critical_anomaly_is_accepted_at_any_stage_even_before_the_first_teat(AnomalyKind kind) {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();

        // Act
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(kind).Generate());

        // Assert
        Assert.Equal(SessionPhase.SafeState, session.Phase);
    }

    [Fact]
    public void A_critical_anomaly_leaves_the_quarters_already_milked_down_completed() {
        // Arrange
        MilkingSession session    = FourQuarterSession().WithSettings(s => s.WithDetachFlowThreshold(0.2m)).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute * 2, 0m));

        // Act
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Assert
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Aborted, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(2m, session.QuarterOf(UdderQuarter.LeftFront).YieldKg);
    }

    [Fact]
    public void A_critical_anomaly_keeps_the_quarters_that_gave_up_attaching_as_failures() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);

        // Act
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Aborted, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void Nothing_more_is_allowed_in_the_safe_state() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       cleaning   = session.Settings.CleaningTime;
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning));
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftFront, true, TimeSpan.FromMinutes(5)));
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, TimeSpan.FromMinutes(5), 1m)));
    }

    [Fact]
    public void A_second_critical_anomaly_in_the_safe_state_is_still_recorded() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Act
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.AnimalAgitation).Generate());

        // Assert
        Assert.Equal(2, session.Anomalies.Count);
        Assert.Equal(SessionPhase.SafeState, session.Phase);
    }

    [Fact]
    public void A_cup_kicked_off_aborts_that_quarter_only() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.RecordFlow(HealthyReading(UdderQuarter.RightRear, attachedAt + OneMinute, 2m));

        // Act
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.RightRear, attachedAt + OneMinute * 2));

        // Assert
        QuarterMilking kicked = session.QuarterOf(UdderQuarter.RightRear);
        Assert.Equal(QuarterState.Aborted, kicked.State);
        Assert.False(kicked.IsCupOn);
        Assert.Equal(1m, kicked.YieldKg);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.True(session.IsIncomplete);
        Assert.NotEqual(SessionPhase.SafeState, session.Phase);
    }

    [Fact]
    public void A_cup_kicked_off_right_after_attachment_aborts_the_quarter() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, attachedAt));

        // Assert
        Assert.Equal(QuarterState.Aborted, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_quarter_aborted_by_a_kick_off_takes_no_more_readings() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, attachedAt));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 1m)));
    }

    [Fact]
    public void The_session_is_completed_when_the_last_quarter_is_kicked_off() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear).Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.RightRear, attachedAt));

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.True(session.IsIncomplete);
    }

    [Fact]
    public void A_kick_off_of_a_quarter_without_a_cup_is_refused() {
        // Arrange: teats prepared, no cup attached yet.
        MilkingSession session = FourQuarterSession().Generate();
        PrepareAll(session);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, TimeSpan.FromMinutes(1))));
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact(Skip = "domain defect: MilkingSession.Apply adds the anomaly to Anomalies before CupKickOff validates that the cup is on, so a refused kick-off is still recorded; seed 983635075 (any seed, the scenario is deterministic)")]
    public void A_refused_kick_off_leaves_no_trace_in_the_anomalies() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        PrepareAll(session);

        // Act
        Assert.Throws<MilkingRefusedException>(
            () => session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, TimeSpan.FromMinutes(1))));

        // Assert: a refused operation must not change the session
        Assert.Empty(session.Anomalies);
    }

    [Theory]
    [InlineData(AnomalyKind.HighConductivity)]
    [InlineData(AnomalyKind.BloodInMilk)]
    public void A_diverting_anomaly_reported_by_the_operator_diverts_the_quarter(AnomalyKind kind) {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.ReportAnomaly(new MilkingAnomaly(kind, UdderQuarter.LeftRear, attachedAt));

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.LeftRear).IsDiverted);
        Assert.False(session.QuarterOf(UdderQuarter.RightRear).IsDiverted);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Single(session.Anomalies);
    }

    [Fact]
    public void A_quarter_diverted_before_any_milk_flows_yields_nothing_saleable() {
        // Arrange
        MilkingSession session    = FourQuarterSession().WithSettings(s => s.WithDetachFlowThreshold(0.2m)).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.BloodInMilk, UdderQuarter.LeftFront, attachedAt));

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + OneMinute, 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.RightFront, attachedAt + OneMinute, 2m));

        // Assert
        Assert.Equal(2m, session.TotalYieldKg);
        Assert.Equal(1m, session.SaleableYieldKg);
    }

    [Fact]
    public void An_anomaly_on_a_blind_quarter_is_refused() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.LeftFront).Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.HighConductivity, UdderQuarter.LeftFront, TimeSpan.Zero)));
        Assert.Empty(session.Anomalies);
    }

    [Fact]
    public void A_missing_anomaly_is_refused() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySession.ReportAnomaly(null!));
    }

    [Fact]
    public void A_released_session_takes_no_more_anomalies() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());
        session.Release(session.StartedAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate()));
    }

    [Fact]
    public void The_anomalies_are_kept_in_the_order_they_came() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.HighConductivity, UdderQuarter.LeftFront, attachedAt));
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.RightFront, attachedAt));
        session.ReportAnomaly(new MilkingAnomaly(AnomalyKind.VacuumLoss, null, attachedAt + OneMinute));

        // Assert
        Assert.Equal(new[] { AnomalyKind.HighConductivity, AnomalyKind.CupKickOff, AnomalyKind.VacuumLoss },
                     session.Anomalies.Select(a => a.Kind));
    }

}

[Reproducible]
public sealed class MilkingSessionReleaseTests {

    [Fact]
    public void A_completed_session_releases_the_cow() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       milkedAt   = MilkEverythingDown(session);
        DateTimeOffset releasedAt = session.StartedAt + milkedAt;

        // Act
        session.Release(releasedAt);

        // Assert
        Assert.Equal(SessionPhase.Released, session.Phase);
        Assert.Equal(releasedAt, session.ReleasedAt);
    }

    [Fact]
    public void A_session_in_the_safe_state_releases_the_cow() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Act
        session.Release(session.StartedAt + TimeSpan.FromMinutes(2));

        // Assert
        Assert.Equal(SessionPhase.Released, session.Phase);
    }

    [Fact]
    public void A_cow_is_not_released_before_the_session_is_over() {
        // Arrange: not started, preparing, attaching, then milking.
        MilkingSession fresh     = FourQuarterSession().Generate();
        MilkingSession preparing = FourQuarterSession().Generate();
        MilkingSession attaching = FourQuarterSession().Generate();
        MilkingSession milking   = FourQuarterSession().Generate();

        preparing.PrepareTeat(UdderQuarter.LeftFront, preparing.Settings.CleaningTime, preparing.Settings.CleaningTime);
        PrepareAll(attaching);
        TimeSpan attachedAt = AttachAll(milking);
        milking.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + TimeSpan.FromMinutes(1), 2m));

        // Assert
        foreach (MilkingSession session in new[] { fresh, preparing, attaching, milking }) {
            DateTimeOffset releasedAt = session.StartedAt + TimeSpan.FromHours(1);
            Assert.Throws<MilkingRefusedException>(() => session.Release(releasedAt));
            Assert.Null(session.ReleasedAt);
        }
    }

    [Fact]
    public void A_cow_is_not_released_while_a_cup_may_still_be_on() {
        // Arrange: three quarters milked down, the fourth still being milked.
        MilkingSession session    = FourQuarterSession().WithSettings(s => s.WithDetachFlowThreshold(0.2m)).Generate();
        TimeSpan       attachedAt = AttachAll(session);
        foreach (UdderQuarter quarter in new[] { UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear }) {
            session.RecordFlow(HealthyReading(quarter, attachedAt + TimeSpan.FromMinutes(1), 2m));
            session.RecordFlow(HealthyReading(quarter, attachedAt + TimeSpan.FromMinutes(2), 0m));
        }

        session.RecordFlow(HealthyReading(UdderQuarter.RightRear, attachedAt + TimeSpan.FromMinutes(1), 2m));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.Release(session.StartedAt + TimeSpan.FromHours(1)));
    }

    [Fact]
    public void The_cow_may_leave_at_the_very_instant_the_session_started() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Act
        session.Release(session.StartedAt);

        // Assert
        Assert.Equal(session.StartedAt, session.ReleasedAt);
    }

    [Fact]
    public void A_release_before_the_start_is_an_invalid_argument_and_leaves_the_session_as_it_was() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => session.Release(session.StartedAt.AddTicks(-1)));

        // Assert
        Assert.Equal("at", refusal.ParamName);
        Assert.Equal(SessionPhase.SafeState, session.Phase);
        Assert.Null(session.ReleasedAt);
    }

    [Fact]
    public void A_cow_is_released_only_once() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());
        session.Release(session.StartedAt + TimeSpan.FromMinutes(3));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.Release(session.StartedAt + TimeSpan.FromMinutes(4)));
        Assert.Equal(session.StartedAt + TimeSpan.FromMinutes(3), session.ReleasedAt);
    }

    [Fact]
    public void Nothing_can_be_operated_after_the_release() {
        // Arrange
        MilkingSession session  = FourQuarterSession().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());
        session.Release(session.StartedAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning));
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftFront, true, TimeSpan.FromMinutes(5)));
        Assert.Throws<MilkingRefusedException>(
            () => session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, TimeSpan.FromMinutes(5), 1m)));
    }

    [Fact]
    public void The_phase_walks_from_identification_to_completion() {
        // Arrange
        MilkingSession session    = FourQuarterSession().WithSettings(s => s.WithDetachFlowThreshold(0.2m)).Generate();
        List<SessionPhase> phases = new List<SessionPhase> { session.Phase };

        // Act
        TimeSpan cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);
        phases.Add(session.Phase);
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning * 2);
        session.PrepareTeat(UdderQuarter.LeftRear, cleaning, cleaning * 3);
        session.PrepareTeat(UdderQuarter.RightRear, cleaning, cleaning * 4);
        phases.Add(session.Phase);
        TimeSpan attachAt = cleaning * 4 + session.Settings.LetDownDelay;
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, true, attachAt);
        }

        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.RecordFlow(HealthyReading(quarter, attachAt + TimeSpan.FromMinutes(1), 2m));
            phases.Add(session.Phase);
        }

        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.RecordFlow(HealthyReading(quarter, attachAt + TimeSpan.FromMinutes(2), 0m));
        }

        phases.Add(session.Phase);
        session.Release(session.StartedAt + attachAt + TimeSpan.FromMinutes(3));
        phases.Add(session.Phase);

        // Assert
        Assert.Equal(new[] {
            SessionPhase.Identified, SessionPhase.Preparing, SessionPhase.Attaching,
            SessionPhase.Milking, SessionPhase.Milking, SessionPhase.Milking, SessionPhase.Milking,
            SessionPhase.Completed, SessionPhase.Released
        }, phases);
    }

    [Fact(Skip = "domain defect: Phase falls back from Milking to Attaching when the only quarter that yielded has finished and the others still wait for their first reading; seed 236391238")]
    public void The_phase_stays_milking_while_one_quarter_has_finished_and_another_has_yet_to_flow() {
        // Arrange: two quarters attached; the left front milks and finishes before the right front gets its first reading.
        MilkingSession session    = SessionWithBlind(UdderQuarter.LeftRear, UdderQuarter.RightRear)
                                   .WithSettings(s => s.WithDetachFlowThreshold(0.2m))
                                   .Generate();
        TimeSpan       attachedAt = AttachAll(session);

        // Act
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + TimeSpan.FromMinutes(1), 2m));
        session.RecordFlow(HealthyReading(UdderQuarter.LeftFront, attachedAt + TimeSpan.FromMinutes(2), 0m));

        // Assert
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(SessionPhase.Milking, session.Phase);
    }

}
