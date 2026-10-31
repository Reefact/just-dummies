using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class MilkFlowTests
{
    [Fact]
    public void The_first_reading_starts_the_milking_phase()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        MilkFlowReading anyFlowingReading = Arbitrary.MilkFlowReading()
                                                     .WithQuarter(UdderQuarter.LeftFront)
                                                     .WithElapsed(attachedAt + TimeSpan.FromSeconds(30))
                                                     .WithFlowKgPerMinute(1.5m)
                                                     .Generate();

        // Act
        anySession.RecordFlow(anyFlowingReading);

        // Assert
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void The_yield_integrates_the_flow_between_readings()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.2m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyLeftFrontReading = Arbitrary.MilkFlowReading().WithQuarter(UdderQuarter.LeftFront);

        // Act: 0 -> 2 kg/min over the first minute, then 2 -> 3 kg/min over the next.
        anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(1)).WithFlowKgPerMinute(2m).Generate());
        anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).WithFlowKgPerMinute(3m).Generate());

        // Assert
        Assert.Equal(3.5m, anySession.QuarterOf(UdderQuarter.LeftFront).YieldKg);
        Assert.Equal(3.5m, anySession.TotalYieldKg);
    }

    [Fact]
    public void A_quarter_whose_flow_falls_under_the_threshold_is_completed()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.3m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyRightRearReading = Arbitrary.MilkFlowReading().WithQuarter(UdderQuarter.RightRear);
        anySession.RecordFlow(anyRightRearReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(3)).WithFlowKgPerMinute(2.5m).Generate());

        // Act
        anySession.RecordFlow(anyRightRearReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(6)).WithFlowKgPerMinute(0.2m).Generate());

        // Assert
        QuarterMilking rightRear = anySession.QuarterOf(UdderQuarter.RightRear);
        Assert.Equal(QuarterState.Completed, rightRear.State);
        Assert.False(rightRear.IsCupOn);
    }

    [Fact]
    public void A_low_flow_before_let_down_does_not_detach_the_cup()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.3m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        MilkFlowReading anyEarlyTrickle = Arbitrary.MilkFlowReading()
                                                   .WithQuarter(UdderQuarter.LeftRear)
                                                   .WithElapsed(attachedAt + TimeSpan.FromSeconds(20))
                                                   .WithFlowKgPerMinute(0.1m)
                                                   .Generate();

        // Act
        anySession.RecordFlow(anyEarlyTrickle);

        // Assert
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.LeftRear).State);
    }

    [Fact]
    public void Readings_of_a_quarter_come_in_time_order()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyLeftFrontReading = Arbitrary.MilkFlowReading().Healthy().WithQuarter(UdderQuarter.LeftFront)
                                                          .WithFlowKgPerMinute(f => f.GreaterThan(1m));
        anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).Generate());

        // Act
        Action earlier = () => anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(1)).Generate());

        // Assert
        Assert.Throws<MilkingRefusedException>(earlier);
    }

    [Fact]
    public void Quarters_milk_in_parallel_and_finish_independently()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear, UdderQuarter.RightRear]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.3m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyReading = Arbitrary.MilkFlowReading().Healthy();
        anySession.RecordFlow(anyReading.WithQuarter(UdderQuarter.LeftFront).WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).WithFlowKgPerMinute(2m).Generate());
        anySession.RecordFlow(anyReading.WithQuarter(UdderQuarter.RightFront).WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).WithFlowKgPerMinute(2m).Generate());

        // Act
        anySession.RecordFlow(anyReading.WithQuarter(UdderQuarter.LeftFront).WithElapsed(attachedAt + TimeSpan.FromMinutes(5)).WithFlowKgPerMinute(0.1m).Generate());

        // Assert
        Assert.Equal(QuarterState.Completed, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.Milking, anySession.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(SessionPhase.Milking, anySession.Phase);
    }

    [Fact]
    public void Once_every_quarter_is_finished_the_milking_is_completed()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear, UdderQuarter.RightRear, UdderQuarter.RightFront]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.3m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyLeftFrontReading = Arbitrary.MilkFlowReading().Healthy().WithQuarter(UdderQuarter.LeftFront);
        anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(2)).WithFlowKgPerMinute(2m).Generate());

        // Act
        anySession.RecordFlow(anyLeftFrontReading.WithElapsed(attachedAt + TimeSpan.FromMinutes(4)).WithFlowKgPerMinute(0m).Generate());

        // Assert
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        Assert.False(anySession.IsIncomplete);
    }

    [Fact]
    public void A_high_conductivity_reading_diverts_the_quarter_milk_away_from_the_tank()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear, UdderQuarter.RightRear]))
                                             .WithSettings(s => s.WithDetachFlowThreshold(0.3m))
                                             .Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        AnyMilkFlowReading anyReading = Arbitrary.MilkFlowReading().Healthy().WithElapsed(attachedAt + TimeSpan.FromMinutes(1)).WithFlowKgPerMinute(2m);
        anySession.RecordFlow(anyReading.WithQuarter(UdderQuarter.LeftFront).Generate());

        // Act
        anySession.RecordFlow(anyReading.WithQuarter(UdderQuarter.RightFront).WithConductivityMsPerCm(9.2m).Generate());

        // Assert
        Assert.True(anySession.QuarterOf(UdderQuarter.RightFront).IsDiverted);
        MilkingAnomaly anomaly = Assert.Single(anySession.Anomalies);
        Assert.Equal(AnomalyKind.HighConductivity, anomaly.Kind);
        Assert.Equal(UdderQuarter.RightFront, anomaly.Quarter);
        Assert.Equal(2m, anySession.TotalYieldKg);
        Assert.Equal(1m, anySession.SaleableYieldKg);
    }

    [Fact]
    public void Blood_in_the_milk_diverts_the_quarter_without_stopping_it()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        MilkFlowReading anyBloodyReading = Arbitrary.MilkFlowReading()
                                                    .Healthy()
                                                    .WithQuarter(UdderQuarter.LeftRear)
                                                    .WithElapsed(attachedAt + TimeSpan.FromMinutes(1))
                                                    .WithFlowKgPerMinute(2m)
                                                    .WithBloodDetected(true)
                                                    .Generate();

        // Act
        anySession.RecordFlow(anyBloodyReading);

        // Assert
        QuarterMilking leftRear = anySession.QuarterOf(UdderQuarter.LeftRear);
        Assert.True(leftRear.IsDiverted);
        Assert.Equal(QuarterState.Milking, leftRear.State);
        Assert.Equal(AnomalyKind.BloodInMilk, Assert.Single(anySession.Anomalies).Kind);
    }
}
