using JustDummies;

using Xunit;

namespace RailwayInterlocking.Domain.Tests;

public sealed class InterlockingConstructionTests {

    [Fact]
    public void An_interlocking_exposes_its_area_name() {
        // Act
        Interlocking interlocking = new("Area north", Array.Empty<TrackSection>(), Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Equal("Area north", interlocking.AreaName);
        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Fact]
    public void A_complete_layout_is_accepted_and_its_sections_and_points_can_be_looked_up() {
        // Arrange
        Network network = Network.Drawn();

        // Act
        Interlocking interlocking = network.Build();

        // Assert
        Assert.Equal(network.C, interlocking.Section(network.C).Id);
        Assert.Equal(network.PointName, interlocking.Point(network.PointName).Name);
        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_blank_area_name_is_refused(string blankName) {
        // Act
        Action construct = () => new Interlocking(blankName, Array.Empty<TrackSection>(), Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_null_area_name_is_refused() {
        // Act
        Action construct = () => new Interlocking(null!, Array.Empty<TrackSection>(), Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Throws<ArgumentNullException>(construct);
    }

    [Fact]
    public void A_null_collection_is_refused() {
        // Arrange
        string areaName = Any.String().WithLengthBetween(1, 20).NotBlank().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, null!, Array.Empty<Point>(), Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, Array.Empty<TrackSection>(), null!, Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(areaName, Array.Empty<TrackSection>(), Array.Empty<Point>(), null!));
    }

    [Fact]
    public void A_null_element_is_refused() {
        // Arrange
        TrackSection anySection = new AnyTrackSection().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Interlocking("Area north", new TrackSection[] { anySection, null! }, Array.Empty<Point>(), Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking("Area north", new[] { anySection }, new Point[] { null! }, Array.Empty<Route>()));
        Assert.Throws<ArgumentNullException>(() => new Interlocking("Area north", new[] { anySection }, Array.Empty<Point>(), new Route[] { null! }));
    }

    [Fact]
    public void A_section_declared_twice_is_refused() {
        // Arrange
        SectionId anyId = new AnySectionId().Generate();
        TrackSection first = new AnyTrackSection().WithId(anyId).Generate();
        TrackSection second = new AnyTrackSection().WithId(anyId).Generate();

        // Act
        Action construct = () => new Interlocking("Area north", new[] { first, second }, Array.Empty<Point>(), Array.Empty<Route>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_point_in_an_unknown_section_is_refused() {
        // Arrange
        List<SectionId> anyIds = Any.ListOf(new AnySectionId()).WithCount(2).Distinct().Generate();
        TrackSection declared = new AnyTrackSection().WithId(anyIds[0]).Generate();
        Point lostPoint = new AnyPoint().WithSection(anyIds[1]).Generate();

        // Act
        Action construct = () => new Interlocking("Area north", new[] { declared }, new[] { lostPoint }, Array.Empty<Route>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_point_declared_twice_is_refused() {
        // Arrange
        SectionId anyId = new AnySectionId().Generate();
        string anyName = Any.String().WithLengthBetween(1, 20).NotBlank().Generate();
        TrackSection section = new AnyTrackSection().WithId(anyId).Generate();
        Point first = new AnyPoint().WithName(anyName).WithSection(anyId).Generate();
        Point second = new AnyPoint().WithName(anyName).WithSection(anyId).Generate();

        // Act
        Action construct = () => new Interlocking("Area north", new[] { section }, new[] { first, second }, Array.Empty<Route>());

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_over_an_unknown_section_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        List<TrackSection> withoutD = new[] { network.A, network.B, network.C, network.E }
                                      .Select(id => new AnyTrackSection().WithId(id).Generate()).ToList();
        Point point = new AnyPoint().WithName(network.PointName).WithSection(network.B).Generate();

        // Act: the layout lacks section D that Parallel runs over
        Action construct = () => new Interlocking("Area north", withoutD, new[] { point }, new[] { network.Parallel });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_setting_an_unknown_point_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        List<TrackSection> sections = new[] { network.A, network.B, network.C }
                                      .Select(id => new AnyTrackSection().WithId(id).Generate()).ToList();

        // Act: Main sets a point the layout does not declare
        Action construct = () => new Interlocking("Area north", sections, Array.Empty<Point>(), new[] { network.Main });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_setting_a_point_outside_its_own_sections_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        List<TrackSection> sections = new[] { network.A, network.B, network.C, network.D }
                                      .Select(id => new AnyTrackSection().WithId(id).Generate()).ToList();
        Point pointInD = new AnyPoint().WithName(network.PointName).WithSection(network.D).Generate();

        // Act: Main sets the point, but the point lies in D, which Main does not traverse
        Action construct = () => new Interlocking("Area north", sections, new[] { pointInD }, new[] { network.Main });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_route_declared_twice_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        TrackSection section = new AnyTrackSection().WithId(network.D).Generate();

        // Act
        Action construct = () => new Interlocking("Area north", new[] { section }, Array.Empty<Point>(), new[] { network.Parallel, network.Parallel });

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_lookup_of_an_unknown_section_or_point_is_refused() {
        // Arrange
        Network network = Network.Drawn();
        Interlocking interlocking = network.Build();
        SectionId anyOtherId = new AnySectionId().WithValue(v => v.DifferentFrom(network.A.Value)
                                                                  .DifferentFrom(network.B.Value)
                                                                  .DifferentFrom(network.C.Value)
                                                                  .DifferentFrom(network.D.Value)
                                                                  .DifferentFrom(network.E.Value)).Generate();

        // Act / Assert
        Assert.Throws<KeyNotFoundException>(() => interlocking.Section(anyOtherId));
        Assert.Throws<KeyNotFoundException>(() => interlocking.Point(network.PointName + "-unknown"));
    }

}
