using JustDummies;

using RailwayInterlocking.Domain;
using RailwayInterlocking.Tests.Expert.Arbitrary;

using Xunit;

namespace RailwayInterlocking.Tests.Expert.Tests;

public sealed class RouteTests {

    [Fact]
    public void A_route_cannot_start_and_end_at_the_same_signal() {
        // Arrange
        string                   anyName     = Recipes.RouteName.Generate();
        string                   anySignal   = Any.StringMatching("[A-Z]{1,2}[0-9]{1,3}").Generate();
        List<SectionId>          anySections = Any.ListOf(Any.SectionId()).NonEmpty().WithMaxCount(6).Distinct().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Route(anyName, anySignal, anySignal, anySections, []));

        // Assert
        Assert.Equal("exitSignal", refusal.ParamName);
    }

    [Fact]
    public void A_route_cannot_traverse_the_same_section_twice() {
        // Arrange
        SectionId anyRepeated = Any.SectionId().Generate();
        List<SectionId> anySectionsVisitingTwice = Any.ListOf(Any.SectionId())
                                                      .WithCountBetween(2, 6)
                                                      .Containing(anyRepeated)
                                                      .Generate();
        anySectionsVisitingTwice.Add(anyRepeated);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Route().WithSections(anySectionsVisitingTwice).Generate());

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_route_cannot_require_two_positions_of_the_same_point() {
        // Arrange
        string anyPointName = Any.PointSetting().Generate().PointName;
        PointSetting[] settingsOfOnePoint = [new(anyPointName, PointPosition.Normal), new(anyPointName, PointPosition.Reverse)];

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Route().WithPointSettings(settingsOfOnePoint).Generate());

        // Assert
        Assert.Equal("pointSettings", refusal.ParamName);
    }

    [Fact]
    public void Routes_sharing_a_section_conflict() {
        // Arrange
        SectionId anyShared     = Any.SectionId().Generate();
        (string anyName, string anyOtherName) = TwoRouteNames();
        Route     anyRoute      = Any.Route().WithName(anyName).WithSections(s => s.Containing(anyShared)).WithPointSettings([]).Generate();
        Route     anyCrossing   = Any.Route().WithName(anyOtherName).WithSections(s => s.Containing(anyShared)).WithPointSettings([]).Generate();

        // Act
        bool conflict = anyRoute.ConflictsWith(anyCrossing);

        // Assert
        Assert.True(conflict);
    }

    [Fact]
    public void Routes_over_different_sections_needing_a_point_in_opposite_positions_conflict() {
        // Arrange
        (IReadOnlyList<SectionId> anySections, IReadOnlyList<SectionId> anyOtherSections) = TwoDisjointSectionRuns();
        (string anyName, string anyOtherName) = TwoRouteNames();
        string anyPointName = Any.PointSetting().Generate().PointName;

        Route normal  = Any.Route().WithName(anyName).WithSections(anySections).WithPointSettings([new PointSetting(anyPointName, PointPosition.Normal)]).Generate();
        Route reverse = Any.Route().WithName(anyOtherName).WithSections(anyOtherSections).WithPointSettings([new PointSetting(anyPointName, PointPosition.Reverse)]).Generate();

        // Act
        bool conflict = normal.ConflictsWith(reverse);

        // Assert
        Assert.True(conflict);
    }

    [Fact]
    public void Routes_over_different_sections_needing_a_point_in_the_same_position_do_not_conflict() {
        // Arrange
        (IReadOnlyList<SectionId> anySections, IReadOnlyList<SectionId> anyOtherSections) = TwoDisjointSectionRuns();
        (string anyName, string anyOtherName) = TwoRouteNames();
        PointSetting anySetting = Any.PointSetting().Generate();

        Route anyRoute      = Any.Route().WithName(anyName).WithSections(anySections).WithPointSettings([anySetting]).Generate();
        Route anyOtherRoute = Any.Route().WithName(anyOtherName).WithSections(anyOtherSections).WithPointSettings([anySetting]).Generate();

        // Act
        bool conflict = anyRoute.ConflictsWith(anyOtherRoute);

        // Assert
        Assert.False(conflict);
    }

    [Fact]
    public void A_route_does_not_conflict_with_itself() {
        // Arrange
        Route anyRoute = Any.Route().Generate();

        // Act
        bool conflict = anyRoute.ConflictsWith(anyRoute);

        // Assert
        Assert.False(conflict);
    }

    private static (string, string) TwoRouteNames() {
        List<string> names = Any.ListOf(Recipes.RouteName).WithCount(2).Distinct().Generate();

        return (names[0], names[1]);
    }

    // Two runs of track with no section in common: drawn as one run of distinct sections, then split.
    private static (IReadOnlyList<SectionId>, IReadOnlyList<SectionId>) TwoDisjointSectionRuns() {
        List<SectionId> sections = Any.ListOf(Any.SectionId()).WithCountBetween(2, 8).Distinct().Generate();
        int             split    = Any.Int32().Between(1, sections.Count - 1).Generate();

        return (sections[..split], sections[split..]);
    }

}
