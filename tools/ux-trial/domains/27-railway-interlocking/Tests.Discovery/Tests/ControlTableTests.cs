using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Discovery.Tests;

[Reproducible]
public sealed class ControlTableTests {

    [Fact]
    public void Routes_sharing_a_section_conflict() {
        // Arrange
        List<SectionId> anyIds   = Any.ListOf(Any.SectionId()).WithCount(3).Distinct().Generate();
        Route           anyFirst  = Any.Route().WithName("A").WithSections([anyIds[0], anyIds[1]]).WithPointSettings([]).Generate();
        Route           anySecond = Any.Route().WithName("B").WithSections([anyIds[1], anyIds[2]]).WithPointSettings([]).Generate();

        // Act
        bool conflicts = anyFirst.ConflictsWith(anySecond);

        // Assert
        Assert.True(conflicts);
    }

    [Fact]
    public void Routes_needing_the_same_point_in_opposite_positions_conflict() {
        // Arrange
        List<SectionId> anyIds       = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        string          anyPointName = Any.String().NotBlank().Generate();
        Route anyNormal  = Any.Route().WithName("A").WithSections([anyIds[0]]).WithPointSettings([new PointSetting(anyPointName, PointPosition.Normal)]).Generate();
        Route anyReverse = Any.Route().WithName("B").WithSections([anyIds[1]]).WithPointSettings([new PointSetting(anyPointName, PointPosition.Reverse)]).Generate();

        // Act
        bool conflicts = anyNormal.ConflictsWith(anyReverse);

        // Assert
        Assert.True(conflicts);
    }

    [Fact]
    public void Routes_needing_the_same_point_in_the_same_position_over_distinct_sections_do_not_conflict() {
        // Arrange
        List<SectionId> anyIds       = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        PointSetting    anySetting   = Any.PointSetting().Generate();
        Route anyFirst  = Any.Route().WithName("A").WithSections([anyIds[0]]).WithPointSettings([anySetting]).Generate();
        Route anySecond = Any.Route().WithName("B").WithSections([anyIds[1]]).WithPointSettings([anySetting]).Generate();

        // Act
        bool conflicts = anyFirst.ConflictsWith(anySecond);

        // Assert
        Assert.False(conflicts);
    }

    [Fact]
    public void A_route_cannot_start_and_end_at_the_same_signal() {
        // Arrange
        string anySignal = Any.String().NotBlank().Generate();
        AnyRoute anyRouteAtOneSignal = Any.Route().WithEntrySignal(anySignal).WithExitSignal(anySignal);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyRouteAtOneSignal.Generate());

        // Assert
        Assert.Equal("exitSignal", refusal.ParamName);
    }

    [Fact]
    public void A_route_cannot_traverse_a_section_twice() {
        // Arrange
        SectionId anySection = Any.SectionId().Generate();
        AnyRoute  anyLoop    = Any.Route().WithSections([anySection, anySection]);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => anyLoop.Generate());

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void An_interlocking_refuses_a_route_setting_a_point_outside_its_sections() {
        // Arrange
        List<SectionId> anyIds       = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        List<TrackSection> sections  = anyIds.Select(id => Any.TrackSection().WithId(id).Generate()).ToList();
        Point           anyPoint     = Any.Point().WithSection(anyIds[0]).Generate();
        Route           anyRoute     = Any.Route().WithSections([anyIds[1]]).WithPointSettings([new PointSetting(anyPoint.Name, PointPosition.Reverse)]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Interlocking(Any.String().NotBlank().Generate(), sections, [anyPoint], [anyRoute]));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void An_interlocking_refuses_a_point_lying_in_an_unknown_section() {
        // Arrange
        List<SectionId> anyIds   = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection    anyKnown = Any.TrackSection().WithId(anyIds[0]).Generate();
        Point           anyStray = Any.Point().WithSection(anyIds[1]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new Interlocking(Any.String().NotBlank().Generate(), [anyKnown], [anyStray], []));

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

}
