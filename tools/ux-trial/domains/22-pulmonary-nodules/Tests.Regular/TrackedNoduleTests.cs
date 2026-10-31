using JustDummies;
using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class TrackedNoduleTests {

    [Fact]
    public void A_follow_up_segmentation_at_the_registered_position_is_recognised_as_the_same_nodule() {
        // Arrange
        (CtExam anyBaselineExam, CtExam anyFollowUpExam) = AnyExamPair(dxMm: 12.0, dyMm: -7.0, dzMm: 20.0, daysApart: 90);
        SpatialPoint  anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule nodule      = AnyNoduleFrom(Scans.ObservationOn(anyBaselineExam, anyCentroid), toleranceMm: 10.0);
        // The patient moved by the registered shift, and the nodule's centroid by 5 mm more (3-4-5).
        NoduleObservation followUp = Scans.ObservationOn(anyFollowUpExam, Scans.Shifted(anyCentroid, 12.0 + 3.0, -7.0 + 4.0, 20.0));

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(5.0, distance, 6);
        Assert.Same(followUp, nodule.Latest);
        Assert.Equal(2, nodule.Observations.Count);
    }

    [Fact]
    public void A_segmentation_beyond_the_match_tolerance_is_not_the_tracked_nodule() {
        // Arrange
        (CtExam anyBaselineExam, CtExam anyFollowUpExam) = AnyExamPair(dxMm: 0.0, dyMm: 0.0, dzMm: 0.0, daysApart: 180);
        SpatialPoint  anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule nodule      = AnyNoduleFrom(Scans.ObservationOn(anyBaselineExam, anyCentroid), toleranceMm: 5.0);
        NoduleObservation elsewhere = Scans.ObservationOn(anyFollowUpExam, Scans.Shifted(anyCentroid, 0.0, 0.0, 30.0));

        // Act
        Assert.Throws<NoduleMismatchException>(() => nodule.Track(elsewhere));

        // Assert
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void An_exam_acquired_before_the_latest_one_is_refused() {
        // Arrange
        (CtExam anyBaselineExam, CtExam anyEarlierExam) = AnyExamPair(dxMm: 0.0, dyMm: 0.0, dzMm: 0.0, daysApart: -30);
        SpatialPoint  anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule nodule      = AnyNoduleFrom(Scans.ObservationOn(anyBaselineExam, anyCentroid), toleranceMm: 10.0);

        // Act
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => nodule.Track(Scans.ObservationOn(anyEarlierExam, anyCentroid)));

        // Assert
        Assert.Contains("not later than", thrown.Message);
    }

    [Fact]
    public void The_latest_exam_cannot_be_tracked_a_second_time() {
        // Arrange
        CtExam            anyExam     = Arbitrary.CtExam().WithLandmarks(Scans.AnyLandmarks(3)).Generate();
        NoduleObservation anyBaseline = Arbitrary.NoduleObservation().WithExam(anyExam).Generate();
        TrackedNodule     nodule      = AnyNoduleFrom(anyBaseline, toleranceMm: 10.0);

        // Act
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => nodule.Track(Arbitrary.NoduleObservation().WithExam(anyExam).Generate()));

        // Assert
        Assert.Contains("already the latest exam", thrown.Message);
    }

    [Fact]
    public void A_follow_up_sharing_too_few_landmarks_with_the_latest_exam_fails_to_register() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = Scans.AnyLandmarks(4);
        DateOnly          anyDate     = Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2030, 12, 31)).Generate();
        CtExam            anyBaseline = Arbitrary.CtExam().WithAcquiredOn(anyDate).WithLandmarks(anyLandmarks).Generate();
        CtExam            anyFollowUp = Arbitrary.CtExam().WithAcquiredOn(anyDate.AddDays(60)).WithLandmarks(anyLandmarks.Take(2).ToList()).Generate();
        SpatialPoint      anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule     nodule      = AnyNoduleFrom(Scans.ObservationOn(anyBaseline, anyCentroid), toleranceMm: 10.0);

        // Act
        Assert.Throws<RegistrationFailedException>(() => nodule.Track(Scans.ObservationOn(anyFollowUp, anyCentroid)));

        // Assert
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_resolved_nodule_records_the_date_and_accepts_no_further_exam() {
        // Arrange
        (CtExam anyBaselineExam, CtExam anyClearExam) = AnyExamPair(dxMm: 0.0, dyMm: 0.0, dzMm: 0.0, daysApart: 365);
        SpatialPoint  anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule nodule      = AnyNoduleFrom(Scans.ObservationOn(anyBaselineExam, anyCentroid), toleranceMm: 10.0);

        // Act
        nodule.MarkResolved(anyClearExam);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, nodule.Status);
        Assert.Equal(anyClearExam.AcquiredOn, nodule.ResolvedOn);
        Assert.Throws<InvalidOperationException>(() => nodule.Track(Scans.ObservationOn(anyClearExam, anyCentroid)));
    }

    [Fact]
    public void A_single_observation_cannot_be_compared() {
        // Arrange
        TrackedNodule anyNodule = Arbitrary.TrackedNodule().Generate();

        // Act
        InvalidOperationException thrown = Assert.Throws<InvalidOperationException>(() => anyNodule.CompareLatest());

        // Assert
        Assert.Contains("two observations", thrown.Message);
    }

    [Fact]
    public void Comparing_with_the_baseline_spans_every_follow_up_while_comparing_latest_spans_the_last_interval() {
        // Arrange
        VoxelSpacing oneCubicMillimetre = new VoxelSpacing(1.0, 1.0, 1.0);
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = Scans.AnyLandmarks(3);
        DateOnly     anyDate     = Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2030, 12, 31)).Generate();
        SpatialPoint anyCentroid = Scans.AnyPointInThorax().Generate();
        TrackedNodule nodule = AnyNoduleFrom(ObservationOf(100, anyDate, oneCubicMillimetre, anyLandmarks, anyCentroid), toleranceMm: 10.0);
        nodule.Track(ObservationOf(150, anyDate.AddDays(90), oneCubicMillimetre, anyLandmarks, anyCentroid));
        nodule.Track(ObservationOf(400, anyDate.AddDays(270), oneCubicMillimetre, anyLandmarks, anyCentroid));

        // Act
        VolumeChange sinceBaseline = nodule.CompareWithBaseline();
        VolumeChange lastInterval  = nodule.CompareLatest();

        // Assert
        Assert.Equal((100.0, 400.0, 270), (sinceBaseline.Before.VolumeMm3, sinceBaseline.After.VolumeMm3, sinceBaseline.IntervalDays));
        Assert.Equal((150.0, 400.0, 180), (lastInterval.Before.VolumeMm3, lastInterval.After.VolumeMm3, lastInterval.IntervalDays));
        Assert.Equal(VolumeTrend.Growth, sinceBaseline.Trend);
    }

    private static (CtExam Baseline, CtExam FollowUp) AnyExamPair(double dxMm, double dyMm, double dzMm, int daysApart) {
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = Scans.AnyLandmarks(4);
        DateOnly anyDate = Any.DateOnly().Between(new DateOnly(2015, 1, 1), new DateOnly(2030, 12, 31)).Generate();
        CtExam baseline = Arbitrary.CtExam().WithAcquiredOn(anyDate).WithLandmarks(anyLandmarks).Generate();
        CtExam followUp = Arbitrary.CtExam()
                                   .WithAcquiredOn(anyDate.AddDays(daysApart))
                                   .WithLandmarks(Scans.Shifted(anyLandmarks, dxMm, dyMm, dzMm))
                                   .Generate();

        return (baseline, followUp);
    }

    private static TrackedNodule AnyNoduleFrom(NoduleObservation baseline, double toleranceMm) {
        return Arbitrary.TrackedNodule().WithBaseline(baseline).WithMatchToleranceMm(toleranceMm).Generate();
    }

    private static NoduleObservation ObservationOf(int voxelCount, DateOnly acquiredOn, VoxelSpacing spacing, IReadOnlyList<AnatomicalLandmark> landmarks, SpatialPoint centroid) {
        CtExam anyExam = Arbitrary.CtExam().WithAcquiredOn(acquiredOn).WithSpacing(spacing).WithLandmarks(landmarks).Generate();

        return Arbitrary.NoduleObservation()
                        .WithExam(anyExam)
                        .WithSegmentation(s => s.WithCentroid(centroid).WithVoxelCount(voxelCount).WithBoundaryVoxelCount(0))
                        .Generate();
    }

}
