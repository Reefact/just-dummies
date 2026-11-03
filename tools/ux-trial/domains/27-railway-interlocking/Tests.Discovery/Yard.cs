using System.Text;

using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

/// <summary>
///     The small junction every interlocking test works on. Its topology is what the tests are about, so ids,
///     route names and point names are literals; lengths, speeds, signals and area name are arbitrary.
///
///         A1 --- J1[P1] --- M1        MAIN   : A1 J1 M1, P1 normal
///                  \------- B1        BRANCH : A1 J1 B1, P1 reverse
///         A2 ------------- B1         SIDING : A2 B1
///         C1                          YARD   : C1
/// </summary>
internal static class Yard {

    public static readonly SectionId A1 = new SectionId("A1");
    public static readonly SectionId A2 = new SectionId("A2");
    public static readonly SectionId J1 = new SectionId("J1");
    public static readonly SectionId M1 = new SectionId("M1");
    public static readonly SectionId B1 = new SectionId("B1");
    public static readonly SectionId C1 = new SectionId("C1");

    public const string Main    = "MAIN";
    public const string Branch  = "BRANCH";
    public const string Siding  = "SIDING";
    public const string YardRun = "YARD";

    public static List<TrackSection> Sections() {
        return new List<TrackSection> {
            Any.TrackSection().WithId(A1).Generate(),
            Any.TrackSection().WithId(A2).Generate(),
            Any.TrackSection().WithId(J1).Generate(),
            Any.TrackSection().WithId(M1).Generate(),
            Any.TrackSection().WithId(B1).Generate(),
            Any.TrackSection().WithId(C1).Generate()
        };
    }

    public static Point JunctionPoint(PointPosition initialPosition) {
        return Any.Point().WithName("P1").WithSection(J1).WithInitialPosition(initialPosition).Generate();
    }

    public static Route MainRoute() {
        return Any.Route()
                  .WithName(Main)
                  .WithSections(new[] { A1, J1, M1 })
                  .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Normal) })
                  .Generate();
    }

    public static Route BranchRoute() {
        return Any.Route()
                  .WithName(Branch)
                  .WithSections(new[] { A1, J1, B1 })
                  .WithPointSettings(new[] { new PointSetting("P1", PointPosition.Reverse) })
                  .Generate();
    }

    public static Route SidingRoute() {
        return Any.Route()
                  .WithName(Siding)
                  .WithSections(new[] { A2, B1 })
                  .WithPointSettings(Array.Empty<PointSetting>())
                  .Generate();
    }

    public static Route YardRoute() {
        return Any.Route()
                  .WithName(YardRun)
                  .WithSections(new[] { C1 })
                  .WithPointSettings(Array.Empty<PointSetting>())
                  .Generate();
    }

    public static Interlocking Build(PointPosition initialPosition = PointPosition.Reverse) {
        string anyAreaName = Any.String().NotBlank().Generate();

        return new Interlocking(anyAreaName,
                                Sections(),
                                new[] { JunctionPoint(initialPosition) },
                                new[] { MainRoute(), BranchRoute(), SidingRoute(), YardRoute() });
    }

    public static DateTimeOffset AnyInstant() {
        return Any.DateTimeOffset().DifferentFrom(default).Generate();
    }

    public static TrainId AnyTrain() {
        return Any.TrainId().Generate();
    }

    public static TrainId AnyTrainOtherThan(TrainId other) {
        return Any.TrainId().DifferentFrom(other).Generate();
    }

    /// <summary>Everything a safety decision can see, as text, to compare before and after a refused request.</summary>
    public static string Snapshot(Interlocking interlocking) {
        StringBuilder text = new StringBuilder();

        foreach (SectionId id in new[] { A1, A2, J1, M1, B1, C1 }) {
            TrackSection section = interlocking.Section(id);
            text.Append($"{id}:occupied={section.IsOccupied},lock={section.LockedByRoute ?? "-"};");
        }

        Point point = interlocking.Point("P1");
        text.Append($"P1:{point.Position},lock={point.LockedByRoute ?? "-"};");
        text.Append("active=").Append(string.Join(",", interlocking.ActiveRoutes.Select(l => l.Route.Name).OrderBy(n => n, StringComparer.Ordinal)));

        return text.ToString();
    }

    public static InterlockingRefusedException AssertRefused(RefusalReason reason, Action action) {
        InterlockingRefusedException refusal = Assert.Throws<InterlockingRefusedException>(action);
        Assert.Equal(reason, refusal.Reason);

        return refusal;
    }

}
