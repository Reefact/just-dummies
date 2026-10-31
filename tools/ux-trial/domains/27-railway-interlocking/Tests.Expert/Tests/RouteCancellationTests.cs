using JustDummies;

using RailwayInterlocking.Domain;
using RailwayInterlocking.Tests.Expert.Arbitrary;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

public sealed class RouteCancellationTests {

    private static readonly AnyJunctionLayout AnyJunction = new();

    [Fact]
    public void Cancelling_a_route_before_the_train_enters_frees_its_sections_and_its_point() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, Any.TrainId().Generate(), Recipes.Instant.Generate());

        // Act
        anyJunction.Interlocking.CancelRoute(anyJunction.BranchRoute);

        // Assert
        Assert.Empty(anyJunction.Interlocking.ActiveRoutes);
        Assert.All([anyJunction.Approach, anyJunction.Junction, anyJunction.Branch], id => Assert.False(anyJunction.SectionAt(id).IsLocked));
        Assert.False(anyJunction.Point.IsLocked);
    }

    [Fact]
    public void Once_cancelled_the_conflicting_route_can_be_set() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        (TrainId anyTrain, TrainId anyOtherTrain) = Recipes.TwoTrains();
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, anyTrain, Recipes.Instant.Generate());
        anyJunction.Interlocking.CancelRoute(anyJunction.MainRoute);

        // Act
        anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, anyOtherTrain, Recipes.Instant.Generate());

        // Assert
        Assert.Equal(PointPosition.Reverse, anyJunction.Point.Position);
    }

    [Fact]
    public void A_route_the_train_has_entered_cannot_be_cancelled() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();
        anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, Any.TrainId().Generate(), Recipes.Instant.Generate());
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(() => anyJunction.Interlocking.CancelRoute(anyJunction.MainRoute));

        // Assert
        Assert.Equal(RefusalReason.ApproachLocked, refusal.Reason);
        Assert.Equal(anyJunction.MainRoute, anyJunction.SectionAt(anyJunction.Main).LockedByRoute);
    }

    [Fact]
    public void Cancelling_a_route_that_is_not_set_is_refused() {
        // Arrange
        JunctionLayout anyJunction = AnyJunction.Generate();

        // Act
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(() => anyJunction.Interlocking.CancelRoute(anyJunction.SidingRoute));

        // Assert
        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

}
