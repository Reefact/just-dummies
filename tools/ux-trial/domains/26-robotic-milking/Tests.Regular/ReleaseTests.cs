using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class ReleaseTests
{
    [Fact]
    public void A_cow_is_not_released_while_cups_are_on()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();
        SessionSteps.AttachEveryCup(anySession);

        // Act
        Action release = () => anySession.Release(anySession.StartedAt.AddMinutes(8));

        // Assert
        Assert.Throws<MilkingRefusedException>(release);
    }

    [Fact]
    public void A_cow_whose_milking_is_completed_is_released()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear]))
                                             .WithSettings(s => s.WithMaxAttachAttempts(1))
                                             .Generate();
        TimeSpan anyAttachTime = SessionSteps.PrepareEveryTeat(anySession) + anySession.Settings.LetDownDelay;
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, anyAttachTime);
        DateTimeOffset releasedAt = anySession.StartedAt.AddMinutes(4);

        // Act
        anySession.Release(releasedAt);

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
        Assert.Equal(releasedAt, anySession.ReleasedAt);
    }

    [Fact]
    public void A_cow_is_released_from_the_safe_state()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();
        SessionSteps.AttachEveryCup(anySession);
        anySession.ReportAnomaly(Arbitrary.MilkingAnomaly().WithKind(AnomalyKind.VacuumLoss).Generate());

        // Act
        anySession.Release(anySession.StartedAt.AddMinutes(3));

        // Assert
        Assert.Equal(SessionPhase.Released, anySession.Phase);
    }

    [Fact]
    public void A_release_before_the_session_start_is_an_invalid_argument()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();
        anySession.ReportAnomaly(Arbitrary.MilkingAnomaly().WithKind(AnomalyKind.AnimalAgitation).Generate());

        // Act
        Action release = () => anySession.Release(anySession.StartedAt.AddSeconds(-1));

        // Assert
        Assert.Throws<ArgumentException>("at", release);
    }

    [Fact]
    public void A_released_session_takes_no_more_anomalies()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();
        anySession.ReportAnomaly(Arbitrary.MilkingAnomaly().WithKind(AnomalyKind.EmergencyStop).Generate());
        anySession.Release(anySession.StartedAt.AddMinutes(1));
        MilkingAnomaly anyAnomaly = Arbitrary.MilkingAnomaly().Generate();

        // Act
        Action report = () => anySession.ReportAnomaly(anyAnomaly);

        // Assert
        Assert.Throws<MilkingRefusedException>(report);
    }
}
