using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class MilkingSessionConstructionTests {

    [Fact]
    public void A_new_session_has_been_identified_and_nothing_else() {
        // Act
        MilkingSession anySession = Any.MilkingSession().Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, anySession.Phase);
        Assert.Null(anySession.ReleasedAt);
        Assert.Empty(anySession.Anomalies);
        Assert.Equal(0m, anySession.TotalYieldKg);
        Assert.Equal(0m, anySession.SaleableYieldKg);
        Assert.False(anySession.IsIncomplete);
        Assert.All(anySession.Quarters, q => Assert.Equal(QuarterState.Pending, q.State));
    }

    [Fact]
    public void A_session_keeps_the_cow_the_box_the_start_and_the_settings() {
        // Arrange
        Animal          anyCow      = Any.Animal().WithoutLastMilkedAt().Generate();
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  startedAt   = new DateTimeOffset(2026, 6, 1, 6, 45, 0, TimeSpan.FromHours(2));

        // Act
        MilkingSession session = new MilkingSession(anyCow, boxNumber: 5, startedAt, anySettings);

        // Assert
        Assert.Same(anyCow, session.Animal);
        Assert.Equal(5, session.BoxNumber);
        Assert.Equal(startedAt, session.StartedAt);
        Assert.Same(anySettings, session.Settings);
    }

    [Fact]
    public void A_session_follows_one_quarter_per_milkable_quarter_of_the_cow() {
        // Act
        MilkingSession anySession = Any.MilkingSession()
                                       .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.LeftRear]))
                                       .Generate();

        // Assert
        Assert.Equal(3, anySession.Quarters.Count);
        Assert.DoesNotContain(anySession.Quarters, q => q.Quarter == UdderQuarter.LeftRear);
        Assert.Equal([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.RightRear],
                     anySession.Quarters.Select(q => q.Quarter).OrderBy(q => q));
    }

    [Fact]
    public void Each_followed_quarter_is_given_the_attempts_the_settings_allow() {
        // Act
        MilkingSession anySession = Any.MilkingSession()
                                       .WithSettings(s => s.WithMaxAttachAttempts(4))
                                       .Generate();

        // Assert
        Assert.All(anySession.Quarters, q => Assert.Equal(4, q.MaxAttachAttempts));
    }

    [Fact]
    public void A_blind_quarter_cannot_be_looked_up() {
        // Arrange
        MilkingSession anySession = Any.MilkingSession()
                                       .WithAnimal(a => a.WithBlindQuarters([UdderQuarter.RightFront]))
                                       .Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(() => anySession.QuarterOf(UdderQuarter.RightFront));
        Assert.Equal(UdderQuarter.LeftFront, anySession.QuarterOf(UdderQuarter.LeftFront).Quarter);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void A_box_from_one_to_eight_is_accepted(int box) {
        MilkingSession anySession = Any.MilkingSession().WithBoxNumber(box).Generate();

        Assert.Equal(box, anySession.BoxNumber);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(int.MaxValue)]
    public void A_box_outside_one_to_eight_is_refused(int box) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.MilkingSession().WithBoxNumber(box).Generate());

        Assert.Equal("boxNumber", refusal.ParamName);
    }

    [Fact]
    public void A_missing_cow_is_refused() {
        // Arrange
        MilkingSettings anySettings = Any.MilkingSettings().Generate();
        DateTimeOffset  anyStart    = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MilkingSession(null!, 1, anyStart, anySettings));
    }

    [Fact]
    public void Missing_settings_are_refused() {
        // Arrange
        Animal         anyCow   = Any.Animal().Generate();
        DateTimeOffset anyStart = Any.DateTimeOffset().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MilkingSession(anyCow, 1, anyStart, null!));
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_can_be_milked_at_any_time() {
        // Arrange
        Animal         anyNewCow = Any.Animal().WithoutLastMilkedAt().Generate();
        DateTimeOffset anyStart  = Any.DateTimeOffset().Generate();

        // Act
        MilkingSession session = new MilkingSession(anyNewCow, 2, anyStart, Any.MilkingSettings().Generate());

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_whose_last_milking_ended_after_the_session_starts_is_refused_as_an_invalid_argument() {
        // Arrange
        DateTimeOffset  lastMilkedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Animal          anyCow       = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();
        MilkingSettings anySettings  = Any.MilkingSettings().Generate();

        // Act / Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new MilkingSession(anyCow, 1, lastMilkedAt - TimeSpan.FromTicks(1), anySettings));

        Assert.Equal("startedAt", refusal.ParamName);
        Assert.IsNotType<MilkingRefusedException>(refusal);
    }

    [Fact]
    public void A_cow_milked_less_than_the_minimum_interval_ago_is_sent_out_without_milking() {
        // Arrange
        DateTimeOffset  lastMilkedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Animal          anyCow       = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();
        MilkingSettings settings     = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();

        // Act / Assert
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => new MilkingSession(anyCow, 1, lastMilkedAt + TimeSpan.FromHours(8) - TimeSpan.FromTicks(1), settings));

        Assert.Contains(anyCow.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_cow_milked_exactly_the_minimum_interval_ago_is_milked() {
        // Arrange
        DateTimeOffset  lastMilkedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Animal          anyCow       = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();
        MilkingSettings settings     = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();

        // Act
        MilkingSession session = new MilkingSession(anyCow, 1, lastMilkedAt + TimeSpan.FromHours(8), settings);

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_that_has_just_been_milked_is_refused_even_at_the_very_same_instant() {
        // Arrange
        DateTimeOffset lastMilkedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Animal         anyCow       = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => new MilkingSession(anyCow, 1, lastMilkedAt, Any.MilkingSettings().Generate()));
    }

    [Fact]
    public void The_interval_is_measured_on_instants_whatever_the_offsets_they_are_written_in() {
        // Arrange: 12:00 UTC and 14:00+02:00 are the same instant.
        DateTimeOffset  lastMilkedAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        Animal          anyCow       = Any.Animal().WithLastMilkedAt(lastMilkedAt).Generate();
        MilkingSettings settings     = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(4)).Generate();

        // Act / Assert
        Assert.Throws<MilkingRefusedException>(
            () => new MilkingSession(anyCow, 1, new DateTimeOffset(2026, 6, 1, 17, 59, 0, TimeSpan.FromHours(2)), settings));   // 15:59 UTC
        MilkingSession session = new MilkingSession(anyCow, 1, new DateTimeOffset(2026, 6, 1, 18, 0, 0, TimeSpan.FromHours(2)), settings);   // 16:00 UTC

        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

}
