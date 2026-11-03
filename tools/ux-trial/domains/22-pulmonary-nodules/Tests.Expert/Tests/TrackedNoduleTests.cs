using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using PulmonaryNodules.Tests.Expert.Support;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class TrackedNoduleTests {

    private static readonly Guid NoduleId = new Guid("3f2c1d3e-9b52-4e5a-8d0e-6a7a1c2b4f10");

    private static TrackedNodule NoduleWith(NoduleObservation baseline, double toleranceMm) {
        return Any.TrackedNodule().WithBaseline(baseline).WithMatchToleranceMm(toleranceMm).Generate();
    }

    // =====================================================================================
    // Creation
    // =====================================================================================

    [Fact]
    public void A_new_nodule_is_under_surveillance_with_its_baseline_as_only_observation() {
        // Arrange
        NoduleObservation baseline = Any.NoduleObservation().Generate();

        // Act
        TrackedNodule nodule = Any.TrackedNodule().WithId(NoduleId).WithPatientId("PAT-0042").WithBaseline(baseline).WithMatchToleranceMm(4.5).Generate();

        // Assert
        Assert.Equal(NoduleId, nodule.Id);
        Assert.Equal("PAT-0042", nodule.PatientId);
        Assert.Equal(4.5, nodule.MatchToleranceMm);
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
        Assert.Null(nodule.ResolvedOn);
        Assert.Same(baseline, Assert.Single(nodule.Observations));
        Assert.Same(baseline, nodule.Baseline);
        Assert.Same(baseline, nodule.Latest);
    }

    [Fact]
    public void A_nodule_without_identifier_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TrackedNodule().WithId(Guid.Empty).Generate());

        // Assert
        Assert.Equal("id", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_patient_identifier_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TrackedNodule().WithPatientId(blank).Generate());

        // Assert
        Assert.Equal("patientId", refusal.ParamName);
    }

    [Fact]
    public void A_missing_patient_identifier_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.TrackedNodule().WithPatientId((string)null!).Generate());

        // Assert
        Assert.Equal("patientId", refusal.ParamName);
    }

    [Fact]
    public void A_patient_identifier_of_64_characters_is_accepted() {
        // Arrange
        string sixtyFourCharacters = Any.String().NotBlank().WithLength(64).Generate();

        // Act
        TrackedNodule nodule = Any.TrackedNodule().WithPatientId(sixtyFourCharacters).Generate();

        // Assert
        Assert.Equal(64, nodule.PatientId.Length);
    }

    [Fact]
    public void A_patient_identifier_of_65_characters_is_refused() {
        // Arrange
        string sixtyFiveCharacters = Any.String().NotBlank().WithLength(65).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.TrackedNodule().WithPatientId(sixtyFiveCharacters).Generate());

        // Assert
        Assert.Equal("patientId", refusal.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(20.0)]
    public void A_match_tolerance_on_either_edge_of_its_range_is_accepted(double edgeMm) {
        // Act
        TrackedNodule nodule = Any.TrackedNodule().WithMatchToleranceMm(edgeMm).Generate();

        // Assert
        Assert.Equal(edgeMm, nodule.MatchToleranceMm);
    }

    [Theory]
    [InlineData(0.999)]
    [InlineData(20.001)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    public void A_match_tolerance_outside_1_to_20_mm_is_refused(double outsideMm) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.TrackedNodule().WithMatchToleranceMm(outsideMm).Generate());

        // Assert
        Assert.Equal("matchToleranceMm", refusal.ParamName);
    }

    [Fact]
    public void A_nodule_needs_a_baseline() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new TrackedNodule(anyNodule.Id, anyNodule.PatientId, null!, anyNodule.MatchToleranceMm));

        // Assert
        Assert.Equal("baseline", refusal.ParamName);
    }

    // =====================================================================================
    // Tracking a follow-up
    // =====================================================================================

    /// <summary>A baseline and a follow-up the next day: the patient moved by <c>shift</c>, the nodule by that shift plus <c>offsetXMm</c>.</summary>
    private static (TrackedNodule Nodule, NoduleObservation FollowUp) FollowUpOffByAlongX(double offsetXMm, double toleranceMm) {
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm);

        // The patient lay 10 mm further along x; the nodule followed, then drifted by the offset.
        NoduleObservation followUp = Scans.ObservationOn(anyBaselineDay.AddDays(1),
                                                         Scans.Shifted(landmarks, 10.0, 0.0, 0.0),
                                                         Scans.Moved(centroid, 10.0 + offsetXMm, 0.0, 0.0));

        return (nodule, followUp);
    }

    [Fact]
    public void A_follow_up_exactly_where_the_registration_predicts_it_is_accepted_and_recorded() {
        // Arrange
        (TrackedNodule nodule, NoduleObservation followUp) = FollowUpOffByAlongX(offsetXMm: 0.0, toleranceMm: 5.0);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(0.0, distance);
        Assert.Equal(2, nodule.Observations.Count);
        Assert.Same(followUp, nodule.Latest);
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
    }

    [Fact]
    public void Tracking_returns_the_distance_between_the_predicted_and_the_observed_centroid() {
        // Arrange
        (TrackedNodule nodule, NoduleObservation followUp) = FollowUpOffByAlongX(offsetXMm: 3.0, toleranceMm: 5.0);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(3.0, distance);
    }

    [Fact]
    public void A_follow_up_exactly_at_the_match_tolerance_is_accepted() {
        // Arrange
        (TrackedNodule nodule, NoduleObservation followUp) = FollowUpOffByAlongX(offsetXMm: 5.0, toleranceMm: 5.0);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(5.0, distance);
        Assert.Same(followUp, nodule.Latest);
    }

    [Fact]
    public void A_follow_up_beyond_the_match_tolerance_is_refused_and_not_recorded() {
        // Arrange
        (TrackedNodule nodule, NoduleObservation followUp) = FollowUpOffByAlongX(offsetXMm: 5.5, toleranceMm: 5.0);

        // Act
        NoduleMismatchException mismatch = Assert.Throws<NoduleMismatchException>(() => nodule.Track(followUp));

        // Assert
        Assert.Contains(nodule.Id.ToString(), mismatch.Message);
        Assert.Single(nodule.Observations);
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
    }

    [Fact]
    public void A_displacement_in_any_direction_counts_the_same() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 5.0);

        // A 3-4-0 displacement is 5 mm in a straight line: right on the tolerance.
        NoduleObservation followUp = Scans.ObservationOn(anyBaselineDay.AddDays(1), landmarks, Scans.Moved(centroid, 3.0, 0.0, 4.0));

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(5.0, distance);
    }

    [Fact]
    public void The_registration_residual_widens_the_distance_a_follow_up_may_be_found_at() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 4);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 3.0);

        // The landmarks scatter by +4, -4, +4, -4 mm: no net shift, but a 4 mm registration residual,
        // so a nodule found 6 mm away is beyond the 3 mm tolerance alone and within 3 + 4 mm.
        NoduleObservation followUp = Scans.ObservationOn(anyBaselineDay.AddDays(1),
                                                         Scans.DisplacedAlongX(landmarks, 4.0, -4.0, 4.0, -4.0),
                                                         Scans.Moved(centroid, 6.0, 0.0, 0.0));

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(6.0, distance);
    }

    [Fact]
    public void A_follow_up_beyond_the_tolerance_plus_the_residual_is_refused() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 4);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 3.0);
        NoduleObservation followUp = Scans.ObservationOn(anyBaselineDay.AddDays(1),
                                                         Scans.DisplacedAlongX(landmarks, 4.0, -4.0, 4.0, -4.0),
                                                         Scans.Moved(centroid, 7.5, 0.0, 0.0));

        // Act / Assert
        Assert.Throws<NoduleMismatchException>(() => nodule.Track(followUp));
    }

    [Fact]
    public void Each_follow_up_is_registered_onto_the_latest_exam_and_not_onto_the_baseline() {
        // Arrange
        // Six landmarks on the baseline day's exam... The baseline locates kinds A-C, the first follow-up A-F,
        // the second only D-F: it shares nothing with the baseline, so it can only be registered onto the first follow-up.
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        List<AnatomicalLandmark> six = Scans.AnyLandmarks(count: 6).ToList();
        IReadOnlyCollection<AnatomicalLandmark> firstThree = six.Take(3).ToList();
        IReadOnlyCollection<AnatomicalLandmark> sixAfterFirstMove = Scans.Shifted(six, 10.0, 0.0, 0.0);
        IReadOnlyCollection<AnatomicalLandmark> lastThreeAfterSecondMove = Scans.Shifted(sixAfterFirstMove.Skip(3).ToList(), 0.0, 20.0, 0.0);

        SpatialPoint centroid = Scans.AnyCentroid();
        SpatialPoint centroidAfterFirstMove = Scans.Moved(centroid, 10.0, 0.0, 0.0);
        SpatialPoint centroidAfterSecondMove = Scans.Moved(centroidAfterFirstMove, 0.0, 20.0, 0.0);

        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, firstThree, centroid), toleranceMm: 1.0);
        NoduleObservation first = Scans.ObservationOn(anyBaselineDay.AddDays(30), sixAfterFirstMove, centroidAfterFirstMove);
        NoduleObservation second = Scans.ObservationOn(anyBaselineDay.AddDays(60), lastThreeAfterSecondMove, centroidAfterSecondMove);

        // Act
        double firstDistance = nodule.Track(first);
        double secondDistance = nodule.Track(second);

        // Assert
        Assert.Equal(0.0, firstDistance);
        Assert.Equal(0.0, secondDistance);
        Assert.Equal(3, nodule.Observations.Count);
        Assert.Same(second, nodule.Latest);
        Assert.Same(first, nodule.Observations[1]);
    }

    [Fact]
    public void A_follow_up_that_cannot_be_registered_is_not_recorded() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 20.0);
        NoduleObservation onlyTwoLandmarks = Scans.ObservationOn(anyBaselineDay.AddDays(1), landmarks.Take(2).ToList(), centroid);

        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => nodule.Track(onlyTwoLandmarks));
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_follow_up_taken_the_same_day_as_the_latest_exam_is_refused() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        NoduleObservation sameDay = Any.NoduleObservation().WithExam(e => e.WithAcquiredOn(anyBaselineDay)).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => nodule.Track(sameDay));

        // Assert
        Assert.Contains("not later", refusal.Message);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_follow_up_taken_before_the_latest_exam_is_refused() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        NoduleObservation dayBefore = Any.NoduleObservation().WithExam(e => e.WithAcquiredOn(anyBaselineDay.AddDays(-1))).Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => nodule.Track(dayBefore));

        // Assert
        Assert.Contains("not later", refusal.Message);
    }

    [Fact]
    public void The_same_exam_cannot_be_tracked_twice_even_if_dated_later() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().Generate();
        NoduleObservation sameAccession = Any.NoduleObservation()
                                             .WithExam(e => e.WithAccessionNumber(nodule.Latest.Exam.AccessionNumber).WithAcquiredOn(nodule.Latest.Exam.AcquiredOn.AddDays(1)))
                                             .Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => nodule.Track(sameAccession));

        // Assert
        Assert.Contains("already the latest", refusal.Message);
    }

    [Fact]
    public void Tracking_nothing_is_refused() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNodule.Track(null!));
    }

    // =====================================================================================
    // Resolution
    // =====================================================================================

    [Fact]
    public void A_nodule_no_longer_seen_on_a_later_exam_is_resolved_on_that_exams_day() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        DateOnly laterDay = anyBaselineDay.AddDays(200);
        CtExam laterExam = Any.CtExam().WithAcquiredOn(laterDay).Generate();

        // Act
        nodule.MarkResolved(laterExam);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, nodule.Status);
        Assert.Equal(laterDay, nodule.ResolvedOn);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void Resolving_needs_no_landmark_since_nothing_has_to_be_registered() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        CtExam withoutLandmarks = Any.CtExam().WithAcquiredOn(anyBaselineDay.AddDays(1)).WithLandmarks(Array.Empty<AnatomicalLandmark>()).Generate();

        // Act
        nodule.MarkResolved(withoutLandmarks);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, nodule.Status);
    }

    [Fact]
    public void Resolving_on_an_exam_not_later_than_the_latest_one_is_refused() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        CtExam sameDay = Any.CtExam().WithAcquiredOn(anyBaselineDay).Generate();
        CtExam dayBefore = Any.CtExam().WithAcquiredOn(anyBaselineDay.AddDays(-1)).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(sameDay));
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(dayBefore));
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
        Assert.Null(nodule.ResolvedOn);
    }

    [Fact]
    public void Resolving_must_come_after_the_latest_follow_up_not_just_after_the_baseline() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 1.0);
        nodule.Track(Scans.ObservationOn(anyBaselineDay.AddDays(100), landmarks, centroid));
        CtExam betweenBaselineAndFollowUp = Any.CtExam().WithAcquiredOn(anyBaselineDay.AddDays(50)).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(betweenBaselineAndFollowUp));
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
    }

    [Fact]
    public void The_exam_that_is_already_the_latest_cannot_resolve_the_nodule() {
        // Arrange
        TrackedNodule nodule = Any.TrackedNodule().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(nodule.Latest.Exam));

        // Assert
        Assert.Contains("already the latest", refusal.Message);
    }

    [Fact]
    public void Resolving_on_nothing_is_refused() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNodule.MarkResolved(null!));
    }

    [Fact]
    public void A_resolved_nodule_accepts_no_further_follow_up() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOn(anyBaselineDay, landmarks, centroid), toleranceMm: 20.0);
        nodule.MarkResolved(Any.CtExam().WithAcquiredOn(anyBaselineDay.AddDays(10)).Generate());
        NoduleObservation laterFollowUp = Scans.ObservationOn(anyBaselineDay.AddDays(20), landmarks, centroid);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => nodule.Track(laterFollowUp));

        // Assert
        Assert.Contains("resolved", refusal.Message);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_resolved_nodule_cannot_be_resolved_again_and_keeps_its_first_resolution_day() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        TrackedNodule nodule = Any.TrackedNodule().WithBaseline(b => b.WithExam(e => e.WithAcquiredOn(anyBaselineDay))).Generate();
        DateOnly firstResolution = anyBaselineDay.AddDays(10);
        nodule.MarkResolved(Any.CtExam().WithAcquiredOn(firstResolution).Generate());
        CtExam laterExam = Any.CtExam().WithAcquiredOn(anyBaselineDay.AddDays(20)).Generate();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(laterExam));
        Assert.Equal(firstResolution, nodule.ResolvedOn);
    }

    // =====================================================================================
    // Comparing observations
    // =====================================================================================

    [Fact]
    public void A_nodule_seen_once_has_nothing_to_compare() {
        // Arrange
        TrackedNodule nodule = Any.TrackedNodule().Generate();

        // Act
        InvalidOperationException latestRefusal = Assert.Throws<InvalidOperationException>(() => nodule.CompareLatest());
        InvalidOperationException baselineRefusal = Assert.Throws<InvalidOperationException>(() => nodule.CompareWithBaseline());

        // Assert
        Assert.Contains("two observations", latestRefusal.Message);
        Assert.Contains("two observations", baselineRefusal.Message);
    }

    [Fact]
    public void With_two_observations_both_comparisons_are_the_baseline_against_the_follow_up() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOfVoxels(100, anyBaselineDay, landmarks, centroid), toleranceMm: 1.0);
        nodule.Track(Scans.ObservationOfVoxels(150, anyBaselineDay.AddDays(30), landmarks, centroid));

        // Act
        VolumeChange latest = nodule.CompareLatest();
        VolumeChange baseline = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(100.0, latest.Before.VolumeMm3);
        Assert.Equal(150.0, latest.After.VolumeMm3);
        Assert.Equal(30, latest.IntervalDays);
        Assert.Equal(100.0, baseline.Before.VolumeMm3);
        Assert.Equal(150.0, baseline.After.VolumeMm3);
        Assert.Equal(30, baseline.IntervalDays);
    }

    [Fact]
    public void With_three_observations_the_latest_comparison_spans_the_last_two_and_the_baseline_one_spans_them_all() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        TrackedNodule nodule = NoduleWith(Scans.ObservationOfVoxels(100, anyBaselineDay, landmarks, centroid), toleranceMm: 1.0);
        nodule.Track(Scans.ObservationOfVoxels(120, anyBaselineDay.AddDays(30), landmarks, centroid));
        nodule.Track(Scans.ObservationOfVoxels(300, anyBaselineDay.AddDays(90), landmarks, centroid));

        // Act
        VolumeChange latest = nodule.CompareLatest();
        VolumeChange baseline = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(120.0, latest.Before.VolumeMm3);
        Assert.Equal(300.0, latest.After.VolumeMm3);
        Assert.Equal(60, latest.IntervalDays);
        Assert.Equal(100.0, baseline.Before.VolumeMm3);
        Assert.Equal(300.0, baseline.After.VolumeMm3);
        Assert.Equal(90, baseline.IntervalDays);
    }

    [Fact]
    public void A_nodule_that_tripled_over_the_follow_up_is_growing() {
        // Arrange
        DateOnly anyBaselineDay = Scans.AnyScanDate().Generate();
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);
        SpatialPoint centroid = Scans.AnyCentroid();
        // 1000 voxels, then 3000 voxels, in segmentations without a boundary voxel to blur the measurement.
        VoxelSpacing unitCube = new VoxelSpacing(1.0, 1.0, 1.0);
        NoduleObservation baseline = Any.NoduleObservation()
                                        .WithExam(e => e.WithAcquiredOn(anyBaselineDay).WithLandmarks(landmarks).WithSpacing(unitCube))
                                        .WithSegmentation(s => s.WithCentroid(centroid).WithSpacing(unitCube).WithVoxelCount(1000).WithBoundaryVoxelCount(0))
                                        .Generate();
        NoduleObservation followUp = Any.NoduleObservation()
                                        .WithExam(e => e.WithAcquiredOn(anyBaselineDay.AddDays(60)).WithLandmarks(landmarks).WithSpacing(unitCube))
                                        .WithSegmentation(s => s.WithCentroid(centroid).WithSpacing(unitCube).WithVoxelCount(3000).WithBoundaryVoxelCount(0))
                                        .Generate();
        TrackedNodule nodule = NoduleWith(baseline, toleranceMm: 1.0);
        nodule.Track(followUp);

        // Act
        VolumeChange change = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(VolumeTrend.Growth, change.Trend);
        Assert.Equal(60 * Math.Log(2.0) / Math.Log(3.0), change.VolumeDoublingTimeDays!.Value, precision: 9);
    }

}
