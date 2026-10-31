using JustDummies;

using RailwayInterlocking.Domain;
using RailwayInterlocking.Tests.Expert.Arbitrary;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

/// <summary>
///     Track-circuit events arrive asynchronously. Sections are released one by one behind the train,
///     from the rear, and never ahead of it.
/// </summary>
public sealed class SequentialReleaseTests {

    private static readonly AnyJunctionLayout AnyJunction = new();

    [Fact]
    public void A_section_the_train_has_passed_through_and_left_is_released() {
        // Arrange
        (JunctionLayout anyJunction, RouteLock routeLock) = MainRouteSet();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Junction);

        // Act
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Approach);

        // Assert
        Assert.False(anyJunction.SectionAt(anyJunction.Approach).IsLocked);
        Assert.Equal([anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
    }

    [Fact]
    public void A_clear_reported_ahead_of_the_train_keeps_the_section_locked() {
        // Arrange
        (JunctionLayout anyJunction, RouteLock routeLock) = MainRouteSet();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);

        // Act
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Main);   // a spurious clear: the train never got there

        // Assert
        Assert.Equal(anyJunction.MainRoute, anyJunction.SectionAt(anyJunction.Main).LockedByRoute);
        Assert.Equal([anyJunction.Approach, anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
    }

    [Fact]
    public void A_clear_arriving_before_the_one_behind_it_waits_for_the_rear_then_both_release() {
        // Arrange
        (JunctionLayout anyJunction, RouteLock routeLock) = MainRouteSet();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Junction);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Main);
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Junction);   // out of order: the rear is still occupied

        // Act
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Approach);

        // Assert
        Assert.Equal([anyJunction.Main], routeLock.RemainingSections);
        Assert.False(anyJunction.SectionAt(anyJunction.Junction).IsLocked);
    }

    [Fact]
    public void A_section_still_occupied_behind_the_train_is_not_released() {
        // Arrange
        (JunctionLayout anyJunction, RouteLock routeLock) = MainRouteSet();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Junction);

        // Act
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Junction);   // the tail is still on the approach

        // Assert
        Assert.Equal([anyJunction.Approach, anyJunction.Junction, anyJunction.Main], routeLock.RemainingSections);
    }

    [Fact]
    public void The_point_is_freed_as_soon_as_the_train_has_cleared_the_section_it_lies_in() {
        // Arrange
        (JunctionLayout anyJunction, _) = MainRouteSet();
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Approach);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Junction);
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Approach);
        anyJunction.Interlocking.OnSectionOccupied(anyJunction.Main);

        // Act
        anyJunction.Interlocking.OnSectionCleared(anyJunction.Junction);

        // Assert
        Assert.False(anyJunction.Point.IsLocked);
        Assert.Single(anyJunction.Interlocking.ActiveRoutes);   // the train is still on Main
    }

    [Fact]
    public void A_route_ends_when_the_train_has_cleared_its_last_section() {
        // Arrange
        (JunctionLayout anyJunction, RouteLock routeLock) = MainRouteSet();
        foreach (SectionId section in new[] { anyJunction.Approach, anyJunction.Junction, anyJunction.Main }) {
            anyJunction.Interlocking.OnSectionOccupied(section);
            anyJunction.Interlocking.OnSectionCleared(section);
        }

        // Act
        bool released = routeLock.IsFullyReleased;

        // Assert
        Assert.True(released);
        Assert.Empty(anyJunction.Interlocking.ActiveRoutes);
    }

    [Fact]
    public void Once_the_first_train_has_passed_the_conflicting_route_can_be_set_for_the_next() {
        // Arrange
        (JunctionLayout anyJunction, _) = MainRouteSet();
        foreach (SectionId section in new[] { anyJunction.Approach, anyJunction.Junction, anyJunction.Main }) {
            anyJunction.Interlocking.OnSectionOccupied(section);
            anyJunction.Interlocking.OnSectionCleared(section);
        }

        // Act
        RouteLock branch = anyJunction.Interlocking.RequestRoute(anyJunction.BranchRoute, Any.TrainId().Generate(), Recipes.Instant.Generate());

        // Assert
        Assert.Equal(anyJunction.BranchRoute, branch.Route.Name);
        Assert.Equal(PointPosition.Reverse, anyJunction.Point.Position);
    }

    private static (JunctionLayout, RouteLock) MainRouteSet() {
        JunctionLayout anyJunction = AnyJunction.Generate();
        RouteLock      routeLock   = anyJunction.Interlocking.RequestRoute(anyJunction.MainRoute, Any.TrainId().Generate(), Recipes.Instant.Generate());

        return (anyJunction, routeLock);
    }

}
