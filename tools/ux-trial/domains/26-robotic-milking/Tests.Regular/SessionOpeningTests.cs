using JustDummies;
using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class SessionOpeningTests
{
    [Fact]
    public void A_new_session_is_identified_and_follows_each_milkable_quarter_only()
    {
        // Arrange
        Animal anyCowBlindRightRear = Arbitrary.Animal().WithBlindQuarters([UdderQuarter.RightRear]).Generate();

        // Act
        MilkingSession session = Arbitrary.MilkingSession().WithAnimal(anyCowBlindRightRear).Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
        Assert.Equal(
            [UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear],
            session.Quarters.Select(q => q.Quarter).Order());
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Pending, q.State));
    }

    [Fact]
    public void A_cow_milked_less_than_the_minimum_interval_ago_is_sent_out_without_milking()
    {
        // Arrange
        DateTimeOffset startedAt = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
        Animal anyCowMilkedFiveHoursAgo = Arbitrary.Animal().WithLastMilkedAt(startedAt.AddHours(-5)).Generate();
        MilkingSettings anySixHourInterval = Arbitrary.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(6)).Generate();

        // Act
        Action open = () => _ = Arbitrary.MilkingSession()
                                         .WithAnimal(anyCowMilkedFiveHoursAgo)
                                         .WithStartedAt(startedAt)
                                         .WithSettings(anySixHourInterval)
                                         .Generate();

        // Assert
        Assert.Throws<MilkingRefusedException>(open);
    }

    [Fact]
    public void A_cow_milked_longer_ago_than_the_minimum_interval_is_accepted()
    {
        // Arrange
        DateTimeOffset startedAt = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
        Animal anyCowMilkedSevenHoursAgo = Arbitrary.Animal().WithLastMilkedAt(startedAt.AddHours(-7)).Generate();
        MilkingSettings anySixHourInterval = Arbitrary.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(6)).Generate();

        // Act
        MilkingSession session = Arbitrary.MilkingSession()
                                          .WithAnimal(anyCowMilkedSevenHoursAgo)
                                          .WithStartedAt(startedAt)
                                          .WithSettings(anySixHourInterval)
                                          .Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_is_accepted()
    {
        // Arrange
        Animal anyNewCow = Arbitrary.Animal().WithLastMilkedAt((DateTimeOffset?)null).Generate();

        // Act
        MilkingSession session = Arbitrary.MilkingSession().WithAnimal(anyNewCow).Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_last_milking_after_the_session_start_is_an_invalid_argument()
    {
        // Arrange
        DateTimeOffset startedAt = new(2026, 10, 30, 14, 0, 0, TimeSpan.Zero);
        Animal anyCowMilkedLater = Arbitrary.Animal().WithLastMilkedAt(startedAt.AddMinutes(10)).Generate();

        // Act
        Action open = () => _ = Arbitrary.MilkingSession().WithAnimal(anyCowMilkedLater).WithStartedAt(startedAt).Generate();

        // Assert
        Assert.Throws<ArgumentException>("startedAt", open);
    }
}
