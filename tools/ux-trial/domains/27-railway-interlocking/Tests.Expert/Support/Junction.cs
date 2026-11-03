using RailwayInterlocking.Domain.Tests.Expert.Generators;

namespace RailwayInterlocking.Domain.Tests.Expert.Support;

/// <summary>
///     A small layout every aggregate test starts from. Its shape is what the tests are about; every name in it is
///     arbitrary and drawn.
///
///     <code>
///                          ┌─ Main      (ToMain:   Approach → Throat → Main,   point Normal)
///         Approach ─ Throat┤
///                          └─ Siding    (ToSiding: Approach → Throat → Siding, point Reverse)
///
///         Goods                          (Goods:    a separate track)
///     </code>
///
///     The points lie in <see cref="Throat" />. ToMain and ToSiding share Approach and Throat; Goods shares nothing.
/// </summary>
internal sealed class Junction {

    private Junction(IReadOnlyList<SectionId> ids, IReadOnlyList<string> names, PointPosition pointInitially) {
        Approach = ids[0];
        Throat   = ids[1];
        Main     = ids[2];
        Siding   = ids[3];
        Yard     = ids[4];

        PointName = names[0];
        ToMain    = AnyRouteOver(names[1], [Approach, Throat, Main], [new PointSetting(PointName, PointPosition.Normal)]);
        ToSiding  = AnyRouteOver(names[2], [Approach, Throat, Siding], [new PointSetting(PointName, PointPosition.Reverse)]);
        Goods     = AnyRouteOver(names[3], [Yard], []);

        Sections = ids.Select(id => Any.TrackSection().WithId(id).Generate()).ToList();
        Points   = [Any.Point().WithName(PointName).WithSection(Throat).WithInitialPosition(pointInitially).Generate()];
        Routes   = [ToMain, ToSiding, Goods];

        Interlocking = new Interlocking(Any.String().NotBlank().Generate(), Sections, Points, Routes);
    }

    public SectionId Approach { get; }
    public SectionId Throat   { get; }
    public SectionId Main     { get; }
    public SectionId Siding   { get; }
    public SectionId Yard     { get; }

    public string PointName { get; }

    public Route ToMain   { get; }
    public Route ToSiding { get; }
    public Route Goods    { get; }

    public IReadOnlyList<TrackSection> Sections { get; }
    public IReadOnlyList<Point>        Points   { get; }
    public IReadOnlyList<Route>        Routes   { get; }

    public Interlocking Interlocking { get; }

    /// <summary>A layout whose points start in a position the test does not care about.</summary>
    public static Junction Build() {
        return Build(Any.Enum<PointPosition>().Generate());
    }

    /// <summary>A layout whose points start in the position the test is about.</summary>
    public static Junction Build(PointPosition pointInitially) {
        List<SectionId> ids   = Any.ListOf(new AnySectionId()).WithCount(5).Distinct().Generate();
        List<string>    names = Any.ListOf(AnyRoute.ValidName()).WithCount(4).Distinct().Generate();

        return new Junction(ids, names, pointInitially);
    }

    /// <summary>A route name the layout does not know.</summary>
    public string AnyUnknownRouteName() {
        return AnyRoute.ValidName().Except(Routes.Select(route => route.Name)).Generate();
    }

    private static Route AnyRouteOver(string name, SectionId[] sections, PointSetting[] settings) {
        return Any.Route().WithName(name).WithSections(sections).WithPointSettings(settings).Generate();
    }

}
