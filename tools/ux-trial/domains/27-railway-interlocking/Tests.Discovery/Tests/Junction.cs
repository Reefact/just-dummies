using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery.Tests;

/// <summary>
///     A small, drawn layout: an approach section, a junction section holding one set of points,
///     then a main line and a branch beyond it, plus an independent siding elsewhere in the area.
///     Two routes leave the approach (one per point position) and a third covers the siding.
///     The topology is what the tests rely on; every name, id and dimension in it is a dummy.
/// </summary>
internal sealed class Junction {

    public Junction() {
        List<SectionId> anyIds = Any.ListOf(Any.SectionId()).WithCount(5).Distinct().Generate();
        Approach = anyIds[0];
        Middle   = anyIds[1];
        Main     = anyIds[2];
        Branch   = anyIds[3];
        Siding   = anyIds[4];

        List<string> anyRouteNames = Any.ListOf(Any.String().NotBlank().WithLengthBetween(1, 12)).WithCount(3).Distinct().Generate();
        PointName = Any.String().NotBlank().WithLengthBetween(1, 8).Generate();

        ToMain   = Any.Route().WithName(anyRouteNames[0]).WithSections([Approach, Middle, Main]).WithPointSettings([new PointSetting(PointName, PointPosition.Normal)]).Generate();
        ToBranch = Any.Route().WithName(anyRouteNames[1]).WithSections([Approach, Middle, Branch]).WithPointSettings([new PointSetting(PointName, PointPosition.Reverse)]).Generate();
        ToSiding = Any.Route().WithName(anyRouteNames[2]).WithSections([Siding]).WithPointSettings([]).Generate();

        List<TrackSection> sections = anyIds.Select(id => Any.TrackSection().WithId(id).Generate()).ToList();
        Point point = Any.Point().WithName(PointName).WithSection(Middle).WithInitialPosition(PointPosition.Normal).Generate();

        Interlocking = new Interlocking(Any.String().NotBlank().Generate(), sections, [point], [ToMain, ToBranch, ToSiding]);
    }

    public SectionId Approach { get; }
    public SectionId Middle   { get; }
    public SectionId Main     { get; }
    public SectionId Branch   { get; }
    public SectionId Siding   { get; }

    public string PointName { get; }

    public Route ToMain   { get; }
    public Route ToBranch { get; }
    public Route ToSiding { get; }

    public Interlocking Interlocking { get; }

    public static DateTimeOffset AnyTime() {
        // A lock time must be set: the default instant is the only one the domain refuses.
        return Any.DateTimeOffset().DifferentFrom(default(DateTimeOffset)).Generate();
    }

}
