using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Expert.Arbitrary;

/// <summary>
///     A simple junction, the smallest area where routes compete for track:
///     <code>
///         Approach ── Junction ─┬─ Main      MainRoute   : Approach, Junction, Main   (point Normal)
///                               └─ Branch    BranchRoute : Approach, Junction, Branch (point Reverse)
///         Siding                             SidingRoute : Siding                     (independent)
///     </code>
///     The topology is what the tests reason about, so it is fixed. Every identifier, name, length,
///     speed and the point's initial lie are arbitrary: no rule of the interlocking may depend on them.
/// </summary>
public sealed class JunctionLayout {

    public required Interlocking Interlocking  { get; init; }
    public required SectionId    Approach      { get; init; }
    public required SectionId    Junction      { get; init; }
    public required SectionId    Main          { get; init; }
    public required SectionId    Branch        { get; init; }
    public required SectionId    Siding        { get; init; }
    public required string       JunctionPoint { get; init; }
    public required string       MainRoute     { get; init; }
    public required string       BranchRoute   { get; init; }
    public required string       SidingRoute   { get; init; }

    public TrackSection SectionAt(SectionId id) => Interlocking.Section(id);

    public Point Point => Interlocking.Point(JunctionPoint);

}

public sealed class AnyJunctionLayout : IAny<JunctionLayout> {

    public JunctionLayout Generate() {
        List<SectionId> ids        = Any.ListOf(Any.SectionId()).WithCount(5).Distinct().Generate();
        List<string>    routeNames = Any.ListOf(Recipes.RouteName).WithCount(3).Distinct().Generate();
        (SectionId approach, SectionId junction, SectionId main, SectionId branch, SectionId siding) = (ids[0], ids[1], ids[2], ids[3], ids[4]);

        Point point = Any.Point().WithSection(junction).Generate();

        Route mainRoute = Any.Route()
                             .WithName(routeNames[0])
                             .WithSections([approach, junction, main])
                             .WithPointSettings([new PointSetting(point.Name, PointPosition.Normal)])
                             .Generate();
        Route branchRoute = Any.Route()
                               .WithName(routeNames[1])
                               .WithSections([approach, junction, branch])
                               .WithPointSettings([new PointSetting(point.Name, PointPosition.Reverse)])
                               .Generate();
        Route sidingRoute = Any.Route()
                               .WithName(routeNames[2])
                               .WithSections([siding])
                               .WithPointSettings([])
                               .Generate();

        Interlocking interlocking = new(Recipes.AreaName.Generate(),
                                        ids.Select(id => Any.TrackSection().WithId(id).Generate()),
                                        [point],
                                        [mainRoute, branchRoute, sidingRoute]);

        return new JunctionLayout {
            Interlocking  = interlocking,
            Approach      = approach,
            Junction      = junction,
            Main          = main,
            Branch        = branch,
            Siding        = siding,
            JunctionPoint = point.Name,
            MainRoute     = mainRoute.Name,
            BranchRoute   = branchRoute.Name,
            SidingRoute   = sidingRoute.Name,
        };
    }

}
