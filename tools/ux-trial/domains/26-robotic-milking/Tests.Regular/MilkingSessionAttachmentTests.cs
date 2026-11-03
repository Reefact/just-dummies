using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using static RoboticMilking.Tests.Regular.MilkingSessionSupport;

namespace RoboticMilking.Tests.Regular;

[Reproducible]
public sealed class MilkingSessionAttachmentTests {

    [Fact]
    public void A_cup_is_attached_once_every_teat_is_prepared_and_the_let_down_delay_elapsed() {
        // Arrange
        MilkingSession session    = FourQuarterSession().Generate();
        TimeSpan       preparedAt = PrepareAll(session);
        TimeSpan       attachAt   = preparedAt + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(UdderQuarter.LeftFront, true, attachAt);

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(QuarterState.Attached, quarter.State);
        Assert.Equal(attachAt, quarter.AttachedAt);
        Assert.Equal(1, quarter.AttachAttempts);
        Assert.True(quarter.IsCupOn);
        Assert.Equal(SessionPhase.Attaching, session.Phase);
    }

    [Fact]
    public void No_cup_is_attached_while_a_teat_is_still_unprepared() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning * 2);
        session.PrepareTeat(UdderQuarter.LeftRear, cleaning, cleaning * 3);

        // Act / Assert: the fourth teat is not prepared yet.
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.AttachCup(UdderQuarter.LeftFront, true, TimeSpan.FromHours(1)));

        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Contains("prepared", refusal.Message);
    }

    [Fact]
    public void A_cup_cannot_be_attached_before_the_let_down_delay_has_elapsed() {
        // Arrange: a 40 s delay, so a one-tick-early attempt is never at the end of the brushing either.
        MilkingSession session = FourQuarterSession().WithSettings(s => s.WithLetDownDelay(TimeSpan.FromSeconds(40))).Generate();
        TimeSpan preparedAt = PrepareAll(session);

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.AttachCup(UdderQuarter.LeftFront, true, preparedAt + TimeSpan.FromSeconds(40) - TimeSpan.FromTicks(1)));

        Assert.Contains("let-down", refusal.Message);
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void Without_a_delay_a_cup_can_be_attached_the_instant_preparation_ends() {
        // Arrange: no delay, which the settings allow once four teats reach thirty seconds by themselves.
        MilkingSession session = FourQuarterSession()
                                .WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(8)).WithLetDownDelay(TimeSpan.Zero))
                                .Generate();
        TimeSpan preparedAt = PrepareAll(session);

        // Act
        session.AttachCup(UdderQuarter.RightRear, true, preparedAt);

        // Assert
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.RightRear).State);
    }

    [Fact]
    public void A_failed_attempt_is_counted_and_the_quarter_stays_ready_for_another() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(3)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt + TimeSpan.FromSeconds(5));

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(2, quarter.AttachAttempts);
        Assert.Equal(QuarterState.Prepared, quarter.State);
        Assert.Null(quarter.AttachedAt);
        Assert.False(session.IsIncomplete);
    }

    [Fact]
    public void A_retry_that_succeeds_attaches_the_cup() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(2)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        TimeSpan       retryAt  = attachAt + TimeSpan.FromSeconds(8);
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);

        // Act
        session.AttachCup(UdderQuarter.LeftFront, true, retryAt);

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(QuarterState.Attached, quarter.State);
        Assert.Equal(2, quarter.AttachAttempts);
        Assert.Equal(retryAt, quarter.AttachedAt);
    }

    [Fact]
    public void The_last_allowed_failure_gives_the_quarter_up() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(3)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);

        // Act
        session.AttachCup(UdderQuarter.LeftFront, false, attachAt);

        // Assert
        QuarterMilking quarter = session.QuarterOf(UdderQuarter.LeftFront);
        Assert.Equal(QuarterState.AttachmentFailed, quarter.State);
        Assert.Equal(3, quarter.AttachAttempts);
        Assert.True(quarter.IsFinished);
        Assert.False(quarter.IsCupOn);
        Assert.True(session.IsIncomplete);
    }

    [Fact]
    public void With_a_single_allowed_attempt_one_failure_is_enough_to_give_up() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(UdderQuarter.RightFront, false, attachAt);

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_quarter_given_up_cannot_be_attached_again() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;
        session.AttachCup(UdderQuarter.RightFront, false, attachAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.RightFront, true, attachAt));
    }

    [Fact]
    public void A_cup_already_on_cannot_be_attached_a_second_time() {
        // Arrange
        MilkingSession session  = FourQuarterSession().Generate();
        TimeSpan       attachAt = AttachAll(session);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftFront, true, attachAt));
    }

    [Fact]
    public void A_blind_quarter_is_never_attached() {
        // Arrange
        MilkingSession session  = SessionWithBlind(UdderQuarter.LeftRear).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftRear, true, attachAt));
    }

    [Fact]
    public void Quarters_are_attached_independently_of_each_other() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(UdderQuarter.LeftFront, true, attachAt);
        session.AttachCup(UdderQuarter.RightFront, false, attachAt);

        // Assert
        Assert.Equal(QuarterState.Attached, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(QuarterState.AttachmentFailed, session.QuarterOf(UdderQuarter.RightFront).State);
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftRear).State);
        Assert.True(session.IsIncomplete);
    }

    [Fact]
    public void When_every_quarter_is_given_up_the_milking_is_over_and_incomplete() {
        // Arrange
        MilkingSession session  = FourQuarterSession().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt = PrepareAll(session) + session.Settings.LetDownDelay;

        // Act
        foreach (UdderQuarter quarter in session.Animal.MilkableQuarters) {
            session.AttachCup(quarter, false, attachAt);
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.True(session.IsIncomplete);
        Assert.Equal(0m, session.TotalYieldKg);
    }

}
