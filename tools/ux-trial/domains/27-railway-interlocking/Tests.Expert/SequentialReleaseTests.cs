using RailwayInterlocking.Domain.Tests.Expert.Support;

namespace RailwayInterlocking.Domain.Tests.Expert;

/// <summary>
///     ToMain runs Approach → Throat → Main, with the points in Throat. The tests drive it with the track circuit
///     events a real train would cause, and a few a real train should not.
/// </summary>
public sealed class SequentialReleaseTests {

    private static (Junction Junction, RouteLock RouteLock) JunctionWithToMainSet() {
        Junction  junction  = Junction.Build();
        RouteLock routeLock = junction.Interlocking.RequestRoute(junction.ToMain.Name, Any.TrainId().Generate(), Instants.AnyLockTime());

        return (junction, routeLock);
    }

    // ---- occupancy ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_train_detected_on_a_section_of_its_route_has_entered_the_route() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();

        junction.Interlocking.OnSectionOccupied(junction.Approach);

        Assert.True(junction.Interlocking.Section(junction.Approach).IsOccupied);
        Assert.True(routeLock.TrainHasEntered);
        Assert.True(routeLock.HasVisited(junction.Approach));
        Assert.False(routeLock.HasVisited(junction.Throat));
    }

    [Fact]
    public void A_detection_on_a_section_no_route_holds_is_recorded_and_changes_no_route() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();

        junction.Interlocking.OnSectionOccupied(junction.Yard);

        Assert.True(junction.Interlocking.Section(junction.Yard).IsOccupied);
        Assert.False(routeLock.TrainHasEntered);
    }

    [Fact]
    public void A_detection_on_a_section_that_does_not_exist_is_refused() {
        Junction  junction = Junction.Build();
        SectionId unknown  = Any.SectionId().WithValue(value => value.Except(junction.Sections.Select(section => section.Id.Value))).Generate();

        Assert.Throws<KeyNotFoundException>(() => junction.Interlocking.OnSectionOccupied(unknown));
        Assert.Throws<KeyNotFoundException>(() => junction.Interlocking.OnSectionCleared(unknown));
    }

    // ---- release behind the train -------------------------------------------------------------------------------

    [Fact]
    public void A_section_is_released_when_the_train_has_been_on_it_and_left_it() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);

        junction.Interlocking.OnSectionCleared(junction.Approach);

        Assert.False(junction.Interlocking.Section(junction.Approach).IsLocked);
        Assert.Equal([junction.Throat, junction.Main], routeLock.RemainingSections);
        Assert.Same(routeLock, Assert.Single(junction.Interlocking.ActiveRoutes));
    }

    [Fact]
    public void The_sections_ahead_of_the_train_stay_locked_while_it_runs_the_route() {
        (Junction junction, _) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);
        junction.Interlocking.OnSectionCleared(junction.Approach);

        Assert.True(junction.Interlocking.Section(junction.Throat).IsLocked);
        Assert.True(junction.Interlocking.Section(junction.Main).IsLocked);
    }

    [Fact]
    public void The_points_stay_locked_until_the_section_they_lie_in_is_released() {
        (Junction junction, _) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);
        junction.Interlocking.OnSectionCleared(junction.Approach);
        Point points = junction.Interlocking.Point(junction.PointName);

        Assert.True(points.IsLocked);

        junction.Interlocking.OnSectionOccupied(junction.Main);
        junction.Interlocking.OnSectionCleared(junction.Throat);

        Assert.False(points.IsLocked);
    }

    [Fact]
    public void The_route_ends_with_the_release_of_its_last_section_and_frees_everything_it_held() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();

        foreach (SectionId section in junction.ToMain.Sections) {
            junction.Interlocking.OnSectionOccupied(section);
            junction.Interlocking.OnSectionCleared(section);
        }

        Assert.True(routeLock.IsFullyReleased);
        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.All(junction.ToMain.Sections, id => Assert.False(junction.Interlocking.Section(id).IsLocked));
        Assert.False(junction.Interlocking.Point(junction.PointName).IsLocked);
    }

    [Fact]
    public void A_train_whose_route_has_ended_can_be_given_another_route() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();
        foreach (SectionId section in junction.ToMain.Sections) {
            junction.Interlocking.OnSectionOccupied(section);
            junction.Interlocking.OnSectionCleared(section);
        }

        RouteLock next = junction.Interlocking.RequestRoute(junction.ToSiding.Name, routeLock.Train, Instants.AnyLockTime());

        Assert.Same(junction.ToSiding, next.Route);
    }

    [Fact]
    public void A_route_over_a_single_section_ends_as_soon_as_the_train_has_cleared_it() {
        Junction junction = Junction.Build();
        junction.Interlocking.RequestRoute(junction.Goods.Name, Any.TrainId().Generate(), Instants.AnyLockTime());
        junction.Interlocking.OnSectionOccupied(junction.Yard);

        junction.Interlocking.OnSectionCleared(junction.Yard);

        Assert.Empty(junction.Interlocking.ActiveRoutes);
        Assert.False(junction.Interlocking.Section(junction.Yard).IsLocked);
    }

    // ---- events that do not release anything --------------------------------------------------------------------

    [Fact]
    public void A_clear_on_a_section_the_train_has_not_reached_keeps_the_lock() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);

        junction.Interlocking.OnSectionCleared(junction.Main);

        Assert.True(junction.Interlocking.Section(junction.Main).IsLocked);
        Assert.True(junction.Interlocking.Section(junction.Approach).IsLocked);
        Assert.Equal(junction.ToMain.Sections, routeLock.RemainingSections);
    }

    [Fact]
    public void A_clear_before_any_detection_releases_nothing() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();

        junction.Interlocking.OnSectionCleared(junction.Approach);

        Assert.Equal(junction.ToMain.Sections, routeLock.RemainingSections);
        Assert.False(routeLock.TrainHasEntered);
        Assert.True(junction.Interlocking.Section(junction.Approach).IsLocked);
    }

    [Fact]
    public void A_clear_on_a_section_no_route_holds_only_records_that_it_is_clear() {
        Junction junction = Junction.Build();
        junction.Interlocking.OnSectionOccupied(junction.Yard);

        junction.Interlocking.OnSectionCleared(junction.Yard);

        Assert.False(junction.Interlocking.Section(junction.Yard).IsOccupied);
    }

    [Fact]
    public void A_section_the_train_is_still_on_is_not_released_by_the_clear_of_the_one_behind_it_having_been_missed() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);

        junction.Interlocking.OnSectionCleared(junction.Throat);   // the clear arrives ahead of the one behind it

        Assert.Equal(junction.ToMain.Sections, routeLock.RemainingSections);
    }

    // ---- out-of-order events -----------------------------------------------------------------------------------

    [Fact]
    public void A_clear_that_arrived_early_is_caught_up_when_the_section_behind_it_clears() {
        (Junction junction, RouteLock routeLock) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);
        junction.Interlocking.OnSectionOccupied(junction.Main);
        junction.Interlocking.OnSectionCleared(junction.Throat);   // early: Approach is still reported occupied

        junction.Interlocking.OnSectionCleared(junction.Approach);

        Assert.Equal([junction.Main], routeLock.RemainingSections);
        Assert.False(junction.Interlocking.Section(junction.Approach).IsLocked);
        Assert.False(junction.Interlocking.Section(junction.Throat).IsLocked);
        Assert.False(junction.Interlocking.Point(junction.PointName).IsLocked);
        Assert.True(junction.Interlocking.Section(junction.Main).IsLocked);
    }

    [Fact]
    public void Events_about_one_route_do_not_release_anything_of_another() {
        (Junction junction, RouteLock toMain) = JunctionWithToMainSet();
        RouteLock goods = junction.Interlocking.RequestRoute(junction.Goods.Name, Trains.AnyOtherThan(toMain.Train), Instants.AnyLockTime());

        junction.Interlocking.OnSectionOccupied(junction.Yard);
        junction.Interlocking.OnSectionCleared(junction.Yard);

        Assert.True(goods.IsFullyReleased);
        Assert.Same(toMain, Assert.Single(junction.Interlocking.ActiveRoutes));
        Assert.Equal(junction.ToMain.Sections, toMain.RemainingSections);
        Assert.False(toMain.TrainHasEntered);
    }

    // ---- what a partly released route still holds ---------------------------------------------------------------

    [Fact]
    public void A_section_released_behind_the_train_is_not_available_to_a_conflicting_route_until_the_whole_route_has_ended() {
        (Junction junction, RouteLock toMain) = JunctionWithToMainSet();
        junction.Interlocking.OnSectionOccupied(junction.Approach);
        junction.Interlocking.OnSectionOccupied(junction.Throat);
        junction.Interlocking.OnSectionCleared(junction.Approach);

        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(
            () => junction.Interlocking.RequestRoute(junction.ToSiding.Name, Trains.AnyOtherThan(toMain.Train), Instants.AnyLockTime()));

        Assert.Equal(RefusalReason.ConflictingRoute, refusal.Reason);
    }

}
