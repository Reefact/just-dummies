using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using static RoboticMilking.Tests.Regular.MilkingSessionSupport;

namespace RoboticMilking.Tests.Regular;

[Reproducible]
public sealed class MilkingSessionStartTests {

    [Fact]
    public void A_new_session_follows_one_quarter_per_milkable_teat_all_pending() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.LeftRear).Generate();

        // Assert
        Assert.Equal(3, session.Quarters.Count);
        Assert.All(session.Quarters, q => Assert.Equal(QuarterState.Pending, q.State));
        Assert.Equal(SessionPhase.Identified, session.Phase);
        Assert.Equal(session.Settings.MaxAttachAttempts, session.Quarters.First().MaxAttachAttempts);
    }

    [Fact]
    public void A_new_session_has_nothing_yielded_and_nothing_reported() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Assert
        Assert.Equal(0m, anySession.TotalYieldKg);
        Assert.Equal(0m, anySession.SaleableYieldKg);
        Assert.False(anySession.IsIncomplete);
        Assert.Empty(anySession.Anomalies);
        Assert.Null(anySession.ReleasedAt);
    }

    [Fact]
    public void A_session_keeps_the_animal_box_start_and_settings_it_was_given() {
        // Arrange
        Animal           anyCow      = Any.Animal().WithoutLastMilkedAt().Generate();
        MilkingSettings  anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset   startedAt   = new DateTimeOffset(2026, 5, 17, 6, 0, 0, TimeSpan.FromHours(2));

        // Act
        MilkingSession session = new MilkingSession(anyCow, 5, startedAt, anySettings);

        // Assert
        Assert.Same(anyCow, session.Animal);
        Assert.Equal(5, session.BoxNumber);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Same(anySettings, session.Settings);
    }

    [Fact]
    public void A_quarter_that_is_blind_cannot_be_looked_up() {
        // Arrange
        MilkingSession session = SessionWithBlind(UdderQuarter.RightFront).Generate();

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => session.QuarterOf(UdderQuarter.RightFront));

        // Assert
        Assert.Contains("blind", refusal.Message);
        Assert.Equal(UdderQuarter.LeftFront, session.QuarterOf(UdderQuarter.LeftFront).Quarter);
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_can_start_whenever() {
        // Arrange
        Animal         anyFreshCow = Any.Animal().WithoutLastMilkedAt().Generate();
        DateTimeOffset anyStart    = Any.DateTimeOffset().Generate();

        // Act
        MilkingSession session = new MilkingSession(anyFreshCow, 1, anyStart, Any.MilkingSettings().Generate());

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_milked_exactly_the_minimum_interval_ago_is_accepted() {
        // Arrange
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  startedAt   = new DateTimeOffset(2026, 5, 17, 18, 0, 0, TimeSpan.Zero);
        Animal          cow         = Any.Animal().WithLastMilkedAt(startedAt - anySettings.MinimumInterval).Generate();

        // Act
        MilkingSession session = new MilkingSession(cow, 2, startedAt, anySettings);

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_milked_a_tick_less_than_the_minimum_interval_ago_is_sent_out() {
        // Arrange
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  startedAt   = new DateTimeOffset(2026, 5, 17, 18, 0, 0, TimeSpan.Zero);
        Animal          cow         = Any.Animal().WithLastMilkedAt(startedAt - anySettings.MinimumInterval + TimeSpan.FromTicks(1)).Generate();

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => new MilkingSession(cow, 2, startedAt, anySettings));

        Assert.Contains(cow.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_cow_that_was_milked_when_the_session_starts_is_refused_not_rejected_as_invalid() {
        // Arrange: equal instants, so zero hours since the last milking.
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  startedAt   = new DateTimeOffset(2026, 5, 17, 18, 0, 0, TimeSpan.Zero);
        Animal          cow         = Any.Animal().WithLastMilkedAt(startedAt).Generate();

        // Assert
        Assert.Throws<MilkingRefusedException>(() => new MilkingSession(cow, 2, startedAt, anySettings));
    }

    [Fact]
    public void A_last_milking_after_the_start_is_an_invalid_argument() {
        // Arrange
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  startedAt   = new DateTimeOffset(2026, 5, 17, 18, 0, 0, TimeSpan.Zero);
        Animal          cow         = Any.Animal().WithLastMilkedAt(startedAt.AddTicks(1)).Generate();

        // Act / Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MilkingSession(cow, 2, startedAt, anySettings));

        Assert.IsNotType<MilkingRefusedException>(refusal);
        Assert.Equal("startedAt", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-1)]
    public void A_box_outside_one_to_eight_is_refused(int box) {
        // Arrange
        Animal          anyCow      = Any.Animal().WithoutLastMilkedAt().Generate();
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  anyStart    = Any.DateTimeOffset().Generate();

        // Act / Assert
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MilkingSession(anyCow, box, anyStart, anySettings));

        Assert.Equal("boxNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void The_first_and_last_boxes_are_accepted(int box) {
        // Arrange
        MilkingSession session = Any.MilkingSession().WithBoxNumber(box).Generate();

        // Assert
        Assert.Equal(box, session.BoxNumber);
    }

    [Fact]
    public void A_missing_animal_or_settings_is_refused() {
        // Arrange
        Animal          anyCow      = Any.Animal().WithoutLastMilkedAt().Generate();
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  anyStart    = Any.DateTimeOffset().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new MilkingSession(null!, 1, anyStart, anySettings));
        Assert.Throws<ArgumentNullException>(() => new MilkingSession(anyCow, 1, anyStart, null!));
    }

    [Fact]
    public void An_arbitrary_session_can_be_started() {
        // Arrange / Act: the draw itself must not break the minimum interval.
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, anySession.Phase);
    }

}
