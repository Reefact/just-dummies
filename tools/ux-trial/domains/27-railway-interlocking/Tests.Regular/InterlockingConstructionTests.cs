using JustDummies;
using JustDummies.Xunit;

using RailwayInterlocking.Domain;

using Xunit;

namespace RailwayInterlocking.Tests.Regular;

public sealed class InterlockingConstructionTests {

    private static readonly AnyString AnyAreaName = Any.String().Alpha().WithLengthBetween(3, 20);

    [Fact, Reproducible]
    public void A_point_lying_in_an_unknown_section_is_refused() {
        // Arrange
        List<SectionId> anyIds     = Any.ListOf(Arbitrary.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection    anySection = Arbitrary.TrackSection().WithId(anyIds[0]).Generate();
        Point           strayPoint = Arbitrary.Point().WithSection(anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Interlocking(AnyAreaName.Generate(), [anySection], [strayPoint], []));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void A_section_declared_twice_is_refused() {
        // Arrange
        SectionId    anyId     = Arbitrary.SectionId().Generate();
        TrackSection anySection = Arbitrary.TrackSection().WithId(anyId).Generate();
        TrackSection sameSectionAgain = Arbitrary.TrackSection().WithId(anyId).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Interlocking(AnyAreaName.Generate(), [anySection, sameSectionAgain], [], []));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact, Reproducible]
    public void A_route_setting_a_point_outside_its_own_sections_is_refused() {
        // Arrange
        List<SectionId> anyIds   = Any.ListOf(Arbitrary.SectionId()).WithCount(2).Distinct().Generate();
        TrackSection[]  sections = [.. anyIds.Select(id => Arbitrary.TrackSection().WithId(id).Generate())];
        Point           anyPoint = Arbitrary.Point().WithName("P1").WithSection(anyIds[1]).Generate();
        Route           routeReachingOver = Arbitrary.Route()
                                                    .WithSections([anyIds[0]])
                                                    .WithPointSettings(p => p.WithCount(1).Containing(new PointSetting("P1", PointPosition.Normal)))
                                                    .Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Interlocking(AnyAreaName.Generate(), sections, [anyPoint], [routeReachingOver]));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

}
