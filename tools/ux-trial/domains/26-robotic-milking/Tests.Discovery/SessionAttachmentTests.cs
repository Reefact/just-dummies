using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class SessionAttachmentTests {

    private static readonly TimeSpan Delay = Scenarios.LetDownDelay;   // 20 s

    [Fact]
    public void No_cup_is_attached_while_a_milkable_teat_is_still_to_prepare() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        anySession.PrepareTeat(UdderQuarter.LeftFront, Scenarios.CleaningTime, endedAt: Scenarios.CleaningTime);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void A_cup_is_attached_once_every_teat_is_prepared_and_the_let_down_delay_has_passed() {
        // Arrange
        MilkingSession anySession    = Scenarios.Session().Generate();
        TimeSpan       preparedAt    = Scenarios.PrepareAllTeats(anySession);
        UdderQuarter   anyQuarter    = Any.Enum<UdderQuarter>().Generate();
        TimeSpan       attachedAt    = preparedAt + Delay;

        // Act
        anySession.AttachCup(anyQuarter, succeeded: true, attachedAt);

        // Assert
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Attached, milking.State);
        Assert.Equal(attachedAt, milking.AttachedAt);
        Assert.Equal(1, milking.AttachAttempts);
        Assert.True(milking.IsCupOn);
    }

    [Fact]
    public void A_cup_attached_before_the_let_down_delay_is_over_is_refused() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       preparedAt = Scenarios.PrepareAllTeats(anySession);
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => anySession.AttachCup(anyQuarter, succeeded: true, preparedAt + Delay - TimeSpan.FromTicks(1)));

        Assert.Equal(QuarterState.Prepared, anySession.QuarterOf(anyQuarter).State);
        Assert.Equal(0, anySession.QuarterOf(anyQuarter).AttachAttempts);
        Assert.Contains("let-down", refusal.Message);
    }

    [Fact]
    public void A_refused_attachment_does_not_use_up_an_attempt() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       preparedAt = Scenarios.PrepareAllTeats(anySession);
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(anyQuarter, succeeded: false, preparedAt));

        // Act
        anySession.AttachCup(anyQuarter, succeeded: false, preparedAt + Delay);

        // Assert
        Assert.Equal(1, anySession.QuarterOf(anyQuarter).AttachAttempts);
    }

    [Fact]
    public void A_failed_attempt_leaves_the_quarter_prepared_for_another_try() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt);

        // Assert
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Prepared, milking.State);
        Assert.Equal(1, milking.AttachAttempts);
        Assert.Null(milking.AttachedAt);
        Assert.False(milking.IsCupOn);
        Assert.False(milking.IsFinished);
    }

    [Fact]
    public void A_later_attempt_can_succeed_after_a_failure() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt);

        // Act
        anySession.AttachCup(anyQuarter, succeeded: true, attachAt + TimeSpan.FromSeconds(4));

        // Assert
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Attached, milking.State);
        Assert.Equal(2, milking.AttachAttempts);
        Assert.Equal(attachAt + TimeSpan.FromSeconds(4), milking.AttachedAt);
    }

    [Fact]
    public void A_quarter_is_given_up_when_its_last_allowed_attempt_fails() {
        // Arrange: three attempts allowed.
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();

        // Act
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt);
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt + TimeSpan.FromSeconds(4));
        QuarterState stateBeforeLastAttempt = anySession.QuarterOf(anyQuarter).State;
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt + TimeSpan.FromSeconds(8));

        // Assert
        Assert.Equal(QuarterState.Prepared, stateBeforeLastAttempt);
        QuarterMilking milking = anySession.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.AttachmentFailed, milking.State);
        Assert.Equal(3, milking.AttachAttempts);
        Assert.True(milking.IsFinished);
        Assert.True(anySession.IsIncomplete);
    }

    [Fact]
    public void A_last_attempt_that_succeeds_attaches_the_cup_instead_of_giving_up() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;
        UdderQuarter   anyQuarter = Any.Enum<UdderQuarter>().Generate();
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt);
        anySession.AttachCup(anyQuarter, succeeded: false, attachAt);

        // Act
        anySession.AttachCup(anyQuarter, succeeded: true, attachAt);

        // Assert
        Assert.Equal(QuarterState.Attached, anySession.QuarterOf(anyQuarter).State);
        Assert.False(anySession.IsIncomplete);
    }

    [Fact]
    public void A_single_attempt_settings_gives_a_quarter_up_at_the_first_failure() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;

        // Act
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, attachAt);

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, anySession.QuarterOf(UdderQuarter.RightRear).State);
    }

    [Fact]
    public void A_quarter_given_up_cannot_be_attached_again() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: false, attachAt);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(UdderQuarter.RightRear, succeeded: true, attachAt));
    }

    [Fact]
    public void A_cup_already_on_is_not_attached_a_second_time() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachedAt = Scenarios.AttachAllCups(anySession);

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, attachedAt));
    }

    [Fact]
    public void No_cup_is_attached_to_a_blind_quarter() {
        // Arrange
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear]))
                                              .Generate();
        TimeSpan attachAt = Scenarios.PrepareAllTeats(anySession) + Delay;

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.AttachCup(UdderQuarter.LeftRear, succeeded: true, attachAt));
    }

    [Fact]
    public void The_let_down_delay_runs_from_the_end_of_the_last_preparation_not_the_start_of_the_session() {
        // Arrange: a long pause before the last brushing pushes the end of the preparation to 5 min.
        MilkingSession anySession = Scenarios.Session()
                                              .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.LeftRear]))
                                              .Generate();
        anySession.PrepareTeat(UdderQuarter.RightRear, Scenarios.CleaningTime, endedAt: TimeSpan.FromMinutes(5));

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => anySession.AttachCup(UdderQuarter.RightRear, succeeded: true, TimeSpan.FromMinutes(5) + Delay - TimeSpan.FromTicks(1)));
        anySession.AttachCup(UdderQuarter.RightRear, succeeded: true, TimeSpan.FromMinutes(5) + Delay);

        Assert.Equal(QuarterState.Attached, anySession.QuarterOf(UdderQuarter.RightRear).State);
    }

    [Fact]
    public void Attaching_the_first_cup_leaves_the_robot_attaching_until_a_quarter_yields_milk() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;

        // Act
        anySession.AttachCup(UdderQuarter.LeftFront, succeeded: true, attachAt);

        // Assert
        Assert.Equal(SessionPhase.Attaching, anySession.Phase);
    }

    [Fact]
    public void A_cow_whose_every_attachment_fails_ends_the_milking_with_nothing_milked() {
        // Arrange
        MilkingSession anySession = Scenarios.Session().WithSettings(s => s.WithMaxAttachAttempts(1)).Generate();
        TimeSpan       attachAt   = Scenarios.PrepareAllTeats(anySession) + Delay;

        // Act
        foreach (UdderQuarter quarter in anySession.Animal.MilkableQuarters) {
            anySession.AttachCup(quarter, succeeded: false, attachAt);
        }

        // Assert
        Assert.Equal(SessionPhase.Completed, anySession.Phase);
        Assert.True(anySession.IsIncomplete);
        Assert.Equal(0m, anySession.TotalYieldKg);
    }

}
