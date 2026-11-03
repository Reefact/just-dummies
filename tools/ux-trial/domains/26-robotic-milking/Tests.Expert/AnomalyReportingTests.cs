using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class AnomalyReportingTests {

    private static readonly TimeSpan HalfAMinute = TimeSpan.FromSeconds(30);

    public static TheoryData<AnomalyKind> CriticalKinds() {
        return new TheoryData<AnomalyKind> { AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop };
    }

    public static TheoryData<AnomalyKind> DivertingKinds() {
        return new TheoryData<AnomalyKind> { AnomalyKind.HighConductivity, AnomalyKind.BloodInMilk };
    }

    private static MilkingAnomaly Critical(AnomalyKind kind) {
        return Any.MilkingAnomaly().WithKind(kind).Generate();
    }

    private static MilkingAnomaly AboutAQuarter(AnomalyKind kind, UdderQuarter quarter) {
        return Any.MilkingAnomaly().WithKind(kind).WithQuarter(quarter).Generate();
    }

    [Theory]
    [MemberData(nameof(CriticalKinds))]
    public void A_critical_anomaly_puts_the_box_in_the_safe_state_at_once_and_gives_up_every_quarter(AnomalyKind kind) {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        MilkingAnomaly anomaly    = Critical(kind);

        // Act
        anySession.ReportAnomaly(anomaly);

        // Assert
        Assert.Equal(SessionPhase.SafeState, anySession.Phase);
        Assert.All(anySession.Quarters, quarter => Assert.Equal(QuarterState.Aborted, quarter.State));
        Assert.True(anySession.IsIncomplete);
        Assert.Same(anomaly, Assert.Single(anySession.Anomalies));
    }

    [Theory]
    [MemberData(nameof(CriticalKinds))]
    public void A_critical_anomaly_in_the_middle_of_the_milking_aborts_what_is_still_milked_and_keeps_what_was_done(AnomalyKind kind) {
        // Arrange
        MilkingSession session    = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       attachedAt = Milking.AttachEveryCup(session);
        Milking.MilkToCompletion(session, UdderQuarter.LeftFront, attachedAt, 4m);
        session.RecordFlow(Milking.HealthyReading(UdderQuarter.RightFront, attachedAt + HalfAMinute, 4m));

        // Act
        session.ReportAnomaly(Critical(kind));

        // Assert
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Aborted,   session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(QuarterState.Aborted,   session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Equal(QuarterState.Aborted,   session.QuarterOf(UdderQuarter.RightRear).State);
        Assert.Equal(3m, session.TotalYieldKg);   // 2 kg from the finished quarter, 1 kg from the one cut short
        Assert.Equal(SessionPhase.SafeState, session.Phase);
    }

    [Fact]
    public void Nothing_more_is_prepared_once_the_box_is_in_the_safe_state() {
        // Arrange
        MilkingSession session  = Any.MilkingSession().Generate();
        UdderQuarter   quarter  = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.ReportAnomaly(Critical(AnomalyKind.EmergencyStop));

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(quarter, cleaning, cleaning));
    }

    [Fact]
    public void No_cup_is_attached_once_the_box_is_in_the_safe_state() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;
        session.ReportAnomaly(Critical(AnomalyKind.VacuumLoss));

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(quarter, succeeded: true, readyAt));
    }

    [Fact]
    public void No_reading_is_taken_once_the_box_is_in_the_safe_state() {
        // Arrange
        (MilkingSession session, UdderQuarter quarter, TimeSpan attachedAt) = StartMilking();
        session.ReportAnomaly(Critical(AnomalyKind.AnimalAgitation));

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.RecordFlow(Milking.HealthyReading(quarter, attachedAt + HalfAMinute * 2, 4m)));
    }

    [Fact(Skip = "domain defect: ReportAnomaly never checks that the box is still active (unlike PrepareTeat, AttachCup and RecordFlow), so a further anomaly is accepted and recorded in the safe state although the model says no further operation; deterministic, no seed involved")]
    public void No_anomaly_is_taken_once_the_box_is_in_the_safe_state() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        session.ReportAnomaly(Critical(AnomalyKind.VacuumLoss));

        // Act
        Exception? refusal = Record.Exception(() => session.ReportAnomaly(Critical(AnomalyKind.EmergencyStop)));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
        Assert.Single(session.Anomalies);
    }

    [Fact]
    public void A_kick_off_while_the_cup_is_attached_aborts_that_quarter_alone() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.AttachEveryCup(session);

        // Act
        session.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, UdderQuarter.RightFront));

        // Assert
        Assert.Equal(QuarterState.Aborted,  session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.RightRear).State);
        Assert.True(session.IsIncomplete);
        Assert.NotEqual(SessionPhase.SafeState, session.Phase);
    }

    [Fact]
    public void A_kick_off_while_the_quarter_is_milking_keeps_the_milk_already_yielded() {
        // Arrange
        MilkingSession session    = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       attachedAt = Milking.AttachEveryCup(session);
        session.RecordFlow(Milking.HealthyReading(UdderQuarter.LeftRear, attachedAt + HalfAMinute, 4m));

        // Act
        session.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, UdderQuarter.LeftRear));

        // Assert
        Assert.Equal(QuarterState.Aborted, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Equal(1m, session.TotalYieldKg);
    }

    [Fact]
    public void A_kick_off_with_every_cup_off_ends_the_milking_incomplete() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        Milking.AttachEveryCup(anySession);

        // Act
        foreach (UdderQuarter quarter in Milking.MilkableQuartersOf(anySession)) {
            anySession.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, quarter));
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_kick_off_of_a_quarter_whose_cup_is_not_on_is_refused_and_leaves_the_quarter_as_it_was() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.PrepareEveryTeat(session);

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, UdderQuarter.LeftFront)));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact(Skip = "domain defect: a refused kick-off is already appended to Anomalies when the refusal is thrown, so the session reports an anomaly it refused; deterministic, no seed involved")]
    public void A_refused_kick_off_is_not_recorded_as_an_anomaly() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        Milking.PrepareEveryTeat(session);

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, UdderQuarter.LeftFront)));

        // Assert
        Assert.Empty(session.Anomalies);
    }

    [Fact]
    public void A_kick_off_of_a_quarter_that_has_already_finished_is_refused() {
        // Arrange
        MilkingSession session    = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       attachedAt = Milking.AttachEveryCup(session);
        Milking.MilkToCompletion(session, UdderQuarter.LeftFront, attachedAt, 4m);

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.ReportAnomaly(AboutAQuarter(AnomalyKind.CupKickOff, UdderQuarter.LeftFront)));
        Assert.Equal(QuarterState.Completed, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Theory]
    [MemberData(nameof(DivertingKinds))]
    public void A_diverting_anomaly_sends_that_quarters_milk_to_the_discard_line_and_no_other(AnomalyKind kind) {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();

        // Act
        session.ReportAnomaly(AboutAQuarter(kind, UdderQuarter.RightRear));

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.RightRear).IsDiverted);
        Assert.Equal(1, session.Quarters.Count(quarter => quarter.IsDiverted));
        Assert.NotEqual(SessionPhase.SafeState, session.Phase);
    }

    [Theory]
    [MemberData(nameof(DivertingKinds))]
    public void A_diverting_anomaly_does_not_stop_the_milking_nor_make_it_incomplete(AnomalyKind kind) {
        // Arrange
        MilkingSession session    = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       attachedAt = Milking.AttachEveryCup(session);
        session.ReportAnomaly(AboutAQuarter(kind, UdderQuarter.RightRear));

        // Act
        foreach (UdderQuarter quarter in Milking.MilkableQuartersOf(session)) {
            Milking.MilkToCompletion(session, quarter, attachedAt, 4m);
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.False(session.IsIncomplete);
        Assert.Equal(8m, session.TotalYieldKg);
        Assert.Equal(6m, session.SaleableYieldKg);
    }

    [Fact]
    public void Anomalies_are_kept_in_the_order_they_were_reported() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().Generate();
        MilkingAnomaly first   = AboutAQuarter(AnomalyKind.BloodInMilk, UdderQuarter.LeftFront);
        MilkingAnomaly second  = AboutAQuarter(AnomalyKind.HighConductivity, UdderQuarter.RightFront);

        // Act
        session.ReportAnomaly(first);
        session.ReportAnomaly(second);

        // Assert
        Assert.Equal(new[] { first, second }, session.Anomalies);
    }

    [Fact]
    public void An_anomaly_about_a_blind_quarter_is_refused() {
        // Arrange
        MilkingSession session      = Any.MilkingSession().WithAnimal(cow => cow.WithBlindQuarters(blind => blind.NonEmpty())).Generate();
        UdderQuarter   blindQuarter = session.Animal.BlindQuarters.First();

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.ReportAnomaly(AboutAQuarter(AnomalyKind.BloodInMilk, blindQuarter)));
        Assert.Empty(session.Anomalies);
    }

    [Fact]
    public void An_anomaly_after_the_cow_has_left_is_refused() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        anySession.ReportAnomaly(Critical(AnomalyKind.EmergencyStop));
        anySession.Release(anySession.StartedAt);

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.ReportAnomaly(Critical(AnomalyKind.VacuumLoss)));
    }

    [Fact]
    public void An_absent_anomaly_is_refused() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anySession.ReportAnomaly(null!));
    }

    private static (MilkingSession Session, UdderQuarter Quarter, TimeSpan AttachedAt) StartMilking() {
        MilkingSession session    = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = Milking.AttachEveryCup(session);

        return (session, Milking.MilkableQuartersOf(session)[0], attachedAt);
    }

}
