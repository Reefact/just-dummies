using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery.Tests;

[Reproducible]
public sealed class MilkingAndAnomalyTests {

    [Fact]
    public void A_first_reading_moves_the_session_to_milking() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = SessionSteps.AttachEveryCup(session);
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        MilkFlowReading anyRisingFlow = SessionSteps.HealthyReading()
                                                    .WithQuarter(anyQuarter)
                                                    .WithElapsed(attachedAt + TimeSpan.FromSeconds(30))
                                                    .WithFlowKgPerMinute(1.5m)
                                                    .Generate();

        // Act
        session.RecordFlow(anyRisingFlow);

        // Assert
        Assert.Equal(SessionPhase.Milking, session.Phase);
        Assert.Equal(QuarterState.Milking, session.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void The_yield_integrates_the_flow_by_the_trapezoidal_rule_and_the_cup_comes_off_when_the_flow_falls() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = SessionSteps.AttachEveryCup(session);
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        AnyMilkFlowReading reading = SessionSteps.HealthyReading().WithQuarter(anyQuarter);
        session.RecordFlow(reading.WithElapsed(attachedAt + TimeSpan.FromMinutes(1)).WithFlowKgPerMinute(2m).Generate());

        // Act
        session.RecordFlow(reading.WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).WithFlowKgPerMinute(0m).Generate());

        // Assert
        QuarterMilking quarter = session.QuarterOf(anyQuarter);
        Assert.Equal(2m, quarter.YieldKg);   // (0 + 2) / 2 * 1 min + (2 + 0) / 2 * 1 min
        Assert.Equal(QuarterState.Completed, quarter.State);
    }

    [Fact]
    public void A_reading_not_after_the_previous_one_is_refused() {
        // Arrange
        MilkingSession     session    = Any.MilkingSession().Generate();
        TimeSpan           attachedAt = SessionSteps.AttachEveryCup(session);
        UdderQuarter       anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        AnyMilkFlowReading reading    = SessionSteps.HealthyReading().WithQuarter(anyQuarter).WithElapsed(attachedAt + TimeSpan.FromMinutes(1));
        session.RecordFlow(reading.WithFlowKgPerMinute(2m).Generate());

        // Act
        Action act = () => session.RecordFlow(reading.WithFlowKgPerMinute(2m).Generate());

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void Milk_with_high_conductivity_is_diverted_from_the_tank() {
        // Arrange
        Animal         anyCowWithFourTeats = Any.Animal().WithBlindQuarters([]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session             = Any.MilkingSession().WithAnimal(anyCowWithFourTeats).Generate();
        TimeSpan       attachedAt          = SessionSteps.AttachEveryCup(session);
        session.RecordFlow(SessionSteps.HealthyReading().WithQuarter(UdderQuarter.LeftFront).WithElapsed(attachedAt + TimeSpan.FromMinutes(1)).WithFlowKgPerMinute(2m).Generate());
        MilkFlowReading anyMastitisSignal = Any.MilkFlowReading()
                                               .WithQuarter(UdderQuarter.RightRear)
                                               .WithElapsed(attachedAt + TimeSpan.FromMinutes(1))
                                               .WithFlowKgPerMinute(4m)
                                               .WithConductivityMsPerCm(9m)
                                               .WithBloodDetected(false)
                                               .Generate();

        // Act
        session.RecordFlow(anyMastitisSignal);

        // Assert
        Assert.True(session.QuarterOf(UdderQuarter.RightRear).IsDiverted);
        Assert.Equal(AnomalyKind.HighConductivity, Assert.Single(session.Anomalies).Kind);
        Assert.Equal(3m, session.TotalYieldKg);     // 1 kg from LeftFront, 2 kg from RightRear
        Assert.Equal(1m, session.SaleableYieldKg);  // RightRear went to the discard line
    }

    [Fact]
    public void Blood_in_a_quarter_diverts_its_milk() {
        // Arrange
        MilkingSession  session     = Any.MilkingSession().Generate();
        TimeSpan        attachedAt  = SessionSteps.AttachEveryCup(session);
        UdderQuarter    anyQuarter  = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        MilkFlowReading anyBloodied = SessionSteps.HealthyReading()
                                                  .WithQuarter(anyQuarter)
                                                  .WithElapsed(attachedAt + TimeSpan.FromSeconds(45))
                                                  .WithBloodDetected(true)
                                                  .Generate();

        // Act
        session.RecordFlow(anyBloodied);

        // Assert
        Assert.True(session.QuarterOf(anyQuarter).IsDiverted);
        Assert.Contains(session.Anomalies, a => a.Kind == AnomalyKind.BloodInMilk && a.Quarter == anyQuarter);
    }

    [Fact]
    public void A_critical_anomaly_puts_the_box_in_the_safe_state_at_once() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        SessionSteps.AttachEveryCup(session);
        MilkingAnomaly anyCritical = Any.MilkingAnomaly()
                                        .WithKind(k => k.OneOf(AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop))
                                        .Generate();

        // Act
        session.ReportAnomaly(anyCritical);

        // Assert
        Assert.Equal(SessionPhase.SafeState, session.Phase);
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
    }

    [Fact]
    public void No_milking_operation_is_allowed_in_the_safe_state() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());
        UdderQuarter anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        Action act = () => session.PrepareTeat(anyQuarter, session.Settings.CleaningTime, session.Settings.CleaningTime);

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_kicked_off_cup_aborts_only_its_quarter_and_leaves_the_milking_incomplete() {
        // Arrange
        Animal         anyCowWithFourTeats = Any.Animal().WithBlindQuarters([]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session             = Any.MilkingSession().WithAnimal(anyCowWithFourTeats).Generate();
        TimeSpan       attachedAt          = SessionSteps.AttachEveryCup(session);
        MilkingAnomaly anyKickOff          = Any.MilkingAnomaly()
                                                .WithKind(AnomalyKind.CupKickOff)
                                                .WithQuarter(UdderQuarter.LeftRear)
                                                .WithElapsed(e => e.GreaterThan(attachedAt))
                                                .Generate();

        // Act
        session.ReportAnomaly(anyKickOff);

        // Assert
        Assert.Equal(QuarterState.Aborted, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.NotEqual(SessionPhase.SafeState, session.Phase);
        Assert.True(session.IsIncomplete);
    }

    [Fact]
    public void A_cup_cannot_be_kicked_off_a_quarter_it_is_not_on() {
        // Arrange
        MilkingSession session        = Any.MilkingSession().Generate();
        SessionSteps.PrepareEveryTeat(session);
        UdderQuarter   anyQuarter     = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        MilkingAnomaly anyKickOff     = Any.MilkingAnomaly().WithKind(AnomalyKind.CupKickOff).WithQuarter(anyQuarter).Generate();

        // Act
        Action act = () => session.ReportAnomaly(anyKickOff);

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_cow_is_not_released_while_cups_may_still_be_on() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        SessionSteps.AttachEveryCup(session);
        DateTimeOffset anyLater = Any.DateTimeOffset().After(session.StartedAt).Generate();

        // Act
        Action act = () => session.Release(anyLater);

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_completed_session_releases_the_cow() {
        // Arrange
        Animal         anyCowWithOneTeat = Any.Animal().WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session           = Any.MilkingSession().WithAnimal(anyCowWithOneTeat).Generate();
        TimeSpan       attachedAt        = SessionSteps.AttachEveryCup(session);
        AnyMilkFlowReading reading       = SessionSteps.HealthyReading().WithQuarter(UdderQuarter.RightRear);
        session.RecordFlow(reading.WithElapsed(attachedAt + TimeSpan.FromMinutes(3)).WithFlowKgPerMinute(3m).Generate());
        session.RecordFlow(reading.WithElapsed(attachedAt + TimeSpan.FromMinutes(6)).WithFlowKgPerMinute(0m).Generate());
        DateTimeOffset anyExit = Any.DateTimeOffset().After(session.StartedAt).Generate();

        // Act
        session.Release(anyExit);

        // Assert
        Assert.Equal(SessionPhase.Released, session.Phase);
        Assert.False(session.IsIncomplete);
    }

    [Fact]
    public void A_release_before_the_session_started_is_an_invalid_argument() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        session.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Act
        Action act = () => session.Release(session.StartedAt - TimeSpan.FromSeconds(1));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

}
