using JustDummies;

using Xunit;

namespace RailwayInterlocking.Domain.Tests;

public sealed class RouteTests {

    private static List<SectionId> AnySections(int count) {
        return Any.ListOf(new AnySectionId()).WithCount(count).Distinct().Generate();
    }

    // The refusals below call the constructor itself: AnyRoute draws the signals so that they differ and
    // wraps whatever its composer throws, so it could never hand the constructor a value to refuse.
    private static Route Build(string? name = null, string? entry = null, string? exit = null,
                               IReadOnlyList<SectionId>? sections = null, IReadOnlyList<PointSetting>? settings = null) {
        return new Route(name ?? "R", entry ?? "S1", exit ?? "S2", sections ?? new[] { new AnySectionId().Generate() }, settings ?? Array.Empty<PointSetting>());
    }

    [Fact]
    public void A_route_keeps_what_it_was_built_with() {
        // Arrange
        List<SectionId> anySections = AnySections(3);
        List<string> anyNames = Any.ListOf(Any.String().WithLengthBetween(1, 20).NotBlank()).WithCount(3).Distinct().Generate();
        PointSetting[] settings = { new("P1", PointPosition.Normal), new("P2", PointPosition.Reverse) };

        // Act
        Route route = new(anyNames[0], anyNames[1], anyNames[2], anySections, settings);

        // Assert
        Assert.Equal(anyNames[0], route.Name);
        Assert.Equal(anyNames[1], route.EntrySignal);
        Assert.Equal(anyNames[2], route.ExitSignal);
        Assert.Equal(anySections, route.Sections);
        Assert.Equal(settings, route.PointSettings);
    }

    [Fact]
    public void A_route_may_set_no_point_at_all() {
        // Act
        Route route = new AnyRoute().WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Assert
        Assert.Empty(route.PointSettings);
    }

    [Fact]
    public void A_route_is_not_changed_by_a_later_change_to_the_lists_it_was_built_from() {
        // Arrange
        List<SectionId> anySections = AnySections(2);
        SectionId anyExtra = new AnySectionId().WithValue(v => v.DifferentFrom(anySections[0].Value).DifferentFrom(anySections[1].Value)).Generate();
        List<PointSetting> settings = new() { new PointSetting("P1", PointPosition.Normal) };
        Route route = new AnyRoute().WithSections(anySections).WithPointSettings(settings).Generate();

        // Act
        anySections.Add(anyExtra);
        settings.Clear();

        // Assert
        Assert.Equal(2, route.Sections.Count);
        Assert.Single(route.PointSettings);
    }

    [Fact]
    public void A_route_keeps_its_sections_in_running_order() {
        // Arrange
        List<SectionId> anySections = AnySections(5);

        // Act
        Route route = new AnyRoute().WithSections(anySections).Generate();

        // Assert
        Assert.Equal(anySections, route.Sections);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_name_or_signal_is_refused(string blank) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => Build(name: blank));
        Assert.Throws<ArgumentException>(() => Build(entry: blank));
        Assert.Throws<ArgumentException>(() => Build(exit: blank));
    }

    [Fact]
    public void A_route_cannot_start_and_end_at_the_same_signal() {
        // Arrange
        string anySignal = Any.String().WithLengthBetween(1, 20).NotBlank().Generate();

        // Act
        Action construct = () => Build(entry: anySignal, exit: anySignal);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Signals_differing_only_by_case_are_different_signals() {
        // Act
        Route route = new AnyRoute().WithEntrySignal("S1").WithExitSignal("s1").Generate();

        // Assert
        Assert.NotEqual(route.EntrySignal, route.ExitSignal);
    }

    [Fact]
    public void A_route_needs_at_least_one_section() {
        // Act
        Action construct = () => Build(sections: Array.Empty<SectionId>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_cannot_traverse_a_section_twice() {
        // Arrange
        SectionId anyId = new AnySectionId().Generate();
        SectionId sameId = new(anyId.Value);

        // Act
        Action construct = () => Build(sections: new[] { anyId, sameId });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_cannot_hold_a_null_section() {
        // Arrange
        SectionId anyId = new AnySectionId().Generate();

        // Act
        Action construct = () => Build(sections: new SectionId[] { anyId, null! });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_cannot_hold_a_null_point_setting() {
        // Act
        Action construct = () => Build(settings: new PointSetting[] { null! });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_cannot_set_the_same_point_twice_even_to_the_same_position() {
        // Arrange
        PointSetting[] twice = { new("P1", PointPosition.Normal), new("P1", PointPosition.Normal) };

        // Act
        Action construct = () => Build(settings: twice);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Point_names_differing_only_by_case_are_different_points() {
        // Arrange
        PointSetting[] settings = { new("P1", PointPosition.Normal), new("p1", PointPosition.Reverse) };

        // Act
        Route route = new AnyRoute().WithPointSettings(settings).Generate();

        // Assert
        Assert.Equal(2, route.PointSettings.Count);
    }

    [Fact]
    public void A_null_list_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Route("R", "S1", "S2", null!, Array.Empty<PointSetting>()));
        Assert.Throws<ArgumentNullException>(() => new Route("R", "S1", "S2", new[] { new AnySectionId().Generate() }, null!));
    }

    // ---- ConflictsWith ----

    [Fact]
    public void Two_routes_sharing_a_section_conflict() {
        // Arrange
        List<SectionId> anySections = AnySections(3);
        Route first = new AnyRoute().WithSections(new[] { anySections[0], anySections[1] }).WithPointSettings(Array.Empty<PointSetting>()).Generate();
        Route second = new AnyRoute().WithSections(new[] { anySections[1], anySections[2] }).WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_routes_with_nothing_in_common_do_not_conflict() {
        // Arrange
        List<SectionId> anySections = AnySections(2);
        Route first = new AnyRoute().WithSections(new[] { anySections[0] }).WithPointSettings(Array.Empty<PointSetting>()).Generate();
        Route second = new AnyRoute().WithSections(new[] { anySections[1] }).WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_routes_needing_the_same_point_in_opposite_positions_conflict_even_on_disjoint_sections() {
        // Arrange
        List<SectionId> anySections = AnySections(2);
        Route toNormal = new AnyRoute().WithSections(new[] { anySections[0] })
                                       .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();
        Route toReverse = new AnyRoute().WithSections(new[] { anySections[1] })
                                        .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Reverse) }).Generate();

        // Assert
        Assert.True(toNormal.ConflictsWith(toReverse));
        Assert.True(toReverse.ConflictsWith(toNormal));
    }

    [Fact]
    public void Two_routes_needing_the_same_point_in_the_same_position_do_not_conflict_on_that_account() {
        // Arrange
        List<SectionId> anySections = AnySections(2);
        Route first = new AnyRoute().WithSections(new[] { anySections[0] })
                                    .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();
        Route second = new AnyRoute().WithSections(new[] { anySections[1] })
                                     .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void Two_routes_needing_different_points_do_not_conflict_on_that_account() {
        // Arrange
        List<SectionId> anySections = AnySections(2);
        Route first = new AnyRoute().WithSections(new[] { anySections[0] })
                                    .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();
        Route second = new AnyRoute().WithSections(new[] { anySections[1] })
                                     .WithPointSettings(new[] { new PointSetting("P2", PointPosition.Reverse) }).Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_route_does_not_conflict_with_itself() {
        // Arrange
        Route route = new AnyRoute().Generate();

        // Assert
        Assert.False(route.ConflictsWith(route));
    }

    [Fact]
    public void Conflicts_with_null_is_refused() {
        // Arrange
        Route route = new AnyRoute().Generate();

        // Act
        Action ask = () => route.ConflictsWith(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(ask);
    }

}

public sealed class RouteLockTests {

    [Fact]
    public void A_new_lock_has_every_section_remaining_and_nothing_visited() {
        // Arrange
        Route anyRoute = new AnyRoute().Generate();
        TrainId anyTrain = new AnyTrainId().Generate();
        DateTimeOffset anyLockedAt = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                                  new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        // Act
        RouteLock routeLock = new(anyRoute, anyTrain, anyLockedAt);

        // Assert
        Assert.Same(anyRoute, routeLock.Route);
        Assert.Equal(anyTrain, routeLock.Train);
        Assert.Equal(anyLockedAt, routeLock.LockedAt);
        Assert.Equal(anyRoute.Sections, routeLock.RemainingSections);
        Assert.False(routeLock.TrainHasEntered);
        Assert.False(routeLock.IsFullyReleased);
        Assert.All(anyRoute.Sections, section => Assert.False(routeLock.HasVisited(section)));
    }

    [Fact]
    public void A_lock_time_must_be_set() {
        // Arrange
        Route anyRoute = new AnyRoute().Generate();
        TrainId anyTrain = new AnyTrainId().Generate();

        // Act
        Action construct = () => new RouteLock(anyRoute, anyTrain, default);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_lock_needs_a_route_and_a_train() {
        // Arrange
        Route anyRoute = new AnyRoute().Generate();
        TrainId anyTrain = new AnyTrainId().Generate();
        DateTimeOffset anyLockedAt = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new RouteLock(null!, anyTrain, anyLockedAt));
        Assert.Throws<ArgumentNullException>(() => new RouteLock(anyRoute, null!, anyLockedAt));
    }

}
