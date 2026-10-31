using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Regular;

public sealed class RouteSettingTests {

    private static readonly AnyDateTimeOffset AnyInstant = Any.DateTimeOffset().After(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact, Reproducible]
    public void Setting_a_route_throws_its_points_and_locks_them_with_its_sections() {
        // Arrange
        Junction junction = new();

        // Act
        junction.Interlocking.RequestRoute(Junction.BranchRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());

        // Assert
        Assert.Equal(PointPosition.Reverse, junction.Points.Position);
        Assert.Equal(Junction.BranchRoute, junction.Points.LockedByRoute);
        Assert.All([junction.Approach, junction.Middle, junction.Branch],
                   id => Assert.Equal(Junction.BranchRoute, junction.Interlocking.Section(id).LockedByRoute));
    }

    [Fact, Reproducible]
    public void The_route_lock_records_the_train_and_the_time_it_was_set() {
        // Arrange
        Junction       junction = new();
        TrainId        train    = new("2B47");
        DateTimeOffset setAt    = new(2026, 10, 30, 7, 15, 0, TimeSpan.Zero);

        // Act
        RouteLock routeLock = junction.Interlocking.RequestRoute(Junction.MainRoute, train, setAt);

        // Assert
        Assert.Equal(train, routeLock.Train);
        Assert.Equal(setAt, routeLock.LockedAt);
        Assert.Equal([junction.Approach, junction.Middle, junction.Main], routeLock.RemainingSections);
    }

    [Fact, Reproducible]
    public void An_unknown_route_is_refused() {
        // Arrange
        Junction junction = new();

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute("NOWHERE", Arbitrary.TrainId().Generate(), AnyInstant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.UnknownRoute, refusal.Reason);
    }

    [Fact, Reproducible]
    public void A_train_cannot_hold_two_routes_at_once() {
        // Arrange
        Junction junction = new();
        TrainId  anyTrain = Arbitrary.TrainId().Generate();
        junction.Interlocking.RequestRoute(Junction.MainRoute, anyTrain, AnyInstant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(Junction.UpRoute, anyTrain, AnyInstant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.TrainAlreadyHasRoute, refusal.Reason);
    }

    [Fact, Reproducible]
    public void A_conflicting_route_is_refused_and_leaves_the_points_where_the_first_route_set_them() {
        // Arrange
        Junction      junction  = new();
        List<TrainId> anyTrains = Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(Junction.MainRoute, anyTrains[0], AnyInstant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(Junction.BranchRoute, anyTrains[1], AnyInstant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
        Assert.Equal(PointPosition.Normal, junction.Points.Position);
        Assert.Equal(Junction.MainRoute, junction.Points.LockedByRoute);
        Assert.Null(junction.Interlocking.Section(junction.Branch).LockedByRoute);
    }

    [Fact, Reproducible]
    public void A_route_already_set_cannot_be_set_again_for_another_train() {
        // Arrange
        Junction      junction  = new();
        List<TrainId> anyTrains = Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(Junction.UpRoute, anyTrains[0], AnyInstant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(Junction.UpRoute, anyTrains[1], AnyInstant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    [Fact, Reproducible]
    public void A_route_over_an_occupied_section_is_refused() {
        // Arrange
        Junction junction = new();
        junction.Interlocking.OnSectionOccupied(junction.Main);

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.SectionOccupied, refusal.Reason);
        Assert.Null(junction.Points.LockedByRoute);
    }

    [Fact, Reproducible]
    public void Routes_with_nothing_in_common_are_set_side_by_side() {
        // Arrange
        Junction      junction  = new();
        List<TrainId> anyTrains = Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(Junction.BranchRoute, anyTrains[0], AnyInstant.Generate());

        // Act
        junction.Interlocking.RequestRoute(Junction.ShuntRoute, anyTrains[1], AnyInstant.Generate());

        // Assert
        Assert.Equal(2, junction.Interlocking.ActiveRoutes.Count);
    }

}
