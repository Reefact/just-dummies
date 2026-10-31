using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Regular;

/// <summary>
/// A small double junction used by the interlocking tests. Its topology is what the tests are about
/// (which route shares what with which), so the route table is written out; the identifiers, lengths,
/// line speeds and area name are arbitrary.
/// <code>
///   Approach -- Junction(P1) --N-- Main
///                            \--R-- Branch          UpLine (independent)
/// </code>
/// </summary>
internal sealed class Junction {

    public const string MainRoute   = "MAIN";
    public const string BranchRoute = "BRANCH";
    public const string ShuntRoute  = "SHUNT";
    public const string UpRoute     = "UP";
    public const string PointName   = "P1";

    public Junction() {
        List<SectionId> anyIds = Any.ListOf(Arbitrary.SectionId()).WithCount(5).Distinct().Generate();
        Approach = anyIds[0];
        Middle   = anyIds[1];
        Main     = anyIds[2];
        Branch   = anyIds[3];
        UpLine   = anyIds[4];

        IEnumerable<TrackSection> sections = anyIds.Select(id => Arbitrary.TrackSection().WithId(id).Generate());
        Point[] points = [Arbitrary.Point().WithName(PointName).WithSection(Middle).Generate()];
        Route[] routes = [
            Arbitrary.Route().WithName(MainRoute).WithSections([Approach, Middle, Main]).WithPointSettings([new PointSetting(PointName, PointPosition.Normal)]).Generate(),
            Arbitrary.Route().WithName(BranchRoute).WithSections([Approach, Middle, Branch]).WithPointSettings([new PointSetting(PointName, PointPosition.Reverse)]).Generate(),
            Arbitrary.Route().WithName(ShuntRoute).WithSections([Main]).WithPointSettings([]).Generate(),
            Arbitrary.Route().WithName(UpRoute).WithSections([UpLine]).WithPointSettings([]).Generate(),
        ];

        Interlocking = new Interlocking(Any.String().Alpha().WithLengthBetween(3, 20).Generate(), sections, points, routes);
    }

    public SectionId Approach { get; }
    public SectionId Middle   { get; }
    public SectionId Main     { get; }
    public SectionId Branch   { get; }
    public SectionId UpLine   { get; }

    public Interlocking Interlocking { get; }

    public Point Points => Interlocking.Point(PointName);

    /// <summary>Drives a train through the given sections: each is occupied, then the one behind it clears.</summary>
    public void Run(params SectionId[] path) {
        for (int i = 0; i < path.Length; i++) {
            Interlocking.OnSectionOccupied(path[i]);
            if (i > 0) {
                Interlocking.OnSectionCleared(path[i - 1]);
            }
        }

        Interlocking.OnSectionCleared(path[^1]);
    }

}
