using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Discovery;

public sealed class TrackedNoduleTests {

    private const double Precision = 1e-6;

    private static readonly LandmarkKind[] RegistrationLandmarks =
        [LandmarkKind.CarinaBifurcation, LandmarkKind.AorticArchApex, LandmarkKind.T4VertebralBody];

    [Fact]
    public void A_follow_up_within_tolerance_after_a_patient_shift_is_the_same_nodule() {
        // Arrange
        Scenario       scenario = Scenario.Arbitrary();
        TrackedNodule  nodule   = scenario.NoduleWithTolerance(5.0);
        NoduleObservation followUp = scenario.FollowUp(daysLater: 90, centroidOffsetXMm: 2.0);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(2.0, distance, Precision);
        Assert.Equal(2, nodule.Observations.Count);
        Assert.Same(followUp, nodule.Latest);
    }

    [Fact]
    public void A_follow_up_beyond_tolerance_is_not_recognised_as_the_nodule() {
        // Arrange
        Scenario          scenario = Scenario.Arbitrary();
        TrackedNodule     nodule   = scenario.NoduleWithTolerance(5.0);
        NoduleObservation followUp = scenario.FollowUp(daysLater: 90, centroidOffsetXMm: 8.0);

        // Act
        Assert.Throws<NoduleMismatchException>(() => nodule.Track(followUp));

        // Assert
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_follow_up_that_cannot_be_registered_is_refused() {
        // Arrange
        Scenario          scenario = Scenario.Arbitrary();
        TrackedNodule     nodule   = scenario.NoduleWithTolerance(5.0);
        CtExam            anyUnregistrableExam = scenario.FollowUpExam(daysLater: 90, landmarkCount: 2);
        NoduleObservation followUp = Any.NoduleObservation().WithExam(anyUnregistrableExam).Generate();

        // Act
        Assert.Throws<RegistrationFailedException>(() => nodule.Track(followUp));

        // Assert
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void An_exam_acquired_before_the_latest_one_cannot_be_tracked() {
        // Arrange
        Scenario          scenario = Scenario.Arbitrary();
        TrackedNodule     nodule   = scenario.NoduleWithTolerance(5.0);
        NoduleObservation earlier  = scenario.FollowUp(daysLater: -30, centroidOffsetXMm: 0.0);

        // Act
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => nodule.Track(earlier));

        // Assert
        Assert.Contains("not later", failure.Message);
    }

    [Fact]
    public void The_latest_exam_cannot_be_tracked_a_second_time() {
        // Arrange
        Scenario          scenario = Scenario.Arbitrary();
        TrackedNodule     nodule   = scenario.NoduleWithTolerance(5.0);
        NoduleObservation again    = Any.NoduleObservation().WithExam(scenario.BaselineExam).Generate();

        // Act
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => nodule.Track(again));

        // Assert
        Assert.Contains("already the latest", failure.Message);
    }

    [Fact]
    public void A_resolved_nodule_records_the_date_and_accepts_no_further_exam() {
        // Arrange
        Scenario      scenario       = Scenario.Arbitrary();
        TrackedNodule nodule         = scenario.NoduleWithTolerance(5.0);
        CtExam        resolutionExam = scenario.FollowUpExam(daysLater: 180, landmarkCount: 3);
        NoduleObservation laterFollowUp = scenario.FollowUp(daysLater: 365, centroidOffsetXMm: 0.0);

        // Act
        nodule.MarkResolved(resolutionExam);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, nodule.Status);
        Assert.Equal(resolutionExam.AcquiredOn, nodule.ResolvedOn);
        Assert.Throws<InvalidOperationException>(() => nodule.Track(laterFollowUp));
    }

    [Fact]
    public void A_nodule_with_only_its_baseline_cannot_be_compared() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => anyNodule.CompareWithBaseline());

        // Assert
        Assert.Contains("two observations", failure.Message);
    }

    [Fact]
    public void Comparisons_span_the_last_interval_or_the_whole_follow_up() {
        // Arrange
        Scenario      scenario = Scenario.Arbitrary();
        TrackedNodule nodule   = scenario.NoduleWithTolerance(5.0, voxelCount: 400);
        nodule.Track(scenario.FollowUp(daysLater: 90, centroidOffsetXMm: 0.0, voxelCount: 420));
        nodule.Track(scenario.FollowUp(daysLater: 270, centroidOffsetXMm: 0.0, voxelCount: 800));

        // Act
        VolumeChange latest       = nodule.CompareLatest();
        VolumeChange withBaseline = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(180, latest.IntervalDays);
        Assert.Equal(270, withBaseline.IntervalDays);
        Assert.Equal(800.0 / 420.0, latest.After.VolumeMm3 / latest.Before.VolumeMm3, Precision);
        Assert.Equal(2.0, withBaseline.After.VolumeMm3 / withBaseline.Before.VolumeMm3, Precision);
    }

    /// <summary>
    ///     A nodule seen on an arbitrary baseline exam, and follow-up exams of the same patient shifted by an
    ///     arbitrary rigid translation. Positions, dates, accession numbers and the shift are dummies; what a test
    ///     chooses is the interval and how far the follow-up centroid lies from where registration predicts it.
    /// </summary>
    private sealed class Scenario {

        private readonly AnatomicalLandmark[] _baselineLandmarks;
        private readonly SpatialPoint         _baselineCentroid;
        private readonly (double X, double Y, double Z) _shift;

        private Scenario(AnatomicalLandmark[] baselineLandmarks, SpatialPoint baselineCentroid, (double, double, double) shift, CtExam baselineExam) {
            _baselineLandmarks = baselineLandmarks;
            _baselineCentroid  = baselineCentroid;
            _shift             = shift;
            BaselineExam       = baselineExam;
        }

        public CtExam BaselineExam { get; }

        public VoxelSpacing Spacing => BaselineExam.Spacing;

        public static Scenario Arbitrary() {
            AnatomicalLandmark[] anyLandmarks = RegistrationLandmarks
                                                .Select(kind => new AnatomicalLandmark(kind, AnyThoracicPoint().Generate()))
                                                .ToArray();
            SpatialPoint anyCentroid = AnyThoracicPoint().Generate();
            IAny<double> anyShiftComponent = Any.Double().Between(-50.0, 50.0);
            (double, double, double) anyShift = (anyShiftComponent.Generate(), anyShiftComponent.Generate(), anyShiftComponent.Generate());
            CtExam anyBaselineExam = Any.CtExam()
                                        .WithAcquiredOn(d => d.Between(new DateOnly(2018, 1, 1), new DateOnly(2025, 12, 31)))
                                        .WithLandmarks(anyLandmarks)
                                        .Generate();

            return new Scenario(anyLandmarks, anyCentroid, anyShift, anyBaselineExam);
        }

        public TrackedNodule NoduleWithTolerance(double toleranceMm, int? voxelCount = null) {
            NoduleObservation baseline = Any.NoduleObservation()
                                            .WithExam(BaselineExam)
                                            .WithSegmentation(s => WithCount(s.WithCentroid(_baselineCentroid), voxelCount))
                                            .Generate();

            return Any.TrackedNodule().WithBaseline(baseline).WithMatchToleranceMm(toleranceMm).Generate();
        }

        public CtExam FollowUpExam(int daysLater, int landmarkCount) {
            AnatomicalLandmark[] shifted = _baselineLandmarks
                                           .Take(landmarkCount)
                                           .Select(l => new AnatomicalLandmark(l.Kind, Shift(l.Position, 0.0)))
                                           .ToArray();

            return Any.CtExam()
                      .WithAcquiredOn(BaselineExam.AcquiredOn.AddDays(daysLater))
                      .WithSpacing(BaselineExam.Spacing)
                      .WithLandmarks(shifted)
                      .Generate();
        }

        public NoduleObservation FollowUp(int daysLater, double centroidOffsetXMm, int? voxelCount = null) {
            CtExam exam = FollowUpExam(daysLater, landmarkCount: 3);

            return Any.NoduleObservation()
                      .WithExam(exam)
                      .WithSegmentation(s => WithCount(s.WithCentroid(Shift(_baselineCentroid, centroidOffsetXMm)), voxelCount))
                      .Generate();
        }

        private static AnySegmentation WithCount(AnySegmentation segmentation, int? voxelCount) {
            return voxelCount is int count ? segmentation.WithVoxelCount(count) : segmentation;
        }

        private SpatialPoint Shift(SpatialPoint point, double extraXMm) {
            return new SpatialPoint(point.XMm + _shift.X + extraXMm, point.YMm + _shift.Y, point.ZMm + _shift.Z);
        }

        private static AnySpatialPoint AnyThoracicPoint() {
            return Any.SpatialPoint()
                      .WithXMm(x => x.Between(-1000.0, 1000.0))
                      .WithYMm(y => y.Between(-1000.0, 1000.0))
                      .WithZMm(z => z.Between(-1000.0, 1000.0));
        }

    }

}
