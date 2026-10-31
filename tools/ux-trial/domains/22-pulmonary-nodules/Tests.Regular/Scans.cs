using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Regular;

/// <summary>Arrange helpers for the tracking scenarios: landmarks and centroids inside a thorax, and their shifted twins.</summary>
internal static class Scans {

    /// <summary>A position well inside the field of view, so that a plausible patient shift keeps it there.</summary>
    public static AnySpatialPoint AnyPointInThorax() {
        return Arbitrary.SpatialPoint()
                        .WithXMm(x => x.Between(-250.0, 250.0))
                        .WithYMm(y => y.Between(-250.0, 250.0))
                        .WithZMm(z => z.Between(-400.0, 400.0));
    }

    /// <summary>Landmarks of distinct kinds located anywhere inside the thorax.</summary>
    public static IReadOnlyList<AnatomicalLandmark> AnyLandmarks(int count) {
        return Any.SetOf(Any.Enum<LandmarkKind>())
                  .WithCount(count)
                  .Generate()
                  .Select(kind => new AnatomicalLandmark(kind, AnyPointInThorax().Generate()))
                  .ToList();
    }

    public static IReadOnlyList<AnatomicalLandmark> Shifted(IEnumerable<AnatomicalLandmark> landmarks, double dxMm, double dyMm, double dzMm) {
        return landmarks.Select(l => new AnatomicalLandmark(l.Kind, Shifted(l.Position, dxMm, dyMm, dzMm))).ToList();
    }

    public static SpatialPoint Shifted(SpatialPoint point, double dxMm, double dyMm, double dzMm) {
        return new SpatialPoint(point.XMm + dxMm, point.YMm + dyMm, point.ZMm + dzMm);
    }

    public static NoduleObservation ObservationOn(CtExam exam, SpatialPoint centroid) {
        return Arbitrary.NoduleObservation()
                        .WithExam(exam)
                        .WithSegmentation(s => s.WithCentroid(centroid))
                        .Generate();
    }

}
