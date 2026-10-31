namespace RoboticMilking.Tests.Expert;

public sealed class MilkingTests {

    [Fact]
    public void A_quarter_yield_integrates_the_flow_between_samples() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();

        // Act: 0 -> 2 -> 3 -> 1 kg/min, one minute apart.
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromMinutes(1), 2m));
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromMinutes(2), 3m));
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromMinutes(3), 1m));

        // Assert: (0+2)/2 + (2+3)/2 + (3+1)/2 = 5.5 kg.
        Assert.Equal(5.5m, anySession.QuarterOf(anyQuarter).YieldKg);
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

    [Fact]
    public void A_cup_comes_off_once_the_flow_falls_back_under_the_detach_threshold() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().WithSettings(s => s.WithDetachFlowThreshold(0.3m)).Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromMinutes(1), 1.5m));

        // Act
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromMinutes(2), 0.29m));

        // Assert
        Assert.Equal(QuarterState.Completed, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void A_low_flow_before_the_milk_comes_in_does_not_detach_the_cup() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().WithSettings(s => s.WithDetachFlowThreshold(0.3m)).Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();

        // Act
        anySession.RecordFlow(Recipes.HealthyReading(anyQuarter, attachedAt + TimeSpan.FromSeconds(20), 0.1m));

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void Readings_of_one_quarter_must_move_forward_in_time() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();
        TimeSpan       sampledAt  = attachedAt + TimeSpan.FromMinutes(1);
        anySession.RecordFlow(Recipes.HealthyReading().WithQuarter(anyQuarter).WithElapsed(sampledAt).Generate());

        // Act
        Exception? refusal = Record.Exception(
            () => anySession.RecordFlow(Recipes.HealthyReading().WithQuarter(anyQuarter).WithElapsed(sampledAt).Generate()));

        // Assert
        Assert.IsType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void Quarters_finish_independently_and_the_session_completes_with_the_last_one() {
        // Arrange: a cow with two milkable quarters at least.
        MilkingSession     anySession  = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2))).Generate();
        TimeSpan           attachedAt  = anySession.AttachEveryCup();
        List<UdderQuarter> anyOrder    = Any.ListOf(Any.ElementOf(anySession.Animal.MilkableQuarters))
                                            .WithCount(anySession.Animal.MilkableQuarters.Count)
                                            .Distinct()
                                            .Generate();
        decimal            anyPeak     = Any.Decimal().Between(1m, MilkFlowReading.MaxFlowKgPerMinute).WithScale(2).Generate();
        UdderQuarter       lastQuarter = anyOrder[^1];
        foreach (UdderQuarter quarter in anyOrder.SkipLast(1)) {
            anySession.MilkOut(quarter, attachedAt, anyPeak);
        }

        SessionPhase phaseBeforeTheLast = anySession.Phase;

        // Act
        anySession.MilkOut(lastQuarter, attachedAt, anyPeak);

        // Assert
        Assert.NotEqual(SessionPhase.Completed, phaseBeforeTheLast);
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        Assert.False(anySession.IsIncomplete);
    }

    [Fact]
    public void Milk_from_a_quarter_suggesting_mastitis_is_diverted_from_the_tank() {
        // Arrange: two quarters at least, so one goes to the tank and one is diverted.
        MilkingSession     anySession  = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2))).Generate();
        TimeSpan           attachedAt  = anySession.AttachEveryCup();
        List<UdderQuarter> anyTwo      = Any.ListOf(Any.ElementOf(anySession.Animal.MilkableQuarters)).WithCount(2).Distinct().Generate();
        UdderQuarter       infected    = anyTwo[0];
        UdderQuarter       healthy     = anyTwo[1];
        TimeSpan           oneMinute   = attachedAt + TimeSpan.FromMinutes(1);
        anySession.RecordFlow(Recipes.HealthyReading(healthy, oneMinute, 2m));

        // Act
        anySession.RecordFlow(Recipes.HealthyReading()
                                     .WithQuarter(infected)
                                     .WithElapsed(oneMinute)
                                     .WithFlowKgPerMinute(4m)
                                     .WithConductivityMsPerCm(c => c.GreaterThan(MilkFlowReading.MastitisConductivityThreshold))
                                     .Generate());

        // Assert: 1 kg from each quarter; only the healthy one is saleable.
        Assert.True(anySession.QuarterOf(infected).IsDiverted);
        Assert.Equal(3m, anySession.TotalYieldKg);
        Assert.Equal(1m, anySession.SaleableYieldKg);
        MilkingAnomaly anomaly = Assert.Single(anySession.Anomalies);
        Assert.Equal((AnomalyKind.HighConductivity, infected), (anomaly.Kind, anomaly.Quarter!.Value));
    }

    [Fact]
    public void Blood_in_a_quarter_diverts_its_milk_without_stopping_the_session() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();
        TimeSpan       attachedAt = anySession.AttachEveryCup();
        UdderQuarter   anyQuarter = anySession.AnyMilkableQuarter();

        // Act
        anySession.RecordFlow(Recipes.HealthyReading()
                                     .WithQuarter(anyQuarter)
                                     .WithElapsed(attachedAt + TimeSpan.FromMinutes(1))
                                     .WithBloodDetected(true)
                                     .Generate());

        // Assert
        Assert.True(anySession.QuarterOf(anyQuarter).IsDiverted);
        Assert.Contains(anySession.Anomalies, a => a.Kind == AnomalyKind.BloodInMilk);
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

}
