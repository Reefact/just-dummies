using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using static RoboticMilking.Tests.Regular.MilkingSessionSupport;

namespace RoboticMilking.Tests.Regular;

[Reproducible]
public sealed class MilkingSessionPreparationTests {

    private static readonly TimeSpan Ten = TimeSpan.FromSeconds(10);

    // Cleaning time of ten seconds, and a delay long enough for any settings rule.
    private static AnyMilkingSession TenSecondBrushing() {
        return FourQuarterSession().WithSettings(s => s.WithCleaningTime(Ten));
    }

    [Fact]
    public void Preparing_a_teat_marks_its_quarter_prepared_and_opens_the_preparation() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();

        // Act
        session.PrepareTeat(UdderQuarter.RightRear, session.Settings.CleaningTime, session.Settings.CleaningTime);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.RightRear).State);
        Assert.Equal(QuarterState.Pending, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Equal(SessionPhase.Preparing, session.Phase);
    }

    [Fact]
    public void Teats_may_be_prepared_in_any_order() {
        // Arrange
        MilkingSession session = FourQuarterSession().Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act
        session.PrepareTeat(UdderQuarter.RightRear, cleaning, cleaning);
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning * 2);
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning * 3);
        session.PrepareTeat(UdderQuarter.LeftRear, cleaning, cleaning * 4);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
    }

    [Fact]
    public void Preparing_every_milkable_teat_leaves_the_session_ready_to_attach() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.LeftFront).Generate();

        // Act
        PrepareAll(session);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Prepared, q.State));
    }

    [Fact]
    public void A_brushing_exactly_as_long_as_the_cleaning_time_is_enough() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();

        // Act
        session.PrepareTeat(UdderQuarter.LeftFront, Ten, Ten);

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_brushing_shorter_than_the_cleaning_time_is_refused() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.PrepareTeat(UdderQuarter.LeftFront, Ten - TimeSpan.FromTicks(1), Ten));

        Assert.Equal(QuarterState.Pending, session.QuarterOf(UdderQuarter.LeftFront).State);
        Assert.Contains("requires", refusal.Message);
    }

    [Fact]
    public void A_brushing_longer_than_required_is_accepted() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();

        // Act
        session.PrepareTeat(UdderQuarter.LeftFront, TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(25));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.LeftFront).State);
    }

    [Fact]
    public void A_brushing_that_starts_before_the_previous_one_ended_is_refused() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, Ten, TimeSpan.FromSeconds(20));  // brushed from 10 s to 20 s

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.PrepareTeat(UdderQuarter.RightFront, Ten, TimeSpan.FromSeconds(29)));  // from 19 s

        Assert.Contains("overlaps", refusal.Message);
        Assert.Equal(QuarterState.Pending, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_brushing_that_starts_exactly_when_the_previous_one_ended_is_accepted() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, Ten, TimeSpan.FromSeconds(20));

        // Act
        session.PrepareTeat(UdderQuarter.RightFront, Ten, TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void A_pause_between_two_brushings_is_accepted() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, Ten, Ten);

        // Act
        session.PrepareTeat(UdderQuarter.RightFront, Ten, TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(QuarterState.Prepared, session.QuarterOf(UdderQuarter.RightFront).State);
    }

    [Fact]
    public void The_first_brushing_cannot_have_started_before_the_session() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();

        // Act / Assert: ending at 9 s a brushing of 10 s would have begun at -1 s.
        Assert.Throws<MilkingRefusedException>(
            () => session.PrepareTeat(UdderQuarter.LeftFront, Ten, TimeSpan.FromSeconds(9)));
    }

    [Fact]
    public void A_teat_is_prepared_only_once() {
        // Arrange
        MilkingSession session = TenSecondBrushing().Generate();
        session.PrepareTeat(UdderQuarter.LeftFront, Ten, Ten);

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => session.PrepareTeat(UdderQuarter.LeftFront, Ten, TimeSpan.FromSeconds(30)));

        Assert.Contains("Prepared", refusal.Message);
    }

    [Fact]
    public void A_blind_quarter_is_never_prepared() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.RightRear).Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => session.PrepareTeat(UdderQuarter.RightRear, cleaning, cleaning));
    }

    [Fact]
    public void Preparation_can_be_completed_without_the_blind_quarter() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.RightRear).Generate();
        TimeSpan       cleaning = session.Settings.CleaningTime;

        // Act
        session.PrepareTeat(UdderQuarter.LeftFront, cleaning, cleaning);
        session.PrepareTeat(UdderQuarter.RightFront, cleaning, cleaning * 2);
        session.PrepareTeat(UdderQuarter.LeftRear, cleaning, cleaning * 3);

        // Assert
        Assert.Equal(SessionPhase.Attaching, session.Phase);
    }

}
