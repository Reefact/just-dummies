using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class TrackCircuitEventTests {

    private static Interlocking WithMainSet(out RouteLock routeLock) {
        Interlocking interlocking = Yard.Build();
        routeLock = interlocking.RequestRoute(Yard.Main, Yard.AnyTrain(), Yard.AnyInstant());

        return interlocking;
    }

    [Fact]
    public void A_section_reported_occupied_is_occupied_whether_or_not_it_is_on_a_route() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act
        interlocking.OnSectionOccupied(Yard.C1);

        // Assert
        Assert.True(interlocking.Section(Yard.C1).IsOccupied);
    }

    [Fact]
    public void A_section_reported_clear_is_clear() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        interlocking.OnSectionOccupied(Yard.C1);

        // Act
        interlocking.OnSectionCleared(Yard.C1);

        // Assert
        Assert.False(interlocking.Section(Yard.C1).IsOccupied);
    }

    [Fact]
    public void Reporting_a_section_that_is_not_on_any_route_does_not_disturb_the_routes_set() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);
        string       before       = Yard.Snapshot(interlocking);

        // Act
        interlocking.OnSectionOccupied(Yard.C1);
        interlocking.OnSectionCleared(Yard.C1);

        // Assert
        Assert.Equal(before, Yard.Snapshot(interlocking));
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void Refuses_to_report_an_unknown_section() {
        // Arrange
        Interlocking interlocking = Yard.Build();
        SectionId    unknown      = new SectionId("Z9");

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => interlocking.OnSectionOccupied(unknown));
        Assert.Throws<KeyNotFoundException>(() => interlocking.OnSectionCleared(unknown));
    }

    [Fact]
    public void A_train_detected_on_its_route_has_entered_it_and_has_visited_that_section_only() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);

        // Act
        interlocking.OnSectionOccupied(Yard.A1);

        // Assert
        Assert.True(routeLock.TrainHasEntered);
        Assert.True(routeLock.HasVisited(Yard.A1));
        Assert.False(routeLock.HasVisited(Yard.J1));
    }

    [Fact]
    public void The_rear_section_is_released_when_the_train_has_left_it_and_the_rest_stays_locked() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);

        // Act
        interlocking.OnSectionCleared(Yard.A1);

        // Assert
        Assert.False(interlocking.Section(Yard.A1).IsLocked);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.J1).LockedByRoute);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.M1).LockedByRoute);
        Assert.Equal(new[] { Yard.J1, Yard.M1 }, routeLock.RemainingSections);
    }

    [Fact]
    public void The_points_stay_locked_while_the_section_they_lie_in_is_still_held() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock _);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);

        // Act
        interlocking.OnSectionCleared(Yard.A1);

        // Assert
        Assert.True(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void The_points_are_freed_once_the_section_they_lie_in_is_released() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock _);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionCleared(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);

        // Act
        interlocking.OnSectionCleared(Yard.J1);

        // Assert
        Assert.False(interlocking.Point("P1").IsLocked);
        Assert.Equal(PointPosition.Normal, interlocking.Point("P1").Position);
    }

    [Fact]
    public void A_train_running_the_whole_route_releases_it_section_by_section_and_ends_the_route() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);

        // Act
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);
        interlocking.OnSectionCleared(Yard.A1);
        interlocking.OnSectionOccupied(Yard.M1);
        interlocking.OnSectionCleared(Yard.J1);
        interlocking.OnSectionCleared(Yard.M1);

        // Assert
        Assert.True(routeLock.IsFullyReleased);
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(Yard.A1).IsLocked);
        Assert.False(interlocking.Section(Yard.J1).IsLocked);
        Assert.False(interlocking.Section(Yard.M1).IsLocked);
        Assert.False(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void A_train_whose_sections_clear_one_by_one_ahead_of_the_next_is_released_the_same_way() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);

        // Act
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionCleared(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);
        interlocking.OnSectionCleared(Yard.J1);
        interlocking.OnSectionOccupied(Yard.M1);
        interlocking.OnSectionCleared(Yard.M1);

        // Assert
        Assert.True(routeLock.IsFullyReleased);
        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Fact]
    public void A_clear_arriving_before_the_one_behind_it_releases_nothing_until_the_rear_clears_and_then_catches_up() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);

        // Act
        interlocking.OnSectionCleared(Yard.J1);

        // Assert
        Assert.Equal(new[] { Yard.A1, Yard.J1, Yard.M1 }, routeLock.RemainingSections);

        // Act
        interlocking.OnSectionCleared(Yard.A1);

        // Assert
        Assert.Equal(new[] { Yard.M1 }, routeLock.RemainingSections);
        Assert.False(interlocking.Section(Yard.J1).IsLocked);
        Assert.False(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void A_clear_ahead_of_the_train_keeps_the_lock() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);
        interlocking.OnSectionOccupied(Yard.A1);

        // Act
        interlocking.OnSectionCleared(Yard.M1);

        // Assert
        Assert.Equal(new[] { Yard.A1, Yard.J1, Yard.M1 }, routeLock.RemainingSections);
        Assert.Equal(Yard.Main, interlocking.Section(Yard.M1).LockedByRoute);
    }

    [Fact]
    public void A_clear_on_a_section_the_train_never_occupied_keeps_the_lock() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);

        // Act
        interlocking.OnSectionCleared(Yard.A1);

        // Assert
        Assert.Equal(3, routeLock.RemainingSections.Count);
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void A_rear_section_is_not_released_while_it_is_occupied_again() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock routeLock);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);

        // Act
        interlocking.OnSectionCleared(Yard.J1);

        // Assert
        Assert.Equal(Yard.A1, routeLock.RemainingSections[0]);
        Assert.True(interlocking.Section(Yard.A1).IsOccupied);
    }

    [Fact]
    public void A_second_train_can_be_given_the_route_once_the_first_has_cleared_it() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock firstLock);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionCleared(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);
        interlocking.OnSectionCleared(Yard.J1);
        interlocking.OnSectionOccupied(Yard.M1);
        interlocking.OnSectionCleared(Yard.M1);

        // Act
        RouteLock secondLock = interlocking.RequestRoute(Yard.Main, Yard.AnyTrainOtherThan(firstLock.Train), Yard.AnyInstant());

        // Assert
        Assert.NotSame(firstLock, secondLock);
        Assert.False(secondLock.TrainHasEntered);
        Assert.Equal(3, secondLock.RemainingSections.Count);
    }

    [Fact]
    public void The_same_train_can_be_given_another_route_once_its_route_has_ended() {
        // Arrange
        Interlocking interlocking = WithMainSet(out RouteLock firstLock);
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionCleared(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);
        interlocking.OnSectionCleared(Yard.J1);
        interlocking.OnSectionOccupied(Yard.M1);
        interlocking.OnSectionCleared(Yard.M1);

        // Act
        RouteLock next = interlocking.RequestRoute(Yard.YardRun, firstLock.Train, Yard.AnyInstant());

        // Assert
        Assert.Equal(Yard.YardRun, next.Route.Name);
    }

    [Fact]
    public void A_section_released_behind_the_train_is_unlocked_but_conflicts_are_judged_on_whole_routes_so_it_stays_unavailable_until_the_route_ends() {
        // Arrange
        Route shunt = Any.Route().WithName("SHUNT").WithSections(new[] { Yard.A1 }).WithPointSettings(Array.Empty<PointSetting>()).Generate();
        Interlocking interlocking = new Interlocking(Any.String().NotBlank().Generate(),
                                                     Yard.Sections(),
                                                     new[] { Yard.JunctionPoint(PointPosition.Reverse) },
                                                     new[] { Yard.MainRoute(), shunt });
        TrainId mainTrain = Yard.AnyTrain();
        interlocking.RequestRoute(Yard.Main, mainTrain, Yard.AnyInstant());
        interlocking.OnSectionOccupied(Yard.A1);
        interlocking.OnSectionOccupied(Yard.J1);
        interlocking.OnSectionCleared(Yard.A1);

        // Act
        Yard.AssertRefused(RefusalReason.ConflictingRoute, () => interlocking.RequestRoute("SHUNT", Yard.AnyTrainOtherThan(mainTrain), Yard.AnyInstant()));

        // Assert
        Assert.False(interlocking.Section(Yard.A1).IsLocked);
    }

}
