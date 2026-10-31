using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class AnomalyTests
{
    [Theory]
    [InlineData(AnomalyKind.AnimalAgitation)]
    [InlineData(AnomalyKind.VacuumLoss)]
    [InlineData(AnomalyKind.EmergencyStop)]
    public void A_critical_anomaly_aborts_every_quarter_and_puts_the_box_in_the_safe_state(AnomalyKind criticalKind)
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        MilkingAnomaly anyCriticalAnomaly = Arbitrary.MilkingAnomaly().WithKind(criticalKind).WithElapsed(attachedAt).Generate();

        // Act
        anySession.ReportAnomaly(anyCriticalAnomaly);

        // Assert
        Assert.Equal(SessionPhase.SafeState, anySession.Phase);
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Aborted, q.State));
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_critical_anomaly_during_preparation_stops_the_session_at_once()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                             .Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));
        MilkingAnomaly anyEmergencyStop = Arbitrary.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate();
        anySession.ReportAnomaly(anyEmergencyStop);

        // Act
        Action brushNext = () => anySession.PrepareTeat(UdderQuarter.RightFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(20));

        // Assert
        Assert.Throws<MilkingRefusedException>(brushNext);
    }

    [Fact]
    public void A_kicked_off_cup_aborts_that_quarter_only()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        TimeSpan attachedAt = SessionSteps.AttachEveryCup(anySession);
        MilkingAnomaly anyKickOff = Arbitrary.MilkingAnomaly()
                                             .WithKind(AnomalyKind.CupKickOff)
                                             .WithQuarter(UdderQuarter.RightFront)
                                             .WithElapsed(attachedAt + TimeSpan.FromMinutes(1))
                                             .Generate();

        // Act
        anySession.ReportAnomaly(anyKickOff);

        // Assert
        Assert.Equal(QuarterState.Aborted, anySession.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(QuarterState.Attached, anySession.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.NotEqual(SessionPhase.SafeState, anySession.Phase);
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_cup_that_is_not_on_cannot_be_kicked_off()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        SessionSteps.PrepareEveryTeat(anySession);
        MilkingAnomaly anyKickOff = Arbitrary.MilkingAnomaly().WithKind(AnomalyKind.CupKickOff).WithQuarter(UdderQuarter.LeftRear).Generate();

        // Act
        Action report = () => anySession.ReportAnomaly(anyKickOff);

        // Assert
        Assert.Throws<MilkingRefusedException>(report);
    }

    [Theory]
    [InlineData(AnomalyKind.CupKickOff)]
    [InlineData(AnomalyKind.HighConductivity)]
    [InlineData(AnomalyKind.BloodInMilk)]
    public void A_quarter_level_anomaly_names_its_quarter(AnomalyKind quarterLevelKind)
    {
        // Arrange
        AnyMilkingAnomaly anyAnomalyWithoutQuarter = Arbitrary.MilkingAnomaly().WithKind(quarterLevelKind).WithQuarter((UdderQuarter?)null);

        // Act
        Action create = () => anyAnomalyWithoutQuarter.Generate();

        // Assert
        Assert.Throws<ArgumentException>("quarter", create);
    }
}
