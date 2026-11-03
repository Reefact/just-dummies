using JustDummies;

namespace RailwayInterlocking.Domain.Tests;

/// <summary>
///     A small track layout drawn afresh for each test: five sections, one set of points lying in the
///     second, and three routes. Only the structure is meaningful (which route shares what with which);
///     every identifier is arbitrary.
///     <code>
///         Main     : A B C   points Normal
///         Siding   : A B E   points Reverse   (conflicts with Main: shares A and B, wants the points the other way)
///         Parallel : D                          (conflicts with nothing)
///     </code>
/// </summary>
internal sealed class Network {

    private Network(List<SectionId> ids, List<string> names, List<TrainId> trains, DateTimeOffset at) {
        A = ids[0];
        B = ids[1];
        C = ids[2];
        D = ids[3];
        E = ids[4];

        PointName = names[0];

        Main = new AnyRoute().WithName(names[1])
                             .WithSections(new[] { A, B, C })
                             .WithPointSettings(new[] { new PointSetting(PointName, PointPosition.Normal) })
                             .Generate();
        Siding = new AnyRoute().WithName(names[2])
                               .WithSections(new[] { A, B, E })
                               .WithPointSettings(new[] { new PointSetting(PointName, PointPosition.Reverse) })
                               .Generate();
        Parallel = new AnyRoute().WithName(names[3])
                                 .WithSections(new[] { D })
                                 .WithPointSettings(Array.Empty<PointSetting>())
                                 .Generate();

        FirstTrain  = trains[0];
        SecondTrain = trains[1];
        At          = at;
    }

    public SectionId A { get; }
    public SectionId B { get; }
    public SectionId C { get; }
    public SectionId D { get; }
    public SectionId E { get; }

    /// <summary>The set of points, lying in <see cref="B" />.</summary>
    public string PointName { get; }

    public Route Main     { get; }
    public Route Siding   { get; }
    public Route Parallel { get; }

    public TrainId FirstTrain  { get; }
    public TrainId SecondTrain { get; }

    public DateTimeOffset At { get; }

    public static Network Drawn() {
        List<SectionId> anyIds    = Any.ListOf(new AnySectionId()).WithCount(5).Distinct().Generate();
        List<string>    anyNames  = Any.ListOf(Any.String().WithLengthBetween(1, 30).NotBlank()).WithCount(4).Distinct().Generate();
        List<TrainId>   anyTrains = Any.ListOf(new AnyTrainId()).WithCount(2).Distinct().Generate();
        DateTimeOffset  anyAt     = Any.DateTimeOffset().Between(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                                 new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)).Generate();

        return new Network(anyIds, anyNames, anyTrains, anyAt);
    }

    /// <summary>A fresh interlocking over this layout. The set of points starts in the position Main does not want.</summary>
    public Interlocking Build() {
        List<TrackSection> sections = new[] { A, B, C, D, E }.Select(id => new AnyTrackSection().WithId(id).Generate()).ToList();
        List<Point> points = new() {
            new AnyPoint().WithName(PointName).WithSection(B).WithInitialPosition(PointPosition.Reverse).Generate()
        };

        return new Interlocking("Area north", sections, points, new[] { Main, Siding, Parallel });
    }

}
