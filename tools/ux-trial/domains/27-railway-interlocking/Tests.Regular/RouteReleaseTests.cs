using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Regular;

public sealed class RouteReleaseTests {

    private static readonly AnyDateTimeOffset AnyInstant = Any.DateTimeOffset().After(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact, Reproducible]
    public void Cancelling_a_route_before_the_train_enters_frees_its_sections_and_points() {
        // Arrange
        Junction junction = new();
        junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());

        // Act
        junction.Interlocking.CancelRoute(Junction.MainRoute);

        // Assert
        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.Null(junction.Points.LockedByRoute);
        Assert.All([junction.Approach, junction.Middle, junction.Main],
                   id => Assert.Null(junction.Interlocking.Section(id).LockedByRoute));
    }

    [Fact, Reproducible]
    public void Once_cancelled_a_conflicting_route_can_be_set_and_moves_the_points() {
        // Arrange
        Junction      junction  = new();
        List<TrainId> anyTrains = Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(Junction.MainRoute, anyTrains[0], AnyInstant.Generate());
        junction.Interlocking.CancelRoute(Junction.MainRoute);

        // Act
        junction.Interlocking.RequestRoute(Junction.BranchRoute, anyTrains[1], AnyInstant.Generate());

        // Assert
        Assert.Equal(PointPosition.Reverse, junction.Points.Position);
    }

    [Fact, Reproducible]
    public void A_route_that_is_not_set_cannot_be_cancelled() {
        // Arrange
        Junction junction = new();

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.CancelRoute(Junction.MainRoute));

        // Assert
        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

    [Fact, Reproducible]
    public void A_route_cannot_be_cancelled_once_its_train_has_entered() {
        // Arrange
        Junction junction = new();
        junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.CancelRoute(Junction.MainRoute));

        // Assert
        Assert.Equal(RefusalReason.ApproachLocked, refusal.Reason);
        Assert.Single(junction.Interlocking.ActiveRoutes);
    }

    [Fact, Reproducible]
    public void Sections_release_one_by_one_behind_the_train() {
        // Arrange
        Junction junction = new();
        RouteLock routeLock = junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Middle);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Approach);

        // Assert
        Assert.Equal([junction.Middle, junction.Main], routeLock.RemainingSections);
        Assert.Null(junction.Interlocking.Section(junction.Approach).LockedByRoute);
        Assert.Equal(Junction.MainRoute, junction.Points.LockedByRoute);
    }

    [Fact, Reproducible]
    public void The_points_are_freed_as_soon_as_the_section_they_lie_in_is_released() {
        // Arrange
        Junction  junction  = new();
        RouteLock routeLock = junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());

        // Act
        junction.Run(junction.Approach, junction.Middle);
        junction.Interlocking.OnSectionOccupied(junction.Main);

        // Assert
        Assert.Null(junction.Points.LockedByRoute);
        Assert.Equal([junction.Main], routeLock.RemainingSections);
    }

    [Fact, Reproducible]
    public void A_route_ends_once_the_train_has_cleared_its_last_section() {
        // Arrange
        Junction junction = new();
        junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());

        // Act
        junction.Run(junction.Approach, junction.Middle, junction.Main);

        // Assert
        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.Null(junction.Interlocking.Section(junction.Main).LockedByRoute);
    }

    [Fact, Reproducible]
    public void A_clear_arriving_before_the_one_behind_it_is_caught_up_when_the_rear_clears() {
        // Arrange
        Junction  junction  = new();
        RouteLock routeLock = junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Middle);
        junction.Interlocking.OnSectionOccupied(junction.Main);
        junction.Interlocking.OnSectionCleared(junction.Middle);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Approach);

        // Assert
        Assert.Equal([junction.Main], routeLock.RemainingSections);
    }

    [Fact, Reproducible]
    public void A_clear_ahead_of_the_train_keeps_the_route_locked() {
        // Arrange
        Junction  junction  = new();
        RouteLock routeLock = junction.Interlocking.RequestRoute(Junction.MainRoute, Arbitrary.TrainId().Generate(), AnyInstant.Generate());
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Main);

        // Assert
        Assert.Equal([junction.Approach, junction.Middle, junction.Main], routeLock.RemainingSections);
        Assert.Equal(Junction.MainRoute, junction.Points.LockedByRoute);
    }

    [Fact, Reproducible]
    public void A_conflicting_route_can_be_set_once_the_first_train_has_passed_through() {
        // Arrange
        Junction      junction  = new();
        List<TrainId> anyTrains = Any.ListOf(Arbitrary.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(Junction.MainRoute, anyTrains[0], AnyInstant.Generate());
        junction.Run(junction.Approach, junction.Middle, junction.Main);

        // Act
        junction.Interlocking.RequestRoute(Junction.BranchRoute, anyTrains[1], AnyInstant.Generate());

        // Assert
        Assert.Equal(PointPosition.Reverse, junction.Points.Position);
    }

}
