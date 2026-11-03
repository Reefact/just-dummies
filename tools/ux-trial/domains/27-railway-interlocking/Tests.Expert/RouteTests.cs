namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class RouteTests {

    // ---- construction -------------------------------------------------------------------------------------------

    [Fact]
    public void A_route_keeps_its_sections_in_running_order_and_its_point_settings() {
        List<SectionId>    anyRunningOrder = Any.ListOf(Any.SectionId()).WithCountBetween(2, 6).Distinct().Generate();
        List<PointSetting> anySettings     = Any.ListOf(Any.PointSetting()).WithCountBetween(1, 4).DistinctBy(setting => setting.PointName).Generate();

        Route route = Any.Route().WithSections(anyRunningOrder).WithPointSettings(anySettings).Generate();

        Assert.Equal(anyRunningOrder, route.Sections);
        Assert.Equal(anySettings, route.PointSettings);
    }

    [Fact]
    public void A_route_is_not_changed_by_a_later_change_to_the_lists_it_was_built_from() {
        List<SectionId>    sections = Any.ListOf(Any.SectionId()).WithCount(3).Distinct().Generate();
        List<PointSetting> settings = Any.ListOf(Any.PointSetting()).WithCount(2).DistinctBy(setting => setting.PointName).Generate();
        Route              route    = Any.Route().WithSections(sections).WithPointSettings(settings).Generate();

        sections.RemoveAt(0);
        settings.Clear();

        Assert.Equal(3, route.Sections.Count);
        Assert.Equal(2, route.PointSettings.Count);
    }

    [Fact]
    public void A_route_without_point_settings_is_a_route_over_plain_track() {
        Route route = Any.Route().WithPointSettings([]).Generate();

        Assert.Empty(route.PointSettings);
    }

    [Fact]
    public void A_route_cannot_start_and_end_at_the_same_signal() {
        Route anyRoute = Any.Route().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.EntrySignal, anyRoute.Sections, anyRoute.PointSettings));

        Assert.Equal("exitSignal", refusal.ParamName);
    }

    [Fact]
    public void A_route_must_traverse_at_least_one_section() {
        Route anyRoute = Any.Route().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, [], anyRoute.PointSettings));

        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_route_cannot_traverse_a_section_twice_even_when_other_sections_come_between() {
        Route           anyRoute     = Any.Route().Generate();
        SectionId       anyRepeated  = Any.SectionId().Generate();
        List<SectionId> anyOthers    = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        SectionId[]     withTheLoop  = [anyRepeated, anyOthers[0], anyOthers[1], anyRepeated];

        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, withTheLoop, anyRoute.PointSettings));
    }

    [Fact]
    public void A_route_cannot_contain_a_missing_section() {
        Route       anyRoute   = Any.Route().Generate();
        SectionId[] withAHole = [Any.SectionId().Generate(), null!];

        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, withAHole, anyRoute.PointSettings));
    }

    [Theory]
    [InlineData(PointPosition.Normal, PointPosition.Normal)]
    [InlineData(PointPosition.Normal, PointPosition.Reverse)]
    public void A_route_cannot_set_the_same_point_twice_whatever_the_positions(PointPosition first, PointPosition second) {
        Route        anyRoute     = Any.Route().Generate();
        string       anyPointName = AnyPoint.ValidName().Generate();
        PointSetting[] twice      = [new PointSetting(anyPointName, first), new PointSetting(anyPointName, second)];

        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, anyRoute.Sections, twice));
    }

    [Fact]
    public void A_route_cannot_contain_a_missing_point_setting() {
        Route          anyRoute   = Any.Route().Generate();
        PointSetting[] withAHole = [Any.PointSetting().Generate(), null!];

        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, anyRoute.Sections, withAHole));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_route_needs_a_name_and_two_signals_that_are_not_blank(string blank) {
        Route anyRoute = Any.Route().Generate();

        Assert.Throws<ArgumentException>(() => new Route(blank, anyRoute.EntrySignal, anyRoute.ExitSignal, anyRoute.Sections, anyRoute.PointSettings));
        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, blank, anyRoute.ExitSignal, anyRoute.Sections, anyRoute.PointSettings));
        Assert.Throws<ArgumentException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, blank, anyRoute.Sections, anyRoute.PointSettings));
    }

    [Fact]
    public void A_route_needs_each_of_its_parts() {
        Route anyRoute = Any.Route().Generate();

        Assert.Throws<ArgumentNullException>(() => new Route(null!, anyRoute.EntrySignal, anyRoute.ExitSignal, anyRoute.Sections, anyRoute.PointSettings));
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Name, null!, anyRoute.ExitSignal, anyRoute.Sections, anyRoute.PointSettings));
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, null!, anyRoute.Sections, anyRoute.PointSettings));
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, null!, anyRoute.PointSettings));
        Assert.Throws<ArgumentNullException>(() => new Route(anyRoute.Name, anyRoute.EntrySignal, anyRoute.ExitSignal, anyRoute.Sections, null!));
    }

    // ---- conflicts ----------------------------------------------------------------------------------------------

    [Fact]
    public void Two_routes_over_a_common_section_conflict_in_both_directions() {
        List<SectionId> anySections = Any.ListOf(Any.SectionId()).WithCount(3).Distinct().Generate();
        SectionId       shared      = anySections[1];
        Route           first       = Any.Route().WithSections([anySections[0], shared]).WithPointSettings([]).Generate();
        Route           second      = Any.Route().WithName(name => name.DifferentFrom(first.Name)).WithSections([shared, anySections[2]]).WithPointSettings([]).Generate();

        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Two_routes_over_different_sections_and_no_common_point_do_not_conflict() {
        List<SectionId> anySections = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        Route           first       = Any.Route().WithSections([anySections[0]]).WithPointSettings([]).Generate();
        Route           second      = Any.Route().WithName(name => name.DifferentFrom(first.Name)).WithSections([anySections[1]]).WithPointSettings([]).Generate();

        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Theory]
    [InlineData(PointPosition.Normal, PointPosition.Reverse)]
    [InlineData(PointPosition.Reverse, PointPosition.Normal)]
    public void Two_routes_needing_the_same_point_in_opposite_positions_conflict_even_over_different_sections(PointPosition firstNeeds, PointPosition secondNeeds) {
        List<SectionId> anySections = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        string          pointName   = AnyPoint.ValidName().Generate();
        Route           first       = Any.Route().WithSections([anySections[0]]).WithPointSettings([new PointSetting(pointName, firstNeeds)]).Generate();
        Route           second      = Any.Route().WithName(name => name.DifferentFrom(first.Name)).WithSections([anySections[1]]).WithPointSettings([new PointSetting(pointName, secondNeeds)]).Generate();

        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Theory]
    [InlineData(PointPosition.Normal)]
    [InlineData(PointPosition.Reverse)]
    public void Two_routes_needing_the_same_point_in_the_same_position_do_not_conflict_over_it(PointPosition both) {
        List<SectionId> anySections = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        string          pointName   = AnyPoint.ValidName().Generate();
        Route           first       = Any.Route().WithSections([anySections[0]]).WithPointSettings([new PointSetting(pointName, both)]).Generate();
        Route           second      = Any.Route().WithName(name => name.DifferentFrom(first.Name)).WithSections([anySections[1]]).WithPointSettings([new PointSetting(pointName, both)]).Generate();

        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void Two_routes_needing_different_points_in_opposite_positions_do_not_conflict() {
        List<SectionId> anySections = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        List<string>    pointNames  = Any.ListOf(AnyPoint.ValidName()).WithCount(2).Distinct().Generate();
        Route           first       = Any.Route().WithSections([anySections[0]]).WithPointSettings([new PointSetting(pointNames[0], PointPosition.Normal)]).Generate();
        Route           second      = Any.Route().WithName(name => name.DifferentFrom(first.Name)).WithSections([anySections[1]]).WithPointSettings([new PointSetting(pointNames[1], PointPosition.Reverse)]).Generate();

        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void A_route_does_not_conflict_with_itself() {
        Route anyRoute = Any.Route().Generate();

        Assert.False(anyRoute.ConflictsWith(anyRoute));
    }

    [Fact]
    public void A_route_does_not_conflict_with_a_route_of_the_same_name() {
        Route first  = Any.Route().Generate();
        Route sameName = Any.Route().WithName(first.Name).WithSections(first.Sections).Generate();

        Assert.False(first.ConflictsWith(sameName));
    }

    [Fact]
    public void A_route_cannot_be_compared_to_a_missing_route() {
        Route anyRoute = Any.Route().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRoute.ConflictsWith(null!));
    }

}
