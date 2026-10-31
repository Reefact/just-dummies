using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Discovery.Tests;

[Reproducible]
public sealed class RouteCancellationAndReleaseTests {

    [Fact]
    public void Cancelling_a_route_before_the_train_enters_frees_its_sections_and_points() {
        // Arrange
        Junction junction = new Junction();
        junction.Interlocking.RequestRoute(junction.ToBranch.Name, Any.TrainId().Generate(), Junction.AnyTime());

        // Act
        junction.Interlocking.CancelRoute(junction.ToBranch.Name);

        // Assert
        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.All(junction.ToBranch.Sections, id => Assert.False(junction.Interlocking.Section(id).IsLocked));
        Assert.False(junction.Interlocking.Point(junction.PointName).IsLocked);
    }

    [Fact]
    public void Cancelling_a_route_that_is_not_set_is_refused() {
        // Arrange
        Junction junction = new Junction();

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.CancelRoute(junction.ToMain.Name));

        // Assert
        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

    [Fact]
    public void A_route_cannot_be_cancelled_once_its_train_has_entered() {
        // Arrange
        Junction junction = new Junction();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.CancelRoute(junction.ToMain.Name));

        // Assert
        Assert.Equal(RefusalReason.ApproachLocked, refusal.Reason);
        Assert.Single(junction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void Sections_release_from_the_rear_as_the_train_leaves_them() {
        // Arrange
        Junction  junction  = new Junction();
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Middle);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Approach);

        // Assert
        Assert.False(junction.Interlocking.Section(junction.Approach).IsLocked);
        Assert.Equal([junction.Middle, junction.Main], routeLock.RemainingSections);
        Assert.True(junction.Interlocking.Point(junction.PointName).IsLocked);
    }

    [Fact]
    public void A_clear_ahead_of_the_train_keeps_the_section_locked() {
        // Arrange
        Junction  junction  = new Junction();
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Main);

        // Assert
        Assert.Equal(junction.ToMain.Sections, routeLock.RemainingSections);
        Assert.True(junction.Interlocking.Section(junction.Main).IsLocked);
    }

    [Fact]
    public void A_clear_received_out_of_order_is_caught_up_when_the_rear_clears() {
        // Arrange
        Junction  junction  = new Junction();
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Middle);
        junction.Interlocking.OnSectionOccupied(junction.Main);
        junction.Interlocking.OnSectionCleared(junction.Middle);   // arrives before the approach's clear

        // Act
        junction.Interlocking.OnSectionCleared(junction.Approach);

        // Assert
        Assert.Equal([junction.Main], routeLock.RemainingSections);
        Assert.False(junction.Interlocking.Section(junction.Middle).IsLocked);
    }

    [Fact]
    public void The_points_unlock_once_the_section_holding_them_is_released() {
        // Arrange
        Junction junction = new Junction();
        junction.Interlocking.RequestRoute(junction.ToBranch.Name, Any.TrainId().Generate(), Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Middle);
        junction.Interlocking.OnSectionOccupied(junction.Branch);
        junction.Interlocking.OnSectionCleared(junction.Approach);

        // Act
        junction.Interlocking.OnSectionCleared(junction.Middle);

        // Assert
        Point point = junction.Interlocking.Point(junction.PointName);
        Assert.False(point.IsLocked);
        Assert.Equal(PointPosition.Reverse, point.Position);
    }

    [Fact]
    public void A_fully_released_route_ends_and_lets_a_conflicting_route_be_set() {
        // Arrange
        Junction      junction     = new Junction();
        List<TrainId> anyTwoTrains = Any.ListOf(Any.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTwoTrains[0], Junction.AnyTime());
        foreach (SectionId id in junction.ToMain.Sections) {
            junction.Interlocking.OnSectionOccupied(id);
            junction.Interlocking.OnSectionCleared(id);
        }

        // Act
        RouteLock branchLock = junction.Interlocking.RequestRoute(junction.ToBranch.Name, anyTwoTrains[1], Junction.AnyTime());

        // Assert
        Assert.Same(junction.ToBranch, Assert.Single(junction.Interlocking.ActiveRoutes).Route);
        Assert.Equal(PointPosition.Reverse, junction.Interlocking.Point(junction.PointName).Position);
        Assert.False(branchLock.TrainHasEntered);
    }

    [Fact]
    public void A_train_whose_route_has_released_can_set_another() {
        // Arrange
        Junction junction = new Junction();
        TrainId  anyTrain = Any.TrainId().Generate();
        junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTrain, Junction.AnyTime());
        junction.Interlocking.OnSectionOccupied(junction.Siding);
        junction.Interlocking.OnSectionCleared(junction.Siding);

        // Act
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTrain, Junction.AnyTime());

        // Assert
        Assert.Equal(anyTrain, routeLock.Train);
    }

}
