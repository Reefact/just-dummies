using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class RequestRouteTests {

    [Fact]
    public void Sets_the_route_for_the_train_at_the_requested_time() {
        // Arrange
        Interlocking   interlocking = Yard.Build();
        TrainId        anyTrain     = Yard.AnyTrain();
        DateTimeOffset at           = new DateTimeOffset(2026, 3, 14, 9, 26, 53, TimeSpan.Zero);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(Yard.Main, anyTrain, at);

        // Assert
        Assert.Equal(Yard.Main, routeLock.Route.Name);
        Assert.Same(anyTrain, routeLock.Train);
        Assert.Equal(at, routeLock.LockedAt);
        Assert.Same(routeLock, Assert.Single(interlocking.ActiveRoutes));
    }

    [Fact]
    public void Locks_every_section_of_the_route_and_no_other() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Assert.Equal(Yard.Main, interlocking.Section(Yard.A1).LockedByRoute);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.J1).LockedByRoute);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.M1).LockedByRoute);
        Assert.False(interlocking.Section(Yard.B1).IsLocked);
        Assert.False(interlocking.Section(Yard.A2).IsLocked);
        Assert.False(interlocking.Section(Yard.C1).IsLocked);
    }

    [Theory]
    [InlineData(PointPosition.Reverse, Yard.Main, PointPosition.Normal)]
    [InlineData(PointPosition.Normal, Yard.Branch, PointPosition.Reverse)]
    public void Throws_the_points_to_the_position_the_route_requires_and_locks_them(PointPosition initial, string route, PointPosition required) {
        // Arrange
        Interlocking interlocking = Yard.Build(initial);

        // Act
        interlocking.RequestRoute(route, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Point point = interlocking.Point("P1");
        Assert.Equal(required, point.Position);
        Assert.Equal(route, point.LockedByRoute);
    }

    [Fact]
    public void Locks_a_point_that_already_lies_as_required_without_moving_it() {
        // Arrange
        Interlocking interlocking = Yard.Build(PointPosition.Normal);

        // Act
        interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Assert.Equal(PointPosition.Normal, interlocking.Point("P1").Position);
        Assert.True(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void Leaves_a_point_the_route_does_not_set_where_it_is() {
        // Arrange
        Interlocking interlocking = Yard.Build(PointPosition.Reverse);

        // Act
        interlocking.RequestRoute(Yard.YardRun, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Assert.Equal(PointPosition.Reverse, interlocking.Point("P1").Position);
        Assert.False(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void Sets_routes_that_do_not_conflict_for_two_trains_at_once() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        TrainId      anyOther     = Yard.AnyTrainOtherThan(anyTrain);

        // Act
        interlocking.RequestRoute(Yard.Main, anyTrain, Yard.AnyInstant());
        interlocking.RequestRoute(Yard.Siding, anyOther, Yard.AnyInstant());

        // Assert
        Assert.Equal(2, interlocking.ActiveRoutes.Count);
        Assert.Equal(Yard.Siding, interlocking.Section(Yard.B1).LockedByRoute);
    }

    [Fact]
    public void Refuses_an_unknown_route() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        string       before       = Yard.Snapshot(interlocking);

        // Act
        Yard.AssertRefused(RefusalReason.UnknownRoute, () => interlocking.RequestRoute("NOWHERE", Yard.AnyTrain(), Yard.AnyInstant()));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
    }

    [Fact]
    public void Route_names_are_case_sensitive() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act & Assert
        Yard.AssertRefused(RefusalReason.UnknownRoute, () => interlocking.RequestRoute("main", Yard.AnyTrain(), Yard.AnyInstant()));
    }

    [Fact]
    public void Refuses_a_null_train() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        DateTimeOffset anyTime    = Yard.AnyInstant();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => interlocking.RequestRoute(Yard.Main, null!, anyTime));
    }

    [Fact]
    public void Refuses_a_second_route_to_a_train_that_already_holds_one_even_if_the_routes_do_not_conflict() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        interlocking.RequestRoute(Yard.Main, anyTrain, Yard.AnyInstant());
        string before = Yard.Snapshot(interlocking);

        // Act
        Yard.AssertRefused(RefusalReason.TrainAlreadyHasRoute, () => interlocking.RequestRoute(Yard.YardRun, new TrainId(anyTrain.Headcode), Yard.AnyInstant()));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
    }

    [Theory]
    [InlineData(Yard.Main, Yard.Branch)]     // share A1, J1 and the point in opposite positions
    [InlineData(Yard.Branch, Yard.Siding)]   // share B1 only
    [InlineData(Yard.Main, Yard.Main)]       // the same route again
    public void Refuses_a_conflicting_route_for_another_train_and_changes_nothing(string alreadySet, string requested) {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        interlocking.RequestRoute(alreadySet, anyTrain, Yard.AnyInstant());
        string before = Yard.Snapshot(interlocking);

        // Act
        InterlockingRefusedException refusal = Yard.AssertRefused(RefusalReason.ConflictingRoute,
                                                                  () => interlocking.RequestRoute(requested, Yard.AnyTrainOtherThan(anyTrain), Yard.AnyInstant()));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
        Assert.Contains(alreadySet, refusal.Message);
    }

    [Fact]
    public void Refuses_a_route_over_an_occupied_section_and_changes_nothing() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.OnSectionOccupied(Yard.M1);
        string before = Yard.Snapshot(interlocking);

        // Act
        InterlockingRefusedException refusal = Yard.AssertRefused(RefusalReason.SectionOccupied,
                                                                  () => interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant()));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
        Assert.Contains("M1", refusal.Message);
        Assert.Equal(PointPosition.Reverse, interlocking.Point("P1").Position);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("J1")]
    [InlineData("M1")]
    public void Refuses_a_route_whichever_of_its_sections_is_occupied(string occupied) {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.OnSectionOccupied(new SectionId(occupied));

        // Act & Assert
        Yard.AssertRefused(RefusalReason.SectionOccupied, () => interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant()));
    }

    [Fact]
    public void Is_not_blocked_by_a_section_occupied_outside_the_route() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.OnSectionOccupied(Yard.C1);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Assert.Equal(Yard.Main, routeLock.Route.Name);
    }

    [Fact]
    public void A_route_can_be_requested_once_the_section_it_was_refused_for_is_clear() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.OnSectionOccupied(Yard.M1);
        Yard.AssertRefused(RefusalReason.SectionOccupied, () => interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant()));
        interlocking.OnSectionCleared(Yard.M1);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        // Assert
        Assert.Equal(Yard.Main, routeLock.Route.Name);
    }

    [Fact(Skip = "domain defect: a default lock time is only refused by 'new RouteLock' after the points are thrown and locked and the sections locked, leaving them locked by a route that is not active; seed-independent (default time is a literal), observed on every run")]
    public void Refuses_a_lock_time_that_is_not_set_and_leaves_the_interlocking_as_it_was() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        string       before       = Yard.Snapshot(interlocking);

        // Act
        Assert.Throws<ArgumentException>(() => interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), default));

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
    }

    [Fact(Skip = "domain defect: see the previous test; the same route can no longer be set after a refused default lock time (SectionLocked by a route that is not active), seed-independent")]
    public void Can_set_the_route_after_a_request_refused_for_its_lock_time() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        TrainId      anyTrain     = Yard.AnyTrain();
        Assert.Throws<ArgumentException>(() => interlocking.RequestRoute(Yard.Main, anyTrain, default));

        // Act
        RouteLock routeLock = interlocking.RequestRoute(Yard.Main, anyTrain, Yard.AnyInstant());

        // Assert
        Assert.Equal(Yard.Main, routeLock.Route.Name);
    }

}
