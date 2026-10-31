using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Discovery.Tests;

[Reproducible]
public sealed class RouteSettingTests {

    [Fact]
    public void Setting_a_free_route_locks_it_for_the_requesting_train() {
        // Arrange
        Junction       junction    = new Junction();
        TrainId        anyTrain    = Any.TrainId().Generate();
        DateTimeOffset anyLockedAt = Junction.AnyTime();

        // Act
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTrain, anyLockedAt);

        // Assert
        Assert.Same(junction.ToMain, routeLock.Route);
        Assert.Equal(anyTrain, routeLock.Train);
        Assert.False(routeLock.TrainHasEntered);
        Assert.Single(junction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void Setting_a_route_locks_every_section_it_traverses_into_it() {
        // Arrange
        Junction junction = new Junction();

        // Act
        junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime());

        // Assert
        Assert.All(junction.ToMain.Sections, id => Assert.Equal(junction.ToMain.Name, junction.Interlocking.Section(id).LockedByRoute));
        Assert.False(junction.Interlocking.Section(junction.Branch).IsLocked);
    }

    [Fact]
    public void Setting_a_route_throws_its_points_to_the_required_position_and_locks_them() {
        // Arrange
        Junction junction = new Junction();   // the point starts Normal

        // Act
        junction.Interlocking.RequestRoute(junction.ToBranch.Name, Any.TrainId().Generate(), Junction.AnyTime());

        // Assert
        Point point = junction.Interlocking.Point(junction.PointName);
        Assert.Equal(PointPosition.Reverse, point.Position);
        Assert.Equal(junction.ToBranch.Name, point.LockedByRoute);
    }

    [Fact]
    public void An_unknown_route_is_refused() {
        // Arrange
        Junction junction        = new Junction();
        string   anyUnknownRoute = Any.String().NotBlank().Except(junction.ToMain.Name, junction.ToBranch.Name, junction.ToSiding.Name).Generate();

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(anyUnknownRoute, Any.TrainId().Generate(), Junction.AnyTime()));

        // Assert
        Assert.Equal(RefusalReason.UnknownRoute, refusal.Reason);
    }

    [Fact]
    public void A_train_that_already_holds_a_route_cannot_set_a_second_one() {
        // Arrange
        Junction junction = new Junction();
        TrainId  anyTrain = Any.TrainId().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTrain, Junction.AnyTime());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTrain, Junction.AnyTime()));

        // Assert
        Assert.Equal(RefusalReason.TrainAlreadyHasRoute, refusal.Reason);
    }

    [Fact]
    public void A_route_conflicting_with_an_active_one_is_refused_and_moves_nothing() {
        // Arrange
        Junction       junction       = new Junction();
        List<TrainId>  anyTwoTrains   = Any.ListOf(Any.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTwoTrains[0], Junction.AnyTime());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(junction.ToBranch.Name, anyTwoTrains[1], Junction.AnyTime()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
        Assert.Equal(PointPosition.Normal, junction.Interlocking.Point(junction.PointName).Position);
        Assert.False(junction.Interlocking.Section(junction.Branch).IsLocked);
        Assert.Single(junction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void The_same_route_cannot_be_set_twice() {
        // Arrange
        Junction      junction     = new Junction();
        List<TrainId> anyTwoTrains = Any.ListOf(Any.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTwoTrains[0], Junction.AnyTime());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTwoTrains[1], Junction.AnyTime()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    [Fact]
    public void Routes_that_share_nothing_can_be_set_together() {
        // Arrange
        Junction      junction     = new Junction();
        List<TrainId> anyTwoTrains = Any.ListOf(Any.TrainId()).WithCount(2).Distinct().Generate();
        junction.Interlocking.RequestRoute(junction.ToMain.Name, anyTwoTrains[0], Junction.AnyTime());

        // Act
        junction.Interlocking.RequestRoute(junction.ToSiding.Name, anyTwoTrains[1], Junction.AnyTime());

        // Assert
        Assert.Equal(2, junction.Interlocking.ActiveRoutes.Count);
    }

    [Fact]
    public void A_route_over_an_occupied_section_is_refused() {
        // Arrange
        Junction junction = new Junction();
        junction.Interlocking.OnSectionOccupied(junction.Main);

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Junction.AnyTime()));

        // Assert
        Assert.Equal(RefusalReason.SectionOccupied, refusal.Reason);
        Assert.Empty(junction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void Occupancy_outside_any_route_is_recorded_without_locking_anything() {
        // Arrange
        Junction junction = new Junction();

        // Act
        junction.Interlocking.OnSectionOccupied(junction.Siding);

        // Assert
        TrackSection siding = junction.Interlocking.Section(junction.Siding);
        Assert.True(siding.IsOccupied);
        Assert.False(siding.IsLocked);
    }

}
