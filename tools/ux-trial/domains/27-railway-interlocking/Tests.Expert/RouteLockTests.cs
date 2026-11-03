namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class RouteLockTests {

    [Fact]
    public void A_new_lock_holds_the_route_for_the_train_from_the_time_it_was_set() {
        Route          anyRoute   = Any.Route().Generate();
        TrainId        train      = new TrainId("1A23");
        DateTimeOffset lockedAt   = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

        RouteLock routeLock = new RouteLock(anyRoute, train, lockedAt);

        Assert.Same(anyRoute, routeLock.Route);
        Assert.Equal(train, routeLock.Train);
        Assert.Equal(lockedAt, routeLock.LockedAt);
    }

    [Fact]
    public void A_new_lock_still_holds_every_section_of_the_route_in_running_order_and_no_train_has_entered() {
        Route anyRoute = Any.Route().WithSections(Any.ListOf(Any.SectionId()).WithCountBetween(2, 6).Distinct().Generate()).Generate();

        RouteLock routeLock = Any.RouteLock().WithRoute(anyRoute).Generate();

        Assert.Equal(anyRoute.Sections, routeLock.RemainingSections);
        Assert.False(routeLock.TrainHasEntered);
        Assert.False(routeLock.IsFullyReleased);
        Assert.All(anyRoute.Sections, section => Assert.False(routeLock.HasVisited(section)));
    }

    [Fact]
    public void A_lock_needs_a_lock_time() {
        RouteLock anyLock = Any.RouteLock().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new RouteLock(anyLock.Route, anyLock.Train, default));

        Assert.Equal("lockedAt", refusal.ParamName);
    }

    [Fact]
    public void A_lock_needs_a_route_and_a_train() {
        RouteLock anyLock = Any.RouteLock().Generate();

        Assert.Throws<ArgumentNullException>(() => new RouteLock(null!, anyLock.Train, anyLock.LockedAt));
        Assert.Throws<ArgumentNullException>(() => new RouteLock(anyLock.Route, null!, anyLock.LockedAt));
    }

}
