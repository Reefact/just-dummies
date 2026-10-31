using JustDummies;

using RailwayInterlocking.Domain;
using RailwayInterlocking.Tests.Expert.Arbitrary;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

public sealed class RouteSettingTests {

    private static readonly AnyJunctionLayout AnyJunction = new();

    [Fact]
    public void Setting_a_route_locks_every_section_it_traverses_into_that_route() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        TrainId        anyTrain    = Any.TrainId().Generate();

        // Act
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, anyTrain, Recipes.Instant.Generate());

        // Assert
        Assert.All([anyJunction.Approach, anyJunction.Junction, anyJunction.Main],
                   id => Assert.Equal(anyJunction.MainRoute, anyJunction.SectionAt(id).LockedByRoute));
        Assert.False(anyJunction.SectionAt(anyJunction.Branch).IsLocked);
    }

    [Fact]
    public void Setting_the_diverging_route_throws_the_point_reverse_and_locks_it() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();

        // Act
        anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, Any.TrainId().Generate(), Recipes.Instant.Generate());

        // Assert
        Assert.Equal(PointPosition.Reverse, anyJunction.Point.Position);
        Assert.Equal(anyJunction.BranchRoute, anyJunction.Point.LockedByRoute);
    }

    [Fact]
    public void The_route_lock_records_the_train_the_route_was_set_for_and_when() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        TrainId        train       = new("2C47");                                                  // the assertion is about these two,
        DateTimeOffset requestedAt = new(2026, 10, 30, 7, 42, 0, TimeSpan.FromHours(1));           // so they are written, not drawn

        // Act
        RouteLock routeLock = anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, train, requestedAt);

        // Assert
        Assert.Equal(train, routeLock.Train);
        Assert.Equal(requestedAt, routeLock.LockedAt);
        Assert.Equal([anyJunction.Approach, anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void A_route_missing_from_the_control_tables_is_refused() {
        // Arrange
        JunctionLayout anyJunction  = AnyJunction.Generate();
        string         anyUnknownRoute = Recipes.RouteName.Generate() + "-UNKNOWN";   // no route name in the tables has a dash

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => anyJunction.Interlocking.RequestRoute(anyUnknownRoute, Any.TrainId().Generate(), Recipes.Instant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.UnknownRoute, refusal.Reason);
    }

    [Fact]
    public void A_conflicting_route_is_refused_and_leaves_the_junction_as_it_was() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        (TrainId anyTrain, TrainId anyOtherTrain) = Recipes.TwoTrains();
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, anyTrain, Recipes.Instant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, anyOtherTrain, Recipes.Instant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
        Assert.Equal(PointPosition.Normal, anyJunction.Point.Position);
        Assert.False(anyJunction.SectionAt(anyJunction.Branch).IsLocked);
        Assert.Single(anyJunction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void A_route_already_set_cannot_be_set_again_for_another_train() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        (TrainId anyTrain, TrainId anyOtherTrain) = Recipes.TwoTrains();
        anyJunction.Interlocking.RequestRoute(anyJunction.SidingRoute, anyTrain, Recipes.Instant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => anyJunction.Interlocking.RequestRoute(anyJunction.SidingRoute, anyOtherTrain, Recipes.Instant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    [Fact]
    public void A_train_holds_at_most_one_route() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        TrainId        anyTrain    = Any.TrainId().Generate();
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, anyTrain, Recipes.Instant.Generate());

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => anyJunction.Interlocking.RequestRoute(anyJunction.SidingRoute, anyTrain, Recipes.Instant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.TrainAlreadyHasRoute, refusal.Reason);
    }

    [Fact]
    public void Independent_routes_are_set_together_for_different_trains() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        (TrainId anyTrain, TrainId anyOtherTrain) = Recipes.TwoTrains();
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, anyTrain, Recipes.Instant.Generate());

        // Act
        anyJunction.Interlocking.RequestRoute(anyJunction.SidingRoute, anyOtherTrain, Recipes.Instant.Generate());

        // Assert
        Assert.Equal(2, anyJunction.Interlocking.ActiveRoutes.Count);
    }

    [Fact]
    public void A_route_is_not_set_over_an_occupied_section() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Junction);   // e.g. a train standing over the points

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, Any.TrainId().Generate(), Recipes.Instant.Generate()));

        // Assert
        Assert.Equal(RefusalReason.SectionOccupied, refusal.Reason);
        Assert.False(anyJunction.Point.IsLocked);
        Assert.Empty(anyJunction.Interlocking.ActiveRoutes);
    }

}
