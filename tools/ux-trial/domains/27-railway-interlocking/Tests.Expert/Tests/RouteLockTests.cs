using JustDummies;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

public sealed class RouteLockTests {

    [Fact]
    public void A_route_lock_without_a_lock_time_is_refused() {
        // Arrange
        AnyRouteLock anyUnstampedLock = Any.RouteLock().WithLockedAt(default(DateTimeOffset));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyUnstampedLock.Generate());

        // Assert
        Assert.Equal("lockedAt", refusal.ParamName);
    }

    [Fact]
    public void A_new_route_lock_holds_every_section_of_its_route_in_running_order() {
        // Arrange
        SectionId[] runningOrder  = [new("T101"), new("T102"), new("AB7")];
        Route       anyRouteOverThem = Any.Route().WithSections(runningOrder).Generate();

        // Act
        RouteLock routeLock = Any.RouteLock().WithRoute(anyRouteOverThem).Generate();

        // Assert
        Assert.Equal(runningOrder, routeLock.RemainingSections);
        Assert.False(routeLock.IsFullyReleased);
    }

}
