using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class SessionAnomalyTests {

    public static TheoryData<AnomalyKind> CriticalKinds => new() { AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop };
    public static TheoryData<AnomalyKind> DivertingKinds => new() { AnomalyKind.HighConductivity, AnomalyKind.BloodInMilk };

    private static readonly TimeSpan AttachedAt = TimeSpan.FromSeconds(60);

    private static MilkingSession MilkingSession() {
        MilkingSession session = Scenarios.Session().Generate();
        Scenarios.AttachAllCups(session);
        session.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(1), flow: 2m));

        return session;
    }

    [Theory]
    [MemberData(nameof(CriticalKinds))]
    public void A_critical_anomaly_puts_the_session_in_the_safe_state_at_once(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = MilkingSession();
        MilkingAnomaly anyCritical = Any.MilkingAnomaly().WithKind(kind).Generate();

        // Act
        anySession.ReportAnomaly(anyCritical);

        // Assert
        Assert.Equal(SessionPhase.SafeState, anySession.Phase);
        Assert.Same(anyCritical, Assert.Single(anySession.Anomalies));
    }

    [Theory]
    [MemberData(nameof(CriticalKinds))]
    public void A_critical_anomaly_aborts_every_quarter_still_in_progress(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = MilkingSession();

        // Act
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(kind).Generate());

        // Assert
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
        Assert.All(anySession.Quarters, q => Assert.False(q.IsCupOn));
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_critical_anomaly_in_the_middle_of_the_preparation_aborts_the_quarters_not_yet_prepared() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, Scenarios.CleaningTime, endedAt: Scenarios.CleaningTime);

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.EmergencyStop, null, Scenarios.CleaningTime));

        // Assert
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
    }

    [Fact]
    public void A_critical_anomaly_keeps_the_milk_already_collected_and_leaves_finished_quarters_as_they_are() {
        // Arrange: left front finishes normally with 2 kg, the others are still milking.
        MilkingSession anySession = MilkingSession();
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2), flow: 0m));
        anySession.RecordFlow(Scenarios.Reading(UdderQuarter.RightFront, AttachedAt + TimeSpan.FromMinutes(1), flow: 3m));

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.VacuumLoss, null, AttachedAt + TimeSpan.FromMinutes(3)));

        // Assert
        Assert.Equal(QuarterState.Completed, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Aborted, anySession.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(2m + 1.5m, anySession.TotalYieldKg);
    }

    [Theory]
    [MemberData(nameof(CriticalKinds))]
    public void Nothing_more_is_done_to_a_session_in_the_safe_state(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(kind).Generate());

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.PrepareTeat(UdderQuarter.LeftFront, Scenarios.CleaningTime, Scenarios.CleaningTime));
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(UdderQuarter.LeftFront, true, TimeSpan.FromMinutes(5)));
        Assert.Throws<MilkingRefusedException>(() => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, TimeSpan.FromMinutes(5), 1m)));
    }

    [Fact]
    public void A_second_critical_anomaly_is_accepted_and_both_are_kept() {
        // Arrange
        MilkingSession anySession = MilkingSession();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.AnimalAgitation, null, AttachedAt));

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.EmergencyStop, null, AttachedAt + TimeSpan.FromSeconds(1)));

        // Assert
        Assert.Equal(SessionPhase.SafeState, anySession.Phase);
        Assert.Equal(2, anySession.Anomalies.Count);
    }

    [Fact]
    public void A_kick_off_aborts_that_quarter_only() {
        // Arrange
        MilkingSession anySession = MilkingSession();

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.RightRear, AttachedAt + TimeSpan.FromMinutes(1)));

        // Assert
        QuarterMilking kicked = anySession.QuarterOf(UdderQuarter.RightRear);
        Assert.Equal(QuarterState.Aborted, kicked.State);
        Assert.False(kicked.IsCupOn);
        Assert.True(anySession.IsIncomplete);
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Attached, anySession.QuarterOf(UdderQuarter.RightFront).State);
        Assert.NotEqual(SessionPhase.SafeState, anySession.Phase);
    }

    [Fact]
    public void A_kicked_off_quarter_yields_no_more_readings_and_cannot_be_attached_again() {
        // Arrange
        MilkingSession anySession = MilkingSession();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2)));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.RecordFlow(Scenarios.Reading(UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(3), flow: 1m)));
        Assert.Throws<MilkingRefusedException>(
            () => anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, AttachedAt + TimeSpan.FromMinutes(3)));
    }

    [Fact]
    public void A_kicked_off_quarter_keeps_the_milk_it_gave_before() {
        // Arrange
        MilkingSession anySession = MilkingSession();

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2)));

        // Assert
        Assert.Equal(1m, anySession.QuarterOf(UdderQuarter.LeftFront).YieldKg);
    }

    [Fact]
    public void The_session_completes_when_the_last_quarter_in_progress_is_kicked_off() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear]))
                                              .Generate();
        Scenarios.AttachAllCups(anySession);

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.RightRear, AttachedAt + TimeSpan.FromSeconds(5)));

        // Assert
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
    }

    [Fact]
    public void A_kick_off_on_a_quarter_whose_cup_is_not_on_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        Scenarios.PrepareAllTeats(anySession);

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, TimeSpan.FromSeconds(45))));

        Assert.Contains("no cup on", refusal.Message);
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact(Skip = "domain defect: ReportAnomaly appends the anomaly to Anomalies before checking it can be applied, so a CupKickOff refused with MilkingRefusedException stays in the log of a session that never had that event (deterministic, no seed involved)")]
    public void A_refused_kick_off_leaves_no_trace_in_the_anomaly_log() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        Scenarios.PrepareAllTeats(anySession);
        Assert.Throws<MilkingRefusedException>(
            () => anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.CupKickOff, UdderQuarter.LeftFront, TimeSpan.FromSeconds(45))));

        // Assert
        Assert.Empty(anySession.Anomalies);
    }

    [Theory]
    [MemberData(nameof(DivertingKinds))]
    public void A_diverting_anomaly_sends_that_quarters_milk_to_the_discard_line(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = MilkingSession();
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(kind).WithQuarter(anyQuarter).Generate());

        // Assert
        Assert.True(anySession.QuarterOf(anyQuarter).IsDiverted);
        Assert.All(anySession.Quarters.Where(q => q.Quarter != anyQuarter), q => Assert.False(q.IsDiverted));
        Assert.NotEqual(SessionPhase.SafeState, anySession.Phase);
    }

    [Theory]
    [MemberData(nameof(DivertingKinds))]
    public void A_diverting_anomaly_does_not_touch_the_cup_or_the_state(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = MilkingSession();

        // Act
        anySession.ReportAnomaly(new MilkingAnomaly(kind, UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(2)));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(1m, anySession.QuarterOf(UdderQuarter.LeftFront).YieldKg);
        Assert.Equal(0m, anySession.SaleableYieldKg);
        Assert.Equal(1m, anySession.TotalYieldKg);
    }

    [Fact]
    public void An_anomaly_naming_a_blind_quarter_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.RightRear]))
                                              .Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.BloodInMilk, UdderQuarter.RightRear, TimeSpan.FromSeconds(80))));
        Assert.Empty(anySession.Anomalies);
    }

    [Fact]
    public void A_missing_anomaly_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anySession.ReportAnomaly(null!));
    }

    [Fact]
    public void A_released_session_takes_no_more_anomalies() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.EmergencyStop, null, TimeSpan.FromSeconds(3)));
        anySession.Release(anySession.StartedAt + TimeSpan.FromSeconds(10));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.VacuumLoss, null, TimeSpan.FromSeconds(11))));
    }

    [Fact(Skip = "domain defect (to confirm with the domain experts): MODEL.md says that after a critical anomaly there is 'no further operation', yet ReportAnomaly in the safe state still applies a diverting anomaly (the quarter is flagged and the anomaly logged) where PrepareTeat, AttachCup and RecordFlow are refused (deterministic, no seed involved)")]
    public void No_diverting_anomaly_is_applied_once_the_session_is_in_the_safe_state() {
        // Arrange
        MilkingSession anySession = MilkingSession();
        anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.EmergencyStop, null, AttachedAt + TimeSpan.FromMinutes(2)));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.ReportAnomaly(new MilkingAnomaly(AnomalyKind.BloodInMilk, UdderQuarter.LeftFront, AttachedAt + TimeSpan.FromMinutes(3))));
    }

}
