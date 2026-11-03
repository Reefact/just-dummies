using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;

namespace PulmonaryNodules.Tests.Expert.Support;

/// <summary>
///     Builders for the tracking scenes: exams whose landmarks are whole millimetres, so that a registration
///     shift is exact and a test can sit exactly on the match tolerance.
/// </summary>
internal static class Scans {

    private static readonly IAny<SpatialPoint> WholeMillimetrePoints = Any.Combine(Any.Int32().Between(-1000, 1000),
                                                                                  Any.Int32().Between(-1000, 1000),
                                                                                  Any.Int32().Between(-1000, 1000),
                                                                                  (x, y, z) => new SpatialPoint(x, y, z));

    /// <summary>A date from which the tests can move forward by days without leaving the calendar.</summary>
    public static AnyDateOnly AnyScanDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2030, 12, 31));
    }

    /// <summary>Landmarks of <paramref name="count" /> distinct kinds, at whole-millimetre positions.</summary>
    public static IReadOnlyCollection<AnatomicalLandmark> AnyLandmarks(int count) {
        Dictionary<LandmarkKind, SpatialPoint> positions = Any.DictionaryOf(Any.Enum<LandmarkKind>(), WholeMillimetrePoints)
                                                              .WithCount(count)
                                                              .Generate();

        return positions.Select(p => new AnatomicalLandmark(p.Key, p.Value)).ToList();
    }

    /// <summary>The same landmarks, each moved by its own displacement along x, in the order given.</summary>
    public static IReadOnlyCollection<AnatomicalLandmark> DisplacedAlongX(IReadOnlyCollection<AnatomicalLandmark> landmarks, params double[] displacementsMm) {
        return landmarks.Zip(displacementsMm, (landmark, dx) => Moved(landmark, dx, 0.0, 0.0)).ToList();
    }

    /// <summary>The same landmarks, all moved by the same shift.</summary>
    public static IReadOnlyCollection<AnatomicalLandmark> Shifted(IReadOnlyCollection<AnatomicalLandmark> landmarks, double dxMm, double dyMm, double dzMm) {
        return landmarks.Select(landmark => Moved(landmark, dxMm, dyMm, dzMm)).ToList();
    }

    /// <summary>A point moved by a shift.</summary>
    public static SpatialPoint Moved(SpatialPoint point, double dxMm, double dyMm, double dzMm) {
        return new SpatialPoint(point.XMm + dxMm, point.YMm + dyMm, point.ZMm + dzMm);
    }

    private static AnatomicalLandmark Moved(AnatomicalLandmark landmark, double dxMm, double dyMm, double dzMm) {
        return new AnatomicalLandmark(landmark.Kind, Moved(landmark.Position, dxMm, dyMm, dzMm));
    }

    /// <summary>A whole-millimetre point that a shift of up to 300 mm cannot push out of the field of view.</summary>
    public static SpatialPoint AnyCentroid() {
        return WholeMillimetrePoints.Generate();
    }

    /// <summary>An observation made on the given day, on an exam carrying these landmarks, with the nodule at <paramref name="centroid" />.</summary>
    public static NoduleObservation ObservationOn(DateOnly day, IReadOnlyCollection<AnatomicalLandmark> landmarks, SpatialPoint centroid) {
        return Any.NoduleObservation()
                  .WithExam(exam => exam.WithAcquiredOn(day).WithLandmarks(landmarks))
                  .WithSegmentation(segmentation => segmentation.WithCentroid(centroid))
                  .Generate();
    }

    /// <summary>An observation like <see cref="ObservationOn" />, whose segmentation holds exactly <paramref name="voxelCount" /> voxels of 1 mm3.</summary>
    public static NoduleObservation ObservationOfVoxels(int voxelCount, DateOnly day, IReadOnlyCollection<AnatomicalLandmark> landmarks, SpatialPoint centroid) {
        VoxelSpacing unitCube = new VoxelSpacing(1.0, 1.0, 1.0);

        return Any.NoduleObservation()
                  .WithExam(exam => exam.WithAcquiredOn(day).WithLandmarks(landmarks).WithSpacing(unitCube))
                  .WithSegmentation(segmentation => segmentation.WithCentroid(centroid).WithSpacing(unitCube).WithVoxelCount(voxelCount))
                  .Generate();
    }

}
