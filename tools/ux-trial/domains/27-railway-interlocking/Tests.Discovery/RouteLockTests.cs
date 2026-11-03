using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class RouteLockTests {

    [Fact]
    public void A_new_lock_holds_every_section_of_its_route_in_running_order_and_nobody_has_entered() {
        // Arrange
        SectionId[] running = { new SectionId("S3"), new SectionId("S1"), new SectionId("S2") };
        Route       route   = Any.Route().WithSections(running).Generate();

        // Act
        RouteLock routeLock = Any.RouteLock().WithRoute(route).Generate();

        // Assert
        Assert.Equal(running, routeLock.RemainingSections);
        Assert.False(routeLock.TrainHasEntered);
        Assert.False(routeLock.IsFullyReleased);
        Assert.False(routeLock.HasVisited(running[0]));
    }

    [Fact]
    public void Keeps_the_route_the_train_and_the_time() {
        // Arrange
        Route          anyRoute = Any.Route().Generate();
        TrainId        anyTrain = Any.TrainId().Generate();
        DateTimeOffset lockedAt = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.FromHours(1));

        // Act
        RouteLock routeLock = new RouteLock(anyRoute, anyTrain, lockedAt);

        // Assert
        Assert.Same(anyRoute, routeLock.Route);
        Assert.Same(anyTrain, routeLock.Train);
        Assert.Equal(lockedAt, routeLock.LockedAt);
    }

    [Fact]
    public void Does_not_share_its_remaining_sections_with_the_route() {
        // Arrange
        Route     route     = Any.Route().WithSections(new[] { new SectionId("S1"), new SectionId("S2") }).Generate();
        RouteLock routeLock = Any.RouteLock().WithRoute(route).Generate();

        // Assert
        Assert.NotSame(route.Sections, routeLock.RemainingSections);
    }

    [Fact]
    public void Refuses_a_null_route() {
        // Arrange
        TrainId        anyTrain = Any.TrainId().Generate();
        DateTimeOffset anyTime  = Yard.AnyInstant();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RouteLock(null!, anyTrain, anyTime));
    }

    [Fact]
    public void Refuses_a_null_train() {
        // Arrange
        Route          anyRoute = Any.Route().Generate();
        DateTimeOffset anyTime  = Yard.AnyInstant();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RouteLock(anyRoute, null!, anyTime));
    }

    [Fact]
    public void Refuses_a_lock_time_that_is_not_set() {
        // Arrange
        Route   anyRoute = Any.Route().Generate();
        TrainId anyTrain = Any.TrainId().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RouteLock(anyRoute, anyTrain, default));

        // Assert
        Assert.Equal("lockedAt", refusal.ParamName);
    }

}
