using RailwayInterlocking.Domain.Tests.Expert.Support;

namespace RailwayInterlocking.Domain.Tests.Expert;

public sealed class InterlockingConstructionTests {

    [Fact]
    public void An_interlocking_knows_its_area_its_sections_and_its_points_and_has_no_route_set() {
        Junction junction = Junction.Build();

        Interlocking interlocking = new Interlocking("North Junction", junction.Sections, junction.Points, junction.Routes);

        Assert.Equal("North Junction", interlocking.AreaName);
        Assert.Same(junction.Sections[0], interlocking.Section(junction.Approach));
        Assert.Same(junction.Points[0], interlocking.Point(junction.PointName));
        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Fact]
    public void An_interlocking_may_have_no_point_and_no_route() {
        Junction junction = Junction.Build();

        Interlocking interlocking = new Interlocking(junction.Interlocking.AreaName, junction.Sections, [], []);

        Assert.Empty(interlocking.ActiveRoutes);
    }

    [Fact]
    public void An_unknown_section_or_point_is_a_missing_element_not_a_refusal_to_set_a_route() {
        Junction     junction     = Junction.Build();
        SectionId    anyUnknownId = Any.SectionId().WithValue(value => value.DifferentFrom(junction.Approach.Value)).Generate();

        Assert.Throws<KeyNotFoundException>(() => junction.Interlocking.Section(anyUnknownId));
        Assert.Throws<KeyNotFoundException>(() => junction.Interlocking.Point(AnyPoint.ValidName().Except(junction.PointName).Generate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t\n")]
    public void An_interlocking_needs_an_area_name(string blankName) {
        Junction junction = Junction.Build();

        Assert.Throws<ArgumentException>(() => new Interlocking(blankName, junction.Sections, junction.Points, junction.Routes));
    }

    [Fact]
    public void An_interlocking_needs_each_of_its_collections() {
        Junction junction = Junction.Build();
        string   anyArea  = junction.Interlocking.AreaName;

        Assert.Throws<ArgumentNullException>(() => new Interlocking(null!, junction.Sections, junction.Points, junction.Routes));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, null!, junction.Points, junction.Routes));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, junction.Sections, null!, junction.Routes));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, junction.Sections, junction.Points, null!));
    }

    [Fact]
    public void An_interlocking_refuses_a_missing_section_point_or_route() {
        Junction junction = Junction.Build();
        string   anyArea  = junction.Interlocking.AreaName;

        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, [.. junction.Sections, null!], junction.Points, junction.Routes));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, junction.Sections, [.. junction.Points, null!], junction.Routes));
        Assert.Throws<ArgumentNullException>(() => new Interlocking(anyArea, junction.Sections, junction.Points, [.. junction.Routes, null!]));
    }

    [Fact]
    public void An_interlocking_refuses_a_section_declared_twice() {
        Junction     junction = Junction.Build();
        TrackSection twin     = Any.TrackSection().WithId(junction.Approach).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, [.. junction.Sections, twin], junction.Points, junction.Routes));
    }

    [Fact]
    public void An_interlocking_refuses_a_point_lying_in_an_unknown_section() {
        Junction  junction = Junction.Build();
        SectionId unknown  = Any.SectionId().WithValue(value => value.Except(junction.Sections.Select(section => section.Id.Value))).Generate();
        Point     stray    = Any.Point().WithSection(unknown).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, [stray], []));
    }

    [Fact]
    public void An_interlocking_refuses_a_point_name_declared_twice() {
        Junction junction = Junction.Build();
        Point    twin     = Any.Point().WithName(junction.PointName).WithSection(junction.Throat).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, [.. junction.Points, twin], junction.Routes));
    }

    [Fact]
    public void An_interlocking_refuses_a_route_over_an_unknown_section() {
        Junction  junction = Junction.Build();
        SectionId unknown  = Any.SectionId().WithValue(value => value.Except(junction.Sections.Select(section => section.Id.Value))).Generate();
        Route     stray    = Any.Route().WithSections([junction.Yard, unknown]).WithPointSettings([]).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, junction.Points, [stray]));
    }

    [Fact]
    public void An_interlocking_refuses_a_route_setting_an_unknown_point() {
        Junction junction = Junction.Build();
        Route    stray    = Any.Route().WithSections([junction.Throat]).WithPointSettings([Any.PointSetting().WithPointName(name => name.Except(junction.PointName)).Generate()]).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, junction.Points, [stray]));
    }

    [Fact]
    public void An_interlocking_refuses_a_route_setting_a_point_that_lies_outside_its_sections() {
        Junction junction = Junction.Build();
        Route    stray    = Any.Route().WithSections([junction.Yard]).WithPointSettings([new PointSetting(junction.PointName, PointPosition.Normal)]).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, junction.Points, [stray]));
    }

    [Fact]
    public void An_interlocking_refuses_a_route_name_declared_twice() {
        Junction junction = Junction.Build();
        Route    twin     = Any.Route().WithName(junction.Goods.Name).WithSections([junction.Yard]).WithPointSettings([]).Generate();

        Assert.Throws<ArgumentException>(() => new Interlocking(junction.Interlocking.AreaName, junction.Sections, junction.Points, [.. junction.Routes, twin]));
    }

}
