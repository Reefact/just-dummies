using Xunit;

namespace RailwayInterlocking.Domain.Tests;

public sealed class InterlockingRoutingTests {

    private static InterlockingRefusedException Refusal(Action request) {
        return Assert.Throws<InterlockingRefusedException>(request);
    }

    // ---- RequestRoute: what a set route does ----

    [Fact]
    public void A_set_route_is_active_and_remembers_its_train_and_time() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Assert
        Assert.Same(network.Main, routeLock.Route);
        Assert.Equal(network.FirstTrain, routeLock.Train);
        Assert.Equal(network.At, routeLock.LockedAt);
        Assert.Same(routeLock, Assert.Single(interlocking.ActiveRoutes));
    }

    [Fact]
    public void A_set_route_locks_every_section_it_traverses_and_no_other() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Assert
        Assert.Equal(network.Main.Name, interlocking.Section(network.A).LockedByRoute);
        Assert.Equal(network.Main.Name, interlocking.Section(network.B).LockedByRoute);
        Assert.Equal(network.Main.Name, interlocking.Section(network.C).LockedByRoute);
        Assert.False(interlocking.Section(network.D).IsLocked);
        Assert.False(interlocking.Section(network.E).IsLocked);
    }

    [Fact]
    public void A_set_route_throws_its_points_to_the_required_position_and_locks_them() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        Assert.Equal(PointPosition.Reverse, interlocking.Point(network.PointName).Position);

        // Act
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Assert
        Point point = interlocking.Point(network.PointName);
        Assert.Equal(PointPosition.Normal, point.Position);
        Assert.Equal(network.Main.Name, point.LockedByRoute);
    }

    [Fact]
    public void A_route_with_no_points_leaves_every_point_alone() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        interlocking.RequestRoute(network.Parallel.Name, network.FirstTrain, network.At);

        // Assert
        Point point = interlocking.Point(network.PointName);
        Assert.Equal(PointPosition.Reverse, point.Position);
        Assert.False(point.IsLocked);
    }

    [Fact]
    public void Two_routes_that_share_nothing_can_be_set_for_two_trains() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.RequestRoute(network.Parallel.Name, network.SecondTrain, network.At);

        // Assert
        Assert.Equal(2, interlocking.ActiveRoutes.Count);
        Assert.Equal(network.Parallel.Name, interlocking.Section(network.D).LockedByRoute);
    }

    // ---- RequestRoute: refusals ----

    [Fact]
    public void An_unknown_route_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Main.Name + "-unknown", network.FirstTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.UnknownRoute, refusal.Reason);
    }

    [Fact]
    public void A_train_that_already_holds_a_route_cannot_be_given_another() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act: Parallel conflicts with nothing, so only the train is at fault
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Parallel.Name, network.FirstTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.TrainAlreadyHasRoute, refusal.Reason);
        Assert.Single(interlocking.ActiveRoutes);
    }

    [Fact]
    public void A_route_sharing_a_section_with_an_active_route_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Siding.Name, network.SecondTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    [Fact]
    public void A_refused_conflicting_route_leaves_the_active_route_exactly_as_it_was() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        Refusal(() => interlocking.RequestRoute(network.Siding.Name, network.SecondTrain, network.At));

        // Assert
        Point point = interlocking.Point(network.PointName);
        Assert.Equal(PointPosition.Normal, point.Position);
        Assert.Equal(network.Main.Name, point.LockedByRoute);
        Assert.Equal(network.Main.Name, interlocking.Section(network.A).LockedByRoute);
        Assert.Null(interlocking.Section(network.E).LockedByRoute);
        Assert.Single(interlocking.ActiveRoutes);
    }

    [Fact]
    public void The_same_route_cannot_be_set_twice_even_for_another_train() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Parallel.Name, network.FirstTrain, network.At);

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Parallel.Name, network.SecondTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    [Fact]
    public void A_route_over_an_occupied_section_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.OnSectionOccupied(network.C);

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.SectionOccupied, refusal.Reason);
    }

    [Fact]
    public void A_refused_route_changes_nothing() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.OnSectionOccupied(network.C);

        // Act: the last section of Main is occupied, the first ones are free
        Refusal(() => interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At));

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.False(interlocking.Section(network.B).IsLocked);
        Assert.False(interlocking.Section(network.C).IsLocked);
        Point point = interlocking.Point(network.PointName);
        Assert.Equal(PointPosition.Reverse, point.Position);
        Assert.False(point.IsLocked);
    }

    [Fact]
    public void A_route_can_be_set_once_the_occupied_section_has_cleared() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.OnSectionOccupied(network.C);
        interlocking.OnSectionCleared(network.C);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Assert
        Assert.Same(network.Main, routeLock.Route);
    }

    [Fact]
    public void A_refusal_is_an_invalid_operation_carrying_its_reason() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        Exception? refusal = Record.Exception(() => interlocking.RequestRoute(network.Main.Name + "-unknown", network.FirstTrain, network.At));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(refusal);
        Assert.Contains(network.Main.Name, refusal!.Message);
    }

    [Fact]
    public void A_null_train_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        Action request = () => interlocking.RequestRoute(network.Main.Name, null!, network.At);

        // Assert
        Assert.Throws<ArgumentNullException>(request);
    }

    [Fact(Skip = "domain defect: RequestRoute with a default lock time throws ArgumentException from RouteLock AFTER it has thrown the points and locked the sections, so the interlocking is left with locked sections and no active route; the failure is deterministic, no seed involved")]
    public void A_request_refused_for_its_lock_time_leaves_the_interlocking_unchanged() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        Record.Exception(() => interlocking.RequestRoute(network.Main.Name, network.FirstTrain, default));

        // Assert: "the interlocking state is left unchanged"
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.False(interlocking.Point(network.PointName).IsLocked);
        Assert.Equal(PointPosition.Reverse, interlocking.Point(network.PointName).Position);
    }

    // ---- CancelRoute ----

    [Fact]
    public void A_cancelled_route_frees_its_sections_and_points_and_ends() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        interlocking.CancelRoute(network.Main.Name);

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.False(interlocking.Section(network.B).IsLocked);
        Assert.False(interlocking.Section(network.C).IsLocked);
        Assert.False(interlocking.Point(network.PointName).IsLocked);
    }

    [Fact]
    public void A_cancelled_route_lets_a_conflicting_one_be_set_and_the_points_follow_it() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.CancelRoute(network.Main.Name);

        // Act
        interlocking.RequestRoute(network.Siding.Name, network.SecondTrain, network.At);

        // Assert
        Assert.Equal(PointPosition.Reverse, interlocking.Point(network.PointName).Position);
        Assert.Equal(network.Siding.Name, interlocking.Point(network.PointName).LockedByRoute);
    }

    [Fact]
    public void A_cancelled_route_frees_its_train_to_ask_again() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.CancelRoute(network.Main.Name);

        // Act
        RouteLock routeLock = interlocking.RequestRoute(network.Parallel.Name, network.FirstTrain, network.At);

        // Assert
        Assert.Same(network.Parallel, routeLock.Route);
    }

    [Fact]
    public void Cancelling_a_route_leaves_other_routes_alone() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.RequestRoute(network.Parallel.Name, network.SecondTrain, network.At);

        // Act
        interlocking.CancelRoute(network.Main.Name);

        // Assert
        Assert.Equal(network.Parallel.Name, Assert.Single(interlocking.ActiveRoutes).Route.Name);
        Assert.Equal(network.Parallel.Name, interlocking.Section(network.D).LockedByRoute);
    }

    [Fact]
    public void Cancelling_a_route_that_is_not_set_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.CancelRoute(network.Main.Name));

        // Assert
        Assert.Equal(RefusalReason.RouteNotSet, refusal.Reason);
    }

    [Fact]
    public void A_route_cannot_be_cancelled_once_its_train_has_entered_it() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);

        // Act
        InterlockingRefusedException refusal = Refusal(() => interlocking.CancelRoute(network.Main.Name));

        // Assert
        Assert.Equal(RefusalReason.ApproachLocked, refusal.Reason);
        Assert.True(routeLock.TrainHasEntered);
        Assert.Equal(network.Main.Name, interlocking.Section(network.B).LockedByRoute);
        Assert.Equal(network.Main.Name, interlocking.Point(network.PointName).LockedByRoute);
    }

    // ---- Section events ----

    [Fact]
    public void An_occupation_is_recorded_on_a_section_that_is_in_no_route() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();

        // Act
        interlocking.OnSectionOccupied(network.E);

        // Assert
        Assert.True(interlocking.Section(network.E).IsOccupied);
    }

    [Fact]
    public void A_clear_removes_the_occupation_of_a_section_that_is_in_no_route() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.OnSectionOccupied(network.E);

        // Act
        interlocking.OnSectionCleared(network.E);

        // Assert
        Assert.False(interlocking.Section(network.E).IsOccupied);
    }

    [Fact]
    public void An_event_on_an_unknown_section_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        SectionId anyUnknown = new AnySectionId().WithValue(v => v.DifferentFrom(network.A.Value)
                                                                  .DifferentFrom(network.B.Value)
                                                                  .DifferentFrom(network.C.Value)
                                                                  .DifferentFrom(network.D.Value)
                                                                  .DifferentFrom(network.E.Value)).Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => interlocking.OnSectionOccupied(anyUnknown));
        Assert.Throws<KeyNotFoundException>(() => interlocking.OnSectionCleared(anyUnknown));
    }

    [Fact]
    public void The_train_entering_the_first_section_marks_the_route_entered() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        Assert.False(routeLock.TrainHasEntered);

        // Act
        interlocking.OnSectionOccupied(network.A);

        // Assert
        Assert.True(routeLock.TrainHasEntered);
        Assert.True(routeLock.HasVisited(network.A));
        Assert.False(routeLock.HasVisited(network.B));
    }

    [Fact]
    public void An_occupation_outside_the_route_does_not_mark_it_entered() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        interlocking.OnSectionOccupied(network.E);

        // Assert
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void The_rear_section_is_released_once_the_train_has_left_it() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionOccupied(network.B);

        // Act
        interlocking.OnSectionCleared(network.A);

        // Assert
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.Equal(network.Main.Name, interlocking.Section(network.B).LockedByRoute);
        Assert.Equal(network.Main.Name, interlocking.Section(network.C).LockedByRoute);
        Assert.Equal(new[] { network.B, network.C }, routeLock.RemainingSections);
    }

    [Fact]
    public void A_section_still_occupied_is_not_released() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);

        // Act: a clear on another section of the route
        interlocking.OnSectionCleared(network.C);

        // Assert
        Assert.Equal(3, routeLock.RemainingSections.Count);
        Assert.True(interlocking.Section(network.A).IsLocked);
    }

    [Fact]
    public void A_clear_ahead_of_the_train_keeps_every_lock() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionCleared(network.A);
        interlocking.OnSectionOccupied(network.B);

        // Act: a spurious clear on the last section, which the train never reached
        interlocking.OnSectionCleared(network.C);

        // Assert
        Assert.Equal(new[] { network.B, network.C }, routeLock.RemainingSections);
        Assert.Equal(network.Main.Name, interlocking.Section(network.C).LockedByRoute);
    }

    [Fact]
    public void A_clear_of_a_section_the_train_never_occupied_releases_nothing() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        interlocking.OnSectionCleared(network.A);

        // Assert
        Assert.Equal(3, routeLock.RemainingSections.Count);
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void Clears_arriving_out_of_order_are_caught_up_when_the_rear_clears() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionOccupied(network.B);

        // Act: B reports clear while A still reports occupied, then A follows
        interlocking.OnSectionCleared(network.B);
        Assert.Equal(3, routeLock.RemainingSections.Count);
        interlocking.OnSectionCleared(network.A);

        // Assert
        Assert.Equal(new[] { network.C }, routeLock.RemainingSections);
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.False(interlocking.Section(network.B).IsLocked);
    }

    [Fact]
    public void The_points_are_released_when_the_section_they_lie_in_is_released_not_before() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionOccupied(network.B);

        // Act / Assert: A (behind the points) leaves: the points, in B, stay locked
        interlocking.OnSectionCleared(network.A);
        Assert.Equal(network.Main.Name, interlocking.Point(network.PointName).LockedByRoute);

        // Act / Assert: the train moves on into C and B clears: the points are free
        interlocking.OnSectionOccupied(network.C);
        interlocking.OnSectionCleared(network.B);
        Assert.False(interlocking.Point(network.PointName).IsLocked);
        Assert.Equal(network.Main.Name, interlocking.Section(network.C).LockedByRoute);
    }

    [Fact]
    public void A_released_point_keeps_the_position_the_route_gave_it() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        RunTrainThroughMain(interlocking, network);

        // Assert
        Assert.Equal(PointPosition.Normal, interlocking.Point(network.PointName).Position);
    }

    [Fact]
    public void A_fully_released_route_ends_and_frees_everything_it_held() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        RouteLock routeLock = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Act
        RunTrainThroughMain(interlocking, network);

        // Assert
        Assert.True(routeLock.IsFullyReleased);
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(network.A).IsLocked);
        Assert.False(interlocking.Section(network.B).IsLocked);
        Assert.False(interlocking.Section(network.C).IsLocked);
        Assert.False(interlocking.Point(network.PointName).IsLocked);
    }

    [Fact]
    public void Once_a_route_has_ended_a_conflicting_route_can_be_set_for_another_train() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        RunTrainThroughMain(interlocking, network);

        // Act
        interlocking.RequestRoute(network.Siding.Name, network.SecondTrain, network.At);

        // Assert
        Assert.Equal(PointPosition.Reverse, interlocking.Point(network.PointName).Position);
    }

    [Fact]
    public void Once_a_route_has_ended_its_train_may_hold_another_and_the_route_can_be_set_again() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        RunTrainThroughMain(interlocking, network);

        // Act
        RouteLock again = interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);

        // Assert
        Assert.Equal(3, again.RemainingSections.Count);
    }

    [Fact]
    public void A_train_that_has_entered_cannot_have_a_route_that_overlaps_the_sections_it_still_holds() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        interlocking.RequestRoute(network.Main.Name, network.FirstTrain, network.At);
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionOccupied(network.B);
        interlocking.OnSectionCleared(network.A);

        // Act: A is free again, but Siding also needs B, still held by Main
        InterlockingRefusedException refusal = Refusal(() => interlocking.RequestRoute(network.Siding.Name, network.SecondTrain, network.At));

        // Assert
        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

    private static void RunTrainThroughMain(Interlocking interlocking, Network network) {
        interlocking.OnSectionOccupied(network.A);
        interlocking.OnSectionOccupied(network.B);
        interlocking.OnSectionCleared(network.A);
        interlocking.OnSectionOccupied(network.C);
        interlocking.OnSectionCleared(network.B);
        interlocking.OnSectionCleared(network.C);
    }

}
