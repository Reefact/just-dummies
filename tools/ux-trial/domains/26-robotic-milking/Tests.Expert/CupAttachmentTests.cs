using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class CupAttachmentTests {

    private static readonly TimeSpan OneTick       = TimeSpan.FromTicks(1);
    private static readonly TimeSpan TwentySeconds = TimeSpan.FromSeconds(20);

    [Fact]
    public void A_cup_cannot_be_attached_while_a_milkable_teat_is_still_unprepared() {
        // Arrange
        MilkingSession session  = Milking.SessionOfAFullUdder().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront,  cleaning, cleaning * 1);
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning * 2);
        session.PrepareTeat(UdderQuarter.LeftRear,   cleaning, cleaning * 3);
        TimeSpan longAfterLetDown = cleaning * 3 + session.Settings.LetDownDelay + TimeSpan.FromMinutes(5);

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(UdderQuarter.LeftFront, succeeded: true, longAfterLetDown));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(0, session.QuarterOf(UdderQuarter.LeftFront).AttachAttempts);
    }

    [Fact]
    public void A_cup_attached_the_instant_the_let_down_delay_is_over_stays_on() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(settings => settings.WithLetDownDelay(TwentySeconds)).Generate();
        UdderQuarter   quarter    = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       preparedAt = Milking.PrepareEveryTeat(session);

        // Act
        session.AttachCup(quarter, succeeded: true, preparedAt + TwentySeconds);

        // Assert
        QuarterMilking milking = session.QuarterOf(quarter);
        Assert.Equal(QuarterState.Attached, milking.State);
        Assert.Equal(preparedAt + TwentySeconds, milking.AttachedAt);
        Assert.Equal(1, milking.AttachAttempts);
        Assert.True(milking.IsCupOn);
    }

    [Fact]
    public void A_cup_attached_one_tick_before_the_let_down_delay_is_over_is_refused() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(settings => settings.WithLetDownDelay(TwentySeconds)).Generate();
        UdderQuarter   quarter    = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       preparedAt = Milking.PrepareEveryTeat(session);

        // Act
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(quarter, succeeded: true, preparedAt + TwentySeconds - OneTick));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(quarter).State);
        Assert.Equal(0, session.QuarterOf(quarter).AttachAttempts);
    }

    [Fact]
    public void Without_a_let_down_delay_the_cups_go_on_as_soon_as_the_last_teat_is_prepared() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(settings => settings.WithLetDownDelay(TimeSpan.Zero)).Generate();
        UdderQuarter   quarter    = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       preparedAt = Milking.PrepareEveryTeat(session);

        // Act
        session.AttachCup(quarter, succeeded: true, preparedAt);

        // Assert
        Assert.Equal(QuarterState.Attached, session.QuarterOf(quarter).State);
    }

    [Fact]
    public void Attaching_every_cup_leaves_the_session_attaching_until_milk_flows() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Act
        Milking.AttachEveryCup(anySession);

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
        Assert.All(anySession.Quarters, quarter => Assert.Equal(QuarterState.Attached, quarter.State));
    }

    [Fact]
    public void A_failed_attempt_with_attempts_left_leaves_the_teat_prepared_for_another_try() {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithSettings(settings => settings.WithMaxAttachAttempts(2)).Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(quarter, succeeded: false, readyAt);

        // Assert
        QuarterMilking milking = session.QuarterOf(quarter);
        Assert.Equal(QuarterState.Prepared, milking.State);
        Assert.Equal(1, milking.AttachAttempts);
        Assert.False(milking.IsCupOn);
    }

    [Fact]
    public void A_retry_that_succeeds_attaches_the_cup_and_counts_both_attempts() {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithSettings(settings => settings.WithMaxAttachAttempts(2)).Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;
        session.AttachCup(quarter, succeeded: false, readyAt);

        // Act
        session.AttachCup(quarter, succeeded: true, readyAt + TimeSpan.FromSeconds(8));

        // Assert
        QuarterMilking milking = session.QuarterOf(quarter);
        Assert.Equal(QuarterState.Attached, milking.State);
        Assert.Equal(2, milking.AttachAttempts);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void The_last_allowed_failure_gives_the_quarter_up_and_not_one_failure_sooner(int allowedAttempts) {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithSettings(settings => settings.WithMaxAttachAttempts(allowedAttempts)).Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        // Act
        for (int failure = 1; failure < allowedAttempts; failure++) {
            session.AttachCup(quarter, succeeded: false, readyAt);
        }

        QuarterState stateBeforeTheLastFailure = session.QuarterOf(quarter).State;
        session.AttachCup(quarter, succeeded: false, readyAt);

        // Assert
        Assert.Equal(QuarterState.Prepared, stateBeforeTheLastFailure);
        Assert.Equal(QuarterState.AttachmentFailed, session.QuarterOf(quarter).State);
        Assert.Equal(allowedAttempts, session.QuarterOf(quarter).AttachAttempts);
    }

    [Fact]
    public void A_quarter_given_up_takes_no_further_attempt() {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithSettings(settings => settings.WithMaxAttachAttempts(1)).Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;
        session.AttachCup(quarter, succeeded: false, readyAt);

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(quarter, succeeded: true, readyAt));
    }

    [Fact]
    public void A_cup_already_on_is_not_attached_again() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();
        UdderQuarter   quarter = Milking.MilkableQuartersOf(session)[0];
        TimeSpan       readyAt = Milking.AttachEveryCup(session);

        // Act & Assert
        Assert.Throws<MilkingRefusedException>(() => session.AttachCup(quarter, succeeded: true, readyAt + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void A_blind_quarter_takes_no_cup() {
        // Arrange
        MilkingSession session      = Any.MilkingSession().WithAnimal(cow => cow.WithBlindQuarters(blind => blind.NonEmpty())).Generate();
        UdderQuarter   blindQuarter = session.Animal.BlindQuarters.First();
        TimeSpan       readyAt      = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => session.AttachCup(blindQuarter, succeeded: true, readyAt));

        // Assert
        Assert.Contains(blindQuarter.ToString(), refusal.Message);
    }

    [Fact]
    public void When_every_quarter_is_given_up_the_milking_is_completed_empty_and_incomplete() {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithSettings(settings => settings.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        // Act
        foreach (UdderQuarter quarter in Milking.MilkableQuartersOf(session)) {
            session.AttachCup(quarter, succeeded: false, readyAt);
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.True(session.IsIncomplete);
        Assert.Equal(0m, session.TotalYieldKg);
    }

    [Fact]
    public void While_one_quarter_still_has_attempts_left_the_session_is_not_completed() {
        // Arrange
        MilkingSession session = Milking.SessionOfAFullUdder().WithSettings(settings => settings.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       readyAt = Milking.PrepareEveryTeat(session) + session.Settings.LetDownDelay;

        // Act
        session.AttachCup(UdderQuarter.LeftFront,  succeeded: false, readyAt);
        session.AttachCup(UdderQuarter.RightFront, succeeded: false, readyAt);
        session.AttachCup(UdderQuarter.LeftRear,   succeeded: false, readyAt);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
        Assert.True(session.IsIncomplete);
    }

}
