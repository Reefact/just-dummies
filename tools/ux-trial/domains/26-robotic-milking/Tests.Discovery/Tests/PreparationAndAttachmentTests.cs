using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery.Tests;

[Reproducible]
public sealed class PreparationAndAttachmentTests {

    [Fact]
    public void Brushing_the_first_teat_starts_the_preparation_phase() {
        // Arrange
        // With a single milkable teat, the first brushing is also the last: the cow needs at least two.
        MilkingSession session    = Any.MilkingSession().WithAnimal(a => a.WithBlindQuarters(b => b.WithMaxCount(2))).Generate();
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        session.PrepareTeat(anyQuarter, session.Settings.CleaningTime, session.Settings.CleaningTime);

        // Assert
        Assert.Equal(SessionPhase.Preparing, session.Phase);
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void A_teat_brushed_for_less_than_the_cleaning_time_is_refused() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(20))).Generate();
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        Action act = () => session.PrepareTeat(anyQuarter, TimeSpan.FromSeconds(19), TimeSpan.FromSeconds(19));

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
        Assert.Equal(QuarterState.Pending, session.QuarterOf(anyQuarter).State);
    }

    [Fact]
    public void Two_brushings_cannot_overlap() {
        // Arrange
        Animal         anyCowWithFourTeats = Any.Animal().WithBlindQuarters([]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session             = Any.MilkingSession().WithAnimal(anyCowWithFourTeats).WithSettings(s => s.WithCleaningTime(TimeSpan.FromSeconds(10))).Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

        // Act
        Action act = () => session.PrepareTeat(UdderQuarter.RightFront, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(35));

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_blind_teat_is_never_prepared() {
        // Arrange
        Animal         anyCowBlindLeftFront = Any.Animal().WithBlindQuarters([UdderQuarter.LeftFront]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session              = Any.MilkingSession().WithAnimal(anyCowBlindLeftFront).Generate();

        // Act
        Action act = () => session.PrepareTeat(UdderQuarter.LeftFront, session.Settings.CleaningTime, session.Settings.CleaningTime);

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_teat_is_prepared_only_once() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().Generate();
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        TimeSpan       cleaning   = session.Settings.CleaningTime;
        session.PrepareTeat(anyQuarter, cleaning, cleaning);

        // Act
        Action act = () => session.PrepareTeat(anyQuarter, cleaning, cleaning * 2);

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void Once_every_milkable_teat_is_prepared_the_robot_attaches_cups() {
        // Arrange
        MilkingSession session = Any.MilkingSession().Generate();

        // Act
        SessionSteps.PrepareEveryTeat(session);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
    }

    [Fact]
    public void No_cup_is_attached_before_every_milkable_teat_is_prepared() {
        // Arrange
        Animal         anyCowWithFourTeats = Any.Animal().WithBlindQuarters([]).WithLastMilkedAt((DateTimeOffset?)null).Generate();
        MilkingSession session             = Any.MilkingSession().WithAnimal(anyCowWithFourTeats).Generate();
        TimeSpan       cleaning            = session.Settings.CleaningTime;
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);

        // Act
        Action act = () => session.AttachCup(UdderQuarter.LeftFront, succeeded: true, TimeSpan.FromMinutes(10));

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void No_cup_is_attached_before_the_let_down_delay_has_passed() {
        // Arrange
        MilkingSession session        = Any.MilkingSession().WithSettings(s => s.WithLetDownDelay(TimeSpan.FromSeconds(60))).Generate();
        TimeSpan       preparationEnd = SessionSteps.PrepareEveryTeat(session);
        UdderQuarter   anyQuarter     = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        Action act = () => session.AttachCup(anyQuarter, succeeded: true, preparationEnd + TimeSpan.FromSeconds(59));

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_successful_attachment_puts_the_cup_on() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().Generate();
        TimeSpan       letDown    = SessionSteps.LetDownReachedAfter(session, SessionSteps.PrepareEveryTeat(session));
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        session.AttachCup(anyQuarter, succeeded: true, letDown);

        // Assert
        QuarterMilking quarter = session.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Attached, quarter.State);
        Assert.True(quarter.IsCupOn);
        Assert.Equal(letDown, quarter.AttachedAt);
    }

    [Fact]
    public void The_last_allowed_failed_attempt_gives_the_quarter_up() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(s => s.WithMaxAttachAttempts(2)).Generate();
        TimeSpan       letDown    = SessionSteps.LetDownReachedAfter(session, SessionSteps.PrepareEveryTeat(session));
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();
        session.AttachCup(anyQuarter, succeeded: false, letDown);

        // Act
        session.AttachCup(anyQuarter, succeeded: false, letDown + TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(QuarterState.AttachmentFailed, session.QuarterOf(anyQuarter).State);
        Assert.True(session.IsIncomplete);
    }

    [Fact]
    public void A_failed_attempt_before_the_last_leaves_the_quarter_to_retry() {
        // Arrange
        MilkingSession session    = Any.MilkingSession().WithSettings(s => s.WithMaxAttachAttempts(3)).Generate();
        TimeSpan       letDown    = SessionSteps.LetDownReachedAfter(session, SessionSteps.PrepareEveryTeat(session));
        UdderQuarter   anyQuarter = Any.ElementOf(session.Animal.MilkableQuarters).Generate();

        // Act
        session.AttachCup(anyQuarter, succeeded: false, letDown);

        // Assert
        QuarterMilking quarter = session.QuarterOf(anyQuarter);
        Assert.Equal(QuarterState.Prepared, quarter.State);
        Assert.Equal(1, quarter.AttachAttempts);
    }

}
