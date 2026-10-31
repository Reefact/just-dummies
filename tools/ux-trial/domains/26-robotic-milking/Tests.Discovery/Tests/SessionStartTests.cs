using JustDummies;
using JustDummies.Xunit;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery.Tests;

[Reproducible]
public sealed class SessionStartTests {

    [Fact]
    public void A_cow_milked_less_than_the_minimum_interval_ago_is_sent_out_without_milking() {
        // Arrange
        DateTimeOffset  anyStart    = SessionSteps.AnyMilkingInstant().Generate();
        MilkingSettings anySettings = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();
        Animal          anyCow      = Any.Animal().WithLastMilkedAt(anyStart - TimeSpan.FromHours(7)).Generate();

        // Act
        Action act = () => Any.MilkingSession().WithAnimal(anyCow).WithStartedAt(anyStart).WithSettings(anySettings).Generate();

        // Assert
        Assert.Throws<MilkingRefusedException>(act);
    }

    [Fact]
    public void A_cow_milked_at_least_the_minimum_interval_ago_is_identified() {
        // Arrange
        DateTimeOffset  anyStart    = SessionSteps.AnyMilkingInstant().Generate();
        MilkingSettings anySettings = Any.MilkingSettings().WithMinimumInterval(TimeSpan.FromHours(8)).Generate();
        Animal          anyCow      = Any.Animal().WithLastMilkedAt(anyStart - TimeSpan.FromHours(8)).Generate();

        // Act
        MilkingSession session = Any.MilkingSession().WithAnimal(anyCow).WithStartedAt(anyStart).WithSettings(anySettings).Generate();

        // Assert
        Assert.Equal(SessionPhase.Identified, session.Phase);
    }

    [Fact]
    public void A_cow_last_milked_after_the_session_starts_is_an_invalid_argument() {
        // Arrange
        DateTimeOffset anyStart = SessionSteps.AnyMilkingInstant().Generate();
        Animal         anyCow   = Any.Animal().WithLastMilkedAt(anyStart + TimeSpan.FromMinutes(1)).Generate();

        // Act
        Action act = () => Any.MilkingSession().WithAnimal(anyCow).WithStartedAt(anyStart).Generate();

        // Assert
        ArgumentException error = Assert.Throws<ArgumentException>(act);
        Assert.Equal("startedAt", error.ParamName);
    }

    [Fact]
    public void A_session_follows_only_the_milkable_quarters() {
        // Arrange
        Animal anyCowBlindRightRear = Any.Animal().WithBlindQuarters([UdderQuarter.RightRear]).WithLastMilkedAt((DateTimeOffset?)null).Generate();

        // Act
        MilkingSession session = Any.MilkingSession().WithAnimal(anyCowBlindRightRear).Generate();

        // Assert
        Assert.Equal(3, session.Quarters.Count);
        Assert.DoesNotContain(session.Quarters, q => q.Quarter == UdderQuarter.RightRear);
        Assert.Throws<MilkingRefusedException>(() => session.QuarterOf(UdderQuarter.RightRear));
    }

}
