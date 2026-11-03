using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class InterlockingConstructionTests {

    private static readonly SectionId S1 = new SectionId("S1");
    private static readonly SectionId S2 = new SectionId("S2");

    private static List<TrackSection> TwoSections() {
        return new List<TrackSection> { Any.TrackSection().WithId(S1).Generate(), Any.TrackSection().WithId(S2).Generate() };
    }

    private static string AnyAreaName() {
        return Any.String().NotBlank().Generate();
    }

    [Fact]
    public void Is_built_from_a_consistent_definition_and_starts_with_nothing_set() {
        // Act
        Interlocking interlocking = Yard.Build();

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
        Assert.False(interlocking.Section(Yard.J1).IsLocked);
        Assert.False(interlocking.Point("P1").IsLocked);
    }

    [Fact]
    public void Is_built_from_any_consistent_definition_a_generator_can_draw() {
        // Act
        Interlocking anyInterlocking = Any.Interlocking().Generate();

        // Assert
        Assert.Empty(anyInterlocking.ActiveRoutes);
    }

    [Fact]
    public void Hands_back_the_area_name_it_was_given() {
        // Arrange
        string areaName = "Northern Junction";

        // Act
        Interlocking interlocking = new Interlocking(areaName, TwoSections(), Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Equal(areaName, interlocking.AreaName);
    }

    [Fact]
    public void Accepts_an_area_without_points_or_routes() {
        // Act
        Interlocking interlocking = new Interlocking(AnyAreaName(), TwoSections(), Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Fact]
    public void Gives_access_to_the_section_and_the_point_it_was_built_with() {
        // Arrange
        TrackSection section = Any.TrackSection().WithId(S1).Generate();
        Point        point   = Any.Point().WithName("P1").WithSection(S1).Generate();

        // Act
        Interlocking interlocking = new Interlocking(AnyAreaName(), new[] { section }, new[] { point }, Array.Empty<Route>());

        // Assert
        Assert.Same(section, interlocking.Section(S1));
        Assert.Same(point, interlocking.Point("P1"));
    }

    [Fact]
    public void Finds_a_section_by_the_value_of_its_id() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act & Assert
        Assert.Same(interlocking.Section(Yard.A1), interlocking.Section(new SectionId("A1")));
    }

    [Fact]
    public void Refuses_to_look_up_an_unknown_section_or_point() {
        // Arrange
        Interlocking interlocking = Yard.Build();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => interlocking.Section(new SectionId("Z9")));
        Assert.Throws<KeyNotFoundException>(() => interlocking.Point("P9"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Refuses_a_blank_area_name(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Interlocking(blank, TwoSections(), Array.Empty<Point>(), Array.Empty<Route>()));
    }

    [Fact]
    public void Refuses_a_null_area_name() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Interlocking(null!, TwoSections(), Array.Empty<Point>(), Array.Empty<Route>()));
    }

    [Fact]
    public void Refuses_null_collections() {
        // Arrange
        string areaName = AnyAreaName();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, null!, Array.Empty<Point>(), Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, TwoSections(), null!, Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, TwoSections(), Array.Empty<Point>(), null!));
    }

    [Fact]
    public void Refuses_a_null_element_in_a_collection() {
        // Arrange
        string areaName = AnyAreaName();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, new TrackSection[] { null! }, Array.Empty<Point>(), Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, TwoSections(), new Point[] { null! }, Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, TwoSections(), Array.Empty<Point>(), new Route[] { null! }));
    }

    [Fact]
    public void Refuses_a_section_declared_twice() {
        // Arrange
        TrackSection first  = Any.TrackSection().WithId(S1).Generate();
        TrackSection second = Any.TrackSection().WithId(new SectionId("S1")).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), new[] { first, second }, Array.Empty<Point>(), Array.Empty<Route>()));

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_point_lying_in_an_unknown_section() {
        // Arrange
        Point point = Any.Point().WithName("P1").WithSection(new SectionId("Z9")).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), new[] { point }, Array.Empty<Route>()));

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_point_declared_twice() {
        // Arrange
        Point first  = Any.Point().WithName("P1").WithSection(S1).Generate();
        Point second = Any.Point().WithName("P1").WithSection(S2).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), new[] { first, second }, Array.Empty<Route>()));

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_route_over_an_unknown_section() {
        // Arrange
        Route route = Any.Route().WithSections(new[] { S1, new SectionId("Z9") }).WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), Array.Empty<Point>(), new[] { route }));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_route_setting_an_unknown_point() {
        // Arrange
        Route route = Any.Route()
                         .WithSections(new[] { S1 })
                         .WithPointSettings(new[] { new PointSetting("P9", PointPosition.Normal) })
                         .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), Array.Empty<Point>(), new[] { route }));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_route_setting_a_point_outside_its_own_sections() {
        // Arrange
        Point point = Any.Point().WithName("P1").WithSection(S2).Generate();
        Route route = Any.Route()
                         .WithSections(new[] { S1 })
                         .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) })
                         .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), new[] { point }, new[] { route }));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void Refuses_a_route_declared_twice() {
        // Arrange
        Route first  = Any.Route().WithName("MAIN").WithSections(new[] { S1 }).WithPointSettings(Array.Empty<PointSetting>()).Generate();
        Route second = Any.Route().WithName("MAIN").WithSections(new[] { S2 }).WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(AnyAreaName(), TwoSections(), Array.Empty<Point>(), new[] { first, second }));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void Accepts_two_routes_over_the_same_sections_under_different_names() {
        // Arrange
        Route first  = Any.Route().WithName("FIRST").WithSections(new[] { S1 }).WithPointSettings(Array.Empty<PointSetting>()).Generate();
        Route second = Any.Route().WithName("SECOND").WithSections(new[] { S1 }).WithPointSettings(Array.Empty<PointSetting>()).Generate();

        // Act
        Interlocking interlocking = new Interlocking(AnyAreaName(), TwoSections(), Array.Empty<Point>(), new[] { first, second });

        // Assert
        Assert.Empty(interlocking.ActiveRoutes);
    }

}
