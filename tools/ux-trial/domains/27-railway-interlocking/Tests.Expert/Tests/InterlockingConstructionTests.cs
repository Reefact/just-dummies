using JustDummies;

using RailwayInterlocking.Domain;
using RailwayInterlocking.Tests.Expert.Arbitrary;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

/// <summary>The control tables are checked when the interlocking is commissioned, not when a train asks.</summary>
public sealed class InterlockingConstructionTests {

    [Fact]
    public void A_point_lying_in_an_undeclared_section_is_refused() {
        // Arrange
        List<SectionId> anyIds        = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection    anyDeclared   = Any.TrackSection().WithId(anyIds[0]).Generate();
        Point           anyStrayPoint = Any.Point().WithSection(anyIds[1]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(Recipes.AreaName.Generate(), [anyDeclared], [anyStrayPoint], []));

        // Assert
        Assert.Equal("points", refusal.ParamName);
    }

    [Fact]
    public void A_route_over_an_undeclared_section_is_refused() {
        // Arrange
        List<SectionId> anyIds       = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection    anyDeclared  = Any.TrackSection().WithId(anyIds[0]).Generate();
        Route           anyStrayRoute = Any.Route().WithSections([anyIds[0], anyIds[1]]).WithPointSettings([]).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(Recipes.AreaName.Generate(), [anyDeclared], [], [anyStrayRoute]));

        // Assert
        Assert.Equal("routes", refusal.ParamName);
    }

    [Fact]
    public void A_route_setting_a_point_outside_its_own_sections_is_refused() {
        // Arrange
        List<SectionId> anyIds     = Any.ListOf(Any.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection[]  anyTrack   = [Any.TrackSection().WithId(anyIds[0]).Generate(), Any.TrackSection().WithId(anyIds[1]).Generate()];
        Point           anyPoint   = Any.Point().WithSection(anyIds[1]).Generate();
        Route           anyOverreachingRoute = Any.Route()
                                                 .WithSections([anyIds[0]])
                                                 .WithPointSettings([Any.PointSetting().WithPointName(anyPoint.Name).Generate()])
                                                 .Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(Recipes.AreaName.Generate(), anyTrack, [anyPoint], [anyOverreachingRoute]));

        // Assert
        Assert.Contains("outside its sections", refusal.Message);
    }

    [Fact]
    public void A_track_section_declared_twice_is_refused() {
        // Arrange
        SectionId    anyId     = Any.SectionId().Generate();
        TrackSection anySection = Any.TrackSection().WithId(anyId).Generate();
        TrackSection anyTwin    = Any.TrackSection().WithId(anyId).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Interlocking(Recipes.AreaName.Generate(), [anySection, anyTwin], [], []));

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

}
