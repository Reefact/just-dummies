using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class RouteTests {

    private static readonly SectionId S1 = new SectionId("S1");
    private static readonly SectionId S2 = new SectionId("S2");
    private static readonly SectionId S3 = new SectionId("S3");

    private static AnyRoute RouteOver(params SectionId[] sections) {
        return Any.Route().WithSections(sections).WithPointSettings(Array.Empty<PointSetting>());
    }

    [Fact]
    public void Keeps_what_it_was_given_in_the_order_given() {
        // Arrange
        PointSetting[] settings = { new PointSetting("P1", PointPosition.Normal), new PointSetting("P2", PointPosition.Reverse) };

        // Act
        Route route = new Route("MAIN", "SIG-A", "SIG-B", new[] { S2, S1, S3 }, settings);

        // Assert
        Assert.Equal("MAIN", route.Name);
        Assert.Equal("SIG-A", route.EntrySignal);
        Assert.Equal("SIG-B", route.ExitSignal);
        Assert.Equal(new[] { S2, S1, S3 }, route.Sections);
        Assert.Equal(settings, route.PointSettings);
    }

    [Fact]
    public void A_route_without_point_settings_is_valid() {
        // Act
        Route route = RouteOver(S1).Generate();

        // Assert
        Assert.Empty(route.PointSettings);
    }

    [Fact]
    public void Is_not_changed_by_a_later_change_to_the_lists_it_was_built_from() {
        // Arrange
        List<SectionId>    sections = new List<SectionId> { S1, S2 };
        List<PointSetting> settings = new List<PointSetting> { new PointSetting("P1", PointPosition.Normal) };

        Route route = Any.Route().WithSections(sections).WithPointSettings(settings).Generate();

        // Act
        sections.Add(S3);
        settings.Clear();

        // Assert
        Assert.Equal(2, route.Sections.Count);
        Assert.Single(route.PointSettings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Refuses_a_blank_name(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Route().WithName(blank).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Refuses_a_blank_entry_signal(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Route().WithEntrySignal(blank).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Refuses_a_blank_exit_signal(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Route().WithExitSignal(blank).Generate());
    }

    [Fact]
    public void Refuses_to_start_and_end_at_the_same_signal() {
        // Arrange
        // Built by hand: the generator ties the two signals so that they differ, which is the very input refused here.
        string anyName   = Any.String().NotBlank().Generate();
        string anySignal = Any.String().NotBlank().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Route(anyName, anySignal, anySignal, new[] { S1 }, Array.Empty<PointSetting>()));

        // Assert
        Assert.Equal("exitSignal", refusal.ParamName);
    }

    [Fact]
    public void Signals_differing_only_by_case_are_two_signals() {
        // Act
        Route route = Any.Route().WithEntrySignal("sig-a").WithExitSignal("SIG-A").Generate();

        // Assert
        Assert.NotEqual(route.EntrySignal, route.ExitSignal);
    }

    [Fact]
    public void Refuses_an_empty_section_list() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RouteOver().Generate());
    }

    [Fact]
    public void Refuses_a_null_section_list() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();
        string anyIn   = Any.String().NotBlank().Generate();
        string anyOut  = anyIn + "x";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Route(anyName, anyIn, anyOut, null!, Array.Empty<PointSetting>()));
    }

    [Fact]
    public void Refuses_a_null_point_setting_list() {
        // Arrange
        string anyName = Any.String().NotBlank().Generate();
        string anyIn   = Any.String().NotBlank().Generate();
        string anyOut  = anyIn + "x";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Route(anyName, anyIn, anyOut, new[] { S1 }, null!));
    }

    [Fact]
    public void Refuses_a_null_section() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RouteOver(S1, null!, S2).Generate());
    }

    [Fact]
    public void Refuses_a_section_listed_twice() {
        // Arrange
        SectionId anySection = Any.SectionId().Generate();
        SectionId sameAgain  = new SectionId(anySection.Value);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => RouteOver(anySection, S1, sameAgain).Generate());
    }

    [Fact]
    public void Refuses_a_null_point_setting() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Route().WithPointSettings(new PointSetting[] { null! }).Generate());
    }

    [Theory]
    [InlineData(PointPosition.Normal, PointPosition.Normal)]
    [InlineData(PointPosition.Normal, PointPosition.Reverse)]
    public void Refuses_to_set_the_same_point_twice(PointPosition first, PointPosition second) {
        // Arrange
        PointSetting[] settings = { new PointSetting("P1", first), new PointSetting("P1", second) };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Any.Route().WithPointSettings(settings).Generate());
    }

    [Fact]
    public void Point_names_differing_only_by_case_are_two_points() {
        // Arrange
        PointSetting[] settings = { new PointSetting("P1", PointPosition.Normal), new PointSetting("p1", PointPosition.Normal) };

        // Act
        Route route = Any.Route().WithPointSettings(settings).Generate();

        // Assert
        Assert.Equal(2, route.PointSettings.Count);
    }

    [Fact]
    public void Conflicts_with_a_route_sharing_a_section() {
        // Arrange
        Route first  = RouteOver(S1, S2).WithName("FIRST").Generate();
        Route second = RouteOver(S2, S3).WithName("SECOND").Generate();

        // Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Does_not_conflict_with_a_route_over_other_sections() {
        // Arrange
        Route first  = RouteOver(S1).WithName("FIRST").Generate();
        Route second = RouteOver(S2, S3).WithName("SECOND").Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
        Assert.False(second.ConflictsWith(first));
    }

    [Fact]
    public void Conflicts_with_a_route_needing_a_shared_point_in_the_opposite_position() {
        // Arrange
        Route first  = RouteOver(S1).WithName("FIRST").WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();
        Route second = RouteOver(S2).WithName("SECOND").WithPointSettings(new[] { new PointSetting("P1", PointPosition.Reverse) }).Generate();

        // Assert
        Assert.True(first.ConflictsWith(second));
        Assert.True(second.ConflictsWith(first));
    }

    [Fact]
    public void Does_not_conflict_over_a_shared_point_when_both_need_it_in_the_same_position() {
        // Arrange
        Route first  = RouteOver(S1).WithName("FIRST").WithPointSettings(new[] { new PointSetting("P1", PointPosition.Reverse) }).Generate();
        Route second = RouteOver(S2).WithName("SECOND").WithPointSettings(new[] { new PointSetting("P1", PointPosition.Reverse) }).Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void Does_not_conflict_over_different_points() {
        // Arrange
        Route first  = RouteOver(S1).WithName("FIRST").WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) }).Generate();
        Route second = RouteOver(S2).WithName("SECOND").WithPointSettings(new[] { new PointSetting("P2", PointPosition.Reverse) }).Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void Does_not_conflict_with_itself() {
        // Arrange
        Route route = RouteOver(S1, S2).Generate();

        // Assert
        Assert.False(route.ConflictsWith(route));
    }

    [Fact]
    public void Does_not_conflict_with_a_route_of_the_same_name() {
        // Arrange
        Route first  = RouteOver(S1).WithName("SAME").Generate();
        Route second = RouteOver(S1).WithName("SAME").Generate();

        // Assert
        Assert.False(first.ConflictsWith(second));
    }

    [Fact]
    public void Refuses_to_be_compared_with_null() {
        // Arrange
        Route route = Any.Route().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => route.ConflictsWith(null!));
    }

}
