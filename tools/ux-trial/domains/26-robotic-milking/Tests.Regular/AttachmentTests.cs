using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class AttachmentTests
{
    [Fact]
    public void No_cup_is_attached_before_every_milkable_teat_is_prepared()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10)))
                                             .Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(10), endedAt: TimeSpan.FromSeconds(10));

        // Act
        Action attach = () => anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Throws<MilkingRefusedException>(attach);
    }

    [Fact]
    public void No_cup_is_attached_before_milk_let_down()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithSettings(s => s.WithLetDownDelay(TimeSpan.FromSeconds(60)))
                                             .Generate();
        TimeSpan preparationEnd = SessionSteps.PrepareEveryTeat(anySession);
        UdderQuarter anyMilkableQuarter = anySession.Animal.MilkableQuarters[0];

        // Act
        Action attachTooEarly = () => anySession.AttachCup(anyMilkableQuarter, succeeded: true, preparationEnd + TimeSpan.FromSeconds(59));

        // Assert
        Assert.Throws<MilkingRefusedException>(attachTooEarly);
    }

    [Fact]
    public void A_failed_attempt_leaves_the_quarter_ready_for_another_one()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithMaxAttachAttempts(3))
                                             .Generate();
        TimeSpan anyAttachTime = SessionSteps.PrepareEveryTeat(anySession) + anySession.Settings.LetDownDelay;

        // Act
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, anyAttachTime);

        // Assert
        QuarterMilking rightRear = anySession.QuarterOf(UdderQuarter.RightRear);
        Assert.Equal(QuarterState.Prepared, rightRear.State);
        Assert.Equal(1, rightRear.AttachAttempts);
    }

    [Fact]
    public void The_last_allowed_failed_attempt_gives_the_quarter_up_and_leaves_the_milking_incomplete()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession()
                                             .WithAnimal(a => a.WithBlindQuarters([]))
                                             .WithSettings(s => s.WithMaxAttachAttempts(2))
                                             .Generate();
        TimeSpan anyAttachTime = SessionSteps.PrepareEveryTeat(anySession) + anySession.Settings.LetDownDelay;
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, anyAttachTime);

        // Act
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, anyAttachTime + TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, anySession.QuarterOf(UdderQuarter.RightRear).State);
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_successful_attempt_puts_the_cup_on()
    {
        // Arrange
        MilkingSession anySession = Arbitrary.MilkingSession().WithAnimal(a => a.WithBlindQuarters([])).Generate();
        TimeSpan anyAttachTime = SessionSteps.PrepareEveryTeat(anySession) + anySession.Settings.LetDownDelay;

        // Act
        anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, anyAttachTime);

        // Assert
        QuarterMilking leftFront = anySession.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(QuarterState.Attached, leftFront.State);
        Assert.True(leftFront.IsCupOn);
    }
}
