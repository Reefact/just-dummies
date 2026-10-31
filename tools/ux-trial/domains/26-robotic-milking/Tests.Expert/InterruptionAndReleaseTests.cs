namespace RoboticMilking.Tests.Expert;

public sealed class InterruptionAndReleaseTests {

    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation)]
    [InlineData(AnomalyKind.VacuumLoss)]
    [InlineData(AnomalyKind.EmergencyStop)]
    public void A_critical_anomaly_takes_every_cup_off_and_puts_the_box_in_the_safe_state(AnomalyKind critical) {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        MilkingAnomaly anyAlarm   = Any.MilkingAnomaly()
                                       .WithKind(critical)
                                       .WithElapsed(e => e.GreaterThan(attachedAt))
                                       .Generate();

        // Act
        anySession.ReportAnomaly(anyAlarm);

        // Assert
        Assert.Equal(SessionPhase.SafeState, anySession.Phase);
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
    }

    [Fact]
    public void A_quarter_already_milked_out_stays_completed_through_the_safe_state() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();
        anySession.MilkOut(anyQuarter, attachedAt, 2m);

        // Act
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());

        // Assert
        Assert.Equal(QuarterState.Completed, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void Once_in_the_safe_state_the_robot_refuses_any_milking_operation() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(k => k.OneOf(AnomalyKind.AnimalAgitation, AnomalyKind.VacuumLoss, AnomalyKind.EmergencyStop)).Generate());
        MilkFlowReading anyLateReading = Recipes.HealthyReading()
                                                .WithQuarter(anySession.AnyMilkableQuarter())
                                                .WithElapsed(e => e.GreaterThan(attachedAt))
                                                .Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.RecordFlow(anyLateReading));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void A_cup_kick_off_aborts_only_that_quarter() {
        // Arrange: two quarters at least, so one keeps milking.
        MilkingSession     anySession = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2))).Generate();
        TimeSpan           attachedAt = anySession.AttachEveryCup();
        List<UdderQuarter> anyTwo     = Any.ListOf(Any.ElementOf(anySession.Animal.MilkableQuarters)).WithCount(2).Distinct().Generate();
        UdderQuarter       kicked     = anyTwo[0];
        UdderQuarter       untouched  = anyTwo[1];
        MilkingAnomaly     anyKickOff = Any.MilkingAnomaly()
                                           .WithKind(AnomalyKind.CupKickOff)
                                           .WithQuarter(kicked)
                                           .WithElapsed(e => e.GreaterThan(attachedAt))
                                           .Generate();

        // Act
        anySession.ReportAnomaly(anyKickOff);

        // Assert
        Assert.Equal(QuarterState.Aborted, anySession.QuarterOf(kicked).State);
        Assert.Equal(QuarterState.Attached, anySession.QuarterOf(untouched).State);
        Assert.True(anySession.IsIncomplete);
        Assert.NotEqual(SessionPhase.SafeState, anySession.Phase);
    }

    [Fact]
    public void A_cup_that_is_not_on_cannot_be_kicked_off() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();
        anySession.PrepareEveryTeat();
        MilkingAnomaly anyKickOff = Any.MilkingAnomaly().WithKind(AnomalyKind.CupKickOff).WithQuarter(anyQuarter).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.ReportAnomaly(anyKickOff));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void A_cow_is_not_released_while_cups_may_still_be_on() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        anySession.AttachEveryCup();
        DateTimeOffset anyExit = anySession.StartedAt + Any.TimeSpan().Between(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.Release(anyExit));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
        Assert.Null(anySession.ReleasedAt);
    }

    [Fact]
    public void A_fully_milked_cow_is_released_through_the_exit_gate() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        TimeSpan       milkedOut  = attachedAt;
        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            milkedOut = anySession.MilkOut(quarter, attachedAt, 2m);
        }

        DateTimeOffset exit = anySession.StartedAt + milkedOut;

        // Act
        anySession.Release(exit);

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
        Assert.Equal(exit, anySession.ReleasedAt);
    }

    [Fact]
    public void A_cow_in_a_safe_box_is_released_and_the_session_closes() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        anySession.AttachEveryCup();
        anySession.ReportAnomaly(Any.MilkingAnomaly().WithKind(AnomalyKind.AnimalAgitation).Generate());
        anySession.Release(anySession.StartedAt + TimeSpan.FromMinutes(3));
        MilkingAnomaly anyLateAnomaly = Any.MilkingAnomaly().Generate();

        // Act
        Exception? refusal = Record.Exception(() => anySession.ReportAnomaly(anyLateAnomaly));

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
        Assert.IsType<MilkingRefusedException>(refusal);
    }

}
