using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class MilkingSessionStartTests {

    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    private static DateTimeOffset AnyStart() {
        return Any.DateTimeOffset()
                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .Generate();
    }

    /// <summary>A session starting at <paramref name="startedAt"/> for a cow last milked <paramref name="sinceLastMilking"/> earlier, under a minimum interval of six hours.</summary>
    private static MilkingSession SessionAfter(DateTimeOffset startedAt, TimeSpan sinceLastMilking) {
        return Any.MilkingSession()
                  .WithStartedAt(startedAt)
                  .WithAnimal(cow => cow.WithLastMilkedAt(startedAt - sinceLastMilking))
                  .WithSettings(settings => settings.WithMinimumInterval(TimeSpan.FromHours(6)))
                  .Generate();
    }

    [Fact]
    public void A_new_session_follows_one_quarter_per_milkable_teat_none_of_them_started() {
        // Arrange
        Animal anyCow = Any.Animal().Generate();

        // Act
        MilkingSession session = Any.MilkingSession().WithAnimal(anyCow).Generate();

        // Assert
        Assert.Equal(anyCow.MilkableQuarters, Milking.MilkableQuartersOf(session));
        Assert.All(session.Quarters, quarter => {
            Assert.Equal(QuarterState.Pending, quarter.State);
            Assert.Equal(session.Settings.MaxAttachAttempts, quarter.MaxAttachAttempts);
            Assert.Equal(0, quarter.AttachAttempts);
            Assert.Equal(0m, quarter.YieldKg);
        });
    }

    [Fact]
    public void A_new_session_is_identified_with_nothing_milked_and_nobody_released() {
        // Act
        MilkingSession session = Any.MilkingSession().Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
        Assert.Null(session.ReleasedAt);
        Assert.Empty(session.Anomalies);
        Assert.Equal(0m, session.TotalYieldKg);
        Assert.Equal(0m, session.SaleableYieldKg);
        Assert.False(session.IsIncomplete);
    }

    [Fact]
    public void A_session_keeps_the_cow_the_settings_and_the_moment_it_was_given() {
        // Arrange
        Animal            anyCow       = Any.Animal().Generate();
        MilkingSettings   anySettings  = Any.MilkingSettings().Generate();
        DateTimeOffset    anyStartedAt = AnyStart();

        // Act
        MilkingSession session = Any.MilkingSession().WithAnimal(anyCow).WithSettings(anySettings).WithStartedAt(anyStartedAt).WithBoxNumber(5).Generate();

        // Assert
        Assert.Same(anyCow, session.Animal);
        Assert.Same(anySettings, session.Settings);
        Assert.Equal(anyStartedAt, session.StartedAt);
        Assert.Equal(5, session.BoxNumber);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    public void The_first_and_the_last_box_are_accepted(int boxNumber) {
        // Act
        MilkingSession session = Any.MilkingSession().WithBoxNumber(boxNumber).Generate();

        // Assert
        Assert.Equal(boxNumber, session.BoxNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(-3)]
    public void A_box_outside_one_to_eight_is_refused(int boxNumber) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.MilkingSession().WithBoxNumber(boxNumber).Generate());

        // Assert
        Assert.Equal("boxNumber", refusal.ParamName);
    }

    [Fact]
    public void A_session_without_a_cow_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Any.MilkingSession().WithAnimal((Animal)null!).Generate());
    }

    [Fact]
    public void A_session_without_settings_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Any.MilkingSession().WithSettings((MilkingSettings)null!).Generate());
    }

    [Fact]
    public void A_cow_never_milked_by_the_robot_is_milked_whenever_she_comes() {
        // Act
        MilkingSession session = Any.MilkingSession().WithAnimal(cow => cow.WithLastMilkedAt((DateTimeOffset?)null)).Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_milked_exactly_the_minimum_interval_ago_is_milked_again() {
        // Arrange
        DateTimeOffset anyStartedAt = AnyStart();

        // Act
        MilkingSession session = SessionAfter(anyStartedAt, TimeSpan.FromHours(6));

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_milked_one_tick_less_than_the_minimum_interval_ago_is_sent_out_without_milking() {
        // Arrange
        DateTimeOffset anyStartedAt = AnyStart();

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(() => SessionAfter(anyStartedAt, TimeSpan.FromHours(6) - OneTick));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
    }

    [Fact]
    public void A_cow_milked_a_moment_ago_is_sent_out_naming_her_ear_tag() {
        // Arrange
        AnimalId       cowId        = new AnimalId("FR123456789012");
        DateTimeOffset anyStartedAt = AnyStart();

        // Act
        MilkingRefusedException refusal = Assert.Throws<MilkingRefusedException>(
            () => Any.MilkingSession()
                     .WithStartedAt(anyStartedAt)
                     .WithAnimal(cow => cow.WithId(cowId).WithLastMilkedAt(anyStartedAt))
                     .Generate());

        // Assert
        Assert.Contains("FR123456789012", refusal.Message);
    }

    [Fact]
    public void A_cow_last_milked_after_the_session_starts_is_a_wrong_argument_not_a_refusal() {
        // Arrange
        DateTimeOffset anyStartedAt = AnyStart();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => SessionAfter(anyStartedAt, -OneTick));

        // Assert
        Assert.Equal("startedAt", refusal.ParamName);
    }

}
