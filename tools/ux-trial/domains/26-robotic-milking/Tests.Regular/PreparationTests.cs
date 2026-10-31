using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class PreparationTests
{
    // Held as a refinement, not drawn here: a static initializer runs outside the [Reproducible] scope.
    private static readonly Func<AnyMilkingSettings, AnyMilkingSettings> TenSecondCleaning =
        s => s.WithCleaningTime(TimeSpan.FromSeconds(10));

    [Fact]
    public void Brushing_the_first_teat_starts_the_preparing_phase()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                             .Generate();

        // Act
        anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(SessionPhase.Preparing, anySession.Phase);
        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_brushing_shorter_than_the_cleaning_time_is_refused()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                             .Generate();

        // Act
        Action brush = () => anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(9), endedAt: TimeSpan.FromSeconds(9));

        // Assert
        Assert.Throws<MilkingRefusedException>(brush);
        Assert.Equal(QuarterState.Pending, anySession.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void Teats_are_brushed_one_at_a_time()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                             .Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));

        // Act: the second brushing would start at 5 s, while the first one ran until 10 s.
        Action overlapping = () => anySession.PrepareTeat(UdderQuarter.RightFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(15));

        // Assert
        Assert.Throws<MilkingRefusedException>(overlapping);
    }

    [Fact]
    public void A_teat_is_prepared_only_once()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(TenSecondCleaning)
                                             .Generate();
        anySession.PrepareTeat(UdderQuarter.LeftRear, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));

        // Act
        Action again = () => anySession.PrepareTeat(UdderQuarter.LeftRear, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(20));

        // Assert
        Assert.Throws<MilkingRefusedException>(again);
    }

    [Fact]
    public void A_blind_teat_is_never_prepared()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear]))
                                             .WithSettings(TenSecondCleaning)
                                             .Generate();

        // Act
        Action brushBlind = () => anySession.PrepareTeat(UdderQuarter.LeftRear, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));

        // Assert
        Assert.Throws<MilkingRefusedException>(brushBlind);
    }

    [Fact]
    public void Once_every_milkable_teat_is_prepared_the_robot_moves_on_to_attaching()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().Generate();

        // Act
        SessionSteps.PrepareEveryTeat(anySession);

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
    }
}
