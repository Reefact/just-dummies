using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Regular;

public sealed class RouteTests {

    [Fact, Reproducible]
    public void A_route_cannot_start_and_end_at_the_same_signal() {
        // Arrange
        string    anySignal = Any.StringMatching("S[0-9]{1,3}").Generate();
        AnyRoute  routeFromAndToTheSameSignal = Arbitrary.Route().WithEntrySignal(anySignal).WithExitSignal(anySignal);

        // Act
        Exception? refusal = Record.Exception(() => routeFromAndToTheSameSignal.Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void A_route_cannot_traverse_the_same_section_twice() {
        // Arrange
        SectionId anySection = Arbitrary.SectionId().Generate();
        AnyRoute  loopingRoute = Arbitrary.Route().WithSections([anySection, anySection]);

        // Act
        Exception? refusal = Record.Exception(() => loopingRoute.Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void A_route_cannot_set_the_same_point_twice() {
        // Arrange
        string   anyPointName = Any.String().Alpha().WithLengthBetween(1, 5).Generate();
        AnyRoute contradictoryRoute = Arbitrary.Route().WithPointSettings([
            new PointSetting(anyPointName, PointPosition.Normal),
            new PointSetting(anyPointName, PointPosition.Reverse),
        ]);

        // Act
        Exception? refusal = Record.Exception(() => contradictoryRoute.Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void Routes_sharing_a_section_conflict() {
        // Arrange
        List<SectionId> anyIds = Any.ListOf(Arbitrary.SectionId()).WithCount(3).Distinct().Generate();
        Route anyRoute      = Arbitrary.Route().WithSections([anyIds[0], anyIds[1]]).WithPointSettings([]).Generate();
        Route anyOtherRoute = Arbitrary.Route().WithSections([anyIds[1], anyIds[2]]).WithPointSettings([]).Generate();

        // Act
        bool conflicts = anyRoute.ConflictsWith(anyOtherRoute);

        // Assert
        Assert.True(conflicts);
    }

    [Fact, Reproducible]
    public void Routes_needing_a_shared_point_in_opposite_positions_conflict_even_over_different_sections() {
        // Arrange
        List<SectionId> anyIds = Any.ListOf(Arbitrary.SectionId()).WithCount(2).Distinct().Generate();
        Route normalRoute  = Arbitrary.Route().WithSections([anyIds[0]]).WithPointSettings([new PointSetting("P7", PointPosition.Normal)]).Generate();
        Route reverseRoute = Arbitrary.Route().WithSections([anyIds[1]]).WithPointSettings([new PointSetting("P7", PointPosition.Reverse)]).Generate();

        // Act
        bool conflicts = normalRoute.ConflictsWith(reverseRoute);

        // Assert
        Assert.True(conflicts);
    }

    [Fact, Reproducible]
    public void Routes_needing_a_shared_point_in_the_same_position_over_different_sections_are_compatible() {
        // Arrange
        List<SectionId>  anyIds      = Any.ListOf(Arbitrary.SectionId()).WithCount(2).Distinct().Generate();
        PointPosition    anyPosition = Any.Enum<PointPosition>().Generate();
        Route anyRoute      = Arbitrary.Route().WithSections([anyIds[0]]).WithPointSettings([new PointSetting("P7", anyPosition)]).Generate();
        Route anyOtherRoute = Arbitrary.Route().WithSections([anyIds[1]]).WithPointSettings([new PointSetting("P7", anyPosition)]).Generate();

        // Act
        bool conflicts = anyRoute.ConflictsWith(anyOtherRoute);

        // Assert
        Assert.False(conflicts);
    }

    [Fact, Reproducible]
    public void A_route_does_not_conflict_with_itself() {
        // Arrange
        Route anyRoute = Arbitrary.Route().Generate();

        // Act
        bool conflicts = anyRoute.ConflictsWith(anyRoute);

        // Assert
        Assert.False(conflicts);
    }

}
