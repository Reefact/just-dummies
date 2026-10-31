using System.Collections.Generic;
using System.Linq;

using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

using Xunit;

namespace PulmonaryNodules.Tests.Expert;

public sealed class TrackedNoduleTests {

    [Fact]
    public void The_same_nodule_is_recognised_after_the_patient_was_repositioned_between_acquisitions() {
        // Arrange
        TrackedNodule        anyNodule        = Thorax.NoduleOnARegistrableBaseline().Generate();
        TranslationTransform anyRepositioning = Thorax.PatientRepositioning().Generate();
        int                  anyDelay         = Thorax.FollowUpDelayInDays().Generate();

        CtExam            anyFollowUpExam = Thorax.FollowUpOf(anyNodule.Latest.Exam, anyRepositioning, anyDelay).Generate();
        SpatialPoint      seenAgain       = anyRepositioning.Apply(anyNodule.Latest.Segmentation.Centroid);
        NoduleObservation followUp        = Thorax.SegmentedAt(anyFollowUpExam, seenAgain);

        // Act
        double distance = anyNodule.Track(followUp);

        // Assert
        Assert.Equal(0.0, distance, precision: 6);
        Assert.Same(followUp, anyNodule.Latest);
        Assert.Equal(2, anyNodule.Observations.Count);
    }

    [Fact]
    public void A_segmentation_within_the_match_tolerance_of_the_predicted_position_is_the_same_nodule() {
        // Arrange
        TrackedNodule        anyNodule        = Thorax.NoduleOnARegistrableBaseline().WithMatchToleranceMm(5.0).Generate();
        TranslationTransform anyRepositioning = Thorax.PatientRepositioning().Generate();
        int                  anyDelay         = Thorax.FollowUpDelayInDays().Generate();

        CtExam            anyFollowUpExam = Thorax.FollowUpOf(anyNodule.Latest.Exam, anyRepositioning, anyDelay).Generate();
        SpatialPoint      threeMmAway     = anyRepositioning.Apply(anyNodule.Latest.Segmentation.Centroid).Offset(0.0, 3.0, 0.0);
        NoduleObservation followUp        = Thorax.SegmentedAt(anyFollowUpExam, threeMmAway);

        // Act
        double distance = anyNodule.Track(followUp);

        // Assert
        Assert.Equal(3.0, distance, precision: 6);
        Assert.Same(followUp, anyNodule.Latest);
    }

    [Fact]
    public void A_segmentation_beyond_the_match_tolerance_is_another_nodule_and_is_not_recorded() {
        // Arrange
        TrackedNodule        anyNodule        = Thorax.NoduleOnARegistrableBaseline().WithMatchToleranceMm(5.0).Generate();
        TranslationTransform anyRepositioning = Thorax.PatientRepositioning().Generate();
        int                  anyDelay         = Thorax.FollowUpDelayInDays().Generate();

        CtExam            anyFollowUpExam = Thorax.FollowUpOf(anyNodule.Latest.Exam, anyRepositioning, anyDelay).Generate();
        SpatialPoint      eightMmAway     = anyRepositioning.Apply(anyNodule.Latest.Segmentation.Centroid).Offset(8.0, 0.0, 0.0);
        NoduleObservation followUp        = Thorax.SegmentedAt(anyFollowUpExam, eightMmAway);

        // Act
        NoduleMismatchException error = Assert.Throws<NoduleMismatchException>(() => anyNodule.Track(followUp));

        // Assert
        Assert.Contains("8.0 mm", error.Message);
        Assert.Single(anyNodule.Observations);
    }

    [Fact]
    public void An_imprecise_registration_widens_the_match_window_by_its_residual() {
        // Arrange — two landmarks off by ±3 mm leave an RMS residual of √6 ≈ 2.45 mm, so 2 mm of
        // tolerance accepts a nodule 4 mm from its predicted position.
        IReadOnlyCollection<AnatomicalLandmark> anyLandmarks = Any.ListOf(Any.AnatomicalLandmark().WithPosition(Thorax.Inside))
                                                                  .Distinct(LandmarkKindComparer.Instance)
                                                                  .WithCount(3)
                                                                  .Generate();
        TrackedNodule anyNodule = Thorax.NoduleOnARegistrableBaseline()
                                        .WithBaseline(b => b.WithExam(e => e.WithLandmarks(anyLandmarks))
                                                            .WithSegmentation(s => s.WithCentroid(Thorax.Inside)))
                                        .WithMatchToleranceMm(2.0)
                                        .Generate();
        AnatomicalLandmark[]     baseline          = anyNodule.Latest.Exam.Landmarks.ToArray();
        List<AnatomicalLandmark> followUpLandmarks = [
            new AnatomicalLandmark(baseline[0].Kind, baseline[0].Position.Offset(3.0, 0.0, 0.0)),
            new AnatomicalLandmark(baseline[1].Kind, baseline[1].Position.Offset(-3.0, 0.0, 0.0)),
            baseline[2]
        ];
        CtExam anyFollowUpExam = Any.CtExam()
                                    .WithAccessionNumber(a => a.DifferentFrom(anyNodule.Latest.Exam.AccessionNumber))
                                    .WithAcquiredOn(anyNodule.Latest.Exam.AcquiredOn.AddDays(Thorax.FollowUpDelayInDays().Generate()))
                                    .WithLandmarks(followUpLandmarks)
                                    .Generate();
        NoduleObservation followUp = Thorax.SegmentedAt(anyFollowUpExam, anyNodule.Latest.Segmentation.Centroid.Offset(0.0, 0.0, 4.0));

        // Act
        double distance = anyNodule.Track(followUp);

        // Assert
        Assert.Equal(4.0, distance, precision: 6);
    }

    [Fact]
    public void A_follow_up_sharing_too_few_landmarks_cannot_be_matched_and_is_not_recorded() {
        // Arrange
        TrackedNodule anyNodule = Thorax.NoduleOnARegistrableBaseline().Generate();
        CtExam        baseline  = anyNodule.Latest.Exam;
        List<AnatomicalLandmark> onlyTwoInCommon = baseline.Landmarks.Take(2).ToList();
        CtExam anyFollowUpExam = Any.CtExam()
                                    .WithAccessionNumber(a => a.DifferentFrom(baseline.AccessionNumber))
                                    .WithAcquiredOn(baseline.AcquiredOn.AddDays(Thorax.FollowUpDelayInDays().Generate()))
                                    .WithLandmarks(onlyTwoInCommon)
                                    .Generate();
        NoduleObservation anyFollowUp = Thorax.SegmentedAt(anyFollowUpExam, anyNodule.Latest.Segmentation.Centroid);

        // Act
        Assert.Throws<RegistrationFailedException>(() => anyNodule.Track(anyFollowUp));

        // Assert
        Assert.Single(anyNodule.Observations);
    }

    [Fact]
    public void An_exam_acquired_before_the_latest_one_cannot_follow_it() {
        // Arrange
        TrackedNodule anyNodule  = Any.TrackedNodule().Generate();
        DateOnly      latestDate = anyNodule.Latest.Exam.AcquiredOn;
        NoduleObservation anyEarlierObservation = Any.NoduleObservation()
                                                     .WithExam(e => e.WithAcquiredOn(d => d.Before(latestDate))
                                                                     .WithAccessionNumber(a => a.DifferentFrom(anyNodule.Latest.Exam.AccessionNumber)))
                                                     .Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyNodule.Track(anyEarlierObservation));

        // Assert
        Assert.Contains("is not later than the latest exam", error.Message);
        Assert.Single(anyNodule.Observations);
    }

    [Fact]
    public void The_latest_exam_cannot_be_tracked_a_second_time() {
        // Arrange
        TrackedNodule     anyNodule         = Any.TrackedNodule().Generate();
        NoduleObservation anyResegmentation = Any.NoduleObservation().WithExam(anyNodule.Latest.Exam).Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyNodule.Track(anyResegmentation));

        // Assert
        Assert.Contains("is already the latest exam", error.Message);
    }

    [Fact]
    public void A_resolved_nodule_records_when_it_disappeared_and_accepts_no_further_exam() {
        // Arrange
        TrackedNodule anyNodule        = Thorax.NoduleOnARegistrableBaseline().Generate();
        DateOnly      latestDate       = anyNodule.Latest.Exam.AcquiredOn;
        DateOnly      resolutionDate   = latestDate.AddDays(180);
        CtExam        anyClearExam     = Any.CtExam().WithAcquiredOn(resolutionDate).Generate();
        CtExam        anyLaterExam     = Thorax.FollowUpOf(anyClearExam, TranslationTransform.Identity, 90).Generate();
        NoduleObservation anyLaterObservation = Thorax.SegmentedAt(anyLaterExam, anyNodule.Latest.Segmentation.Centroid);

        // Act
        anyNodule.MarkResolved(anyClearExam);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, anyNodule.Status);
        Assert.Equal(resolutionDate, anyNodule.ResolvedOn);
        Assert.Throws<InvalidOperationException>(() => anyNodule.Track(anyLaterObservation));
    }

    [Fact]
    public void A_nodule_seen_only_once_has_nothing_to_compare_with() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => anyNodule.CompareWithBaseline());

        // Assert
        Assert.Equal("A comparison needs at least two observations.", error.Message);
    }

    [Fact]
    public void The_latest_comparison_spans_the_last_interval_while_the_baseline_comparison_spans_the_whole_follow_up() {
        // Arrange — the same acquisition grid at every exam: 1 mm³ voxels, no boundary uncertainty.
        VoxelSpacing         isotropicMillimetre = new VoxelSpacing(1.0, 1.0, 1.0);
        TranslationTransform anyRepositioning    = Thorax.PatientRepositioning().Generate();
        TrackedNodule        anyNodule           = Thorax.NoduleOnARegistrableBaseline()
                                                         .WithBaseline(b => b.WithExam(e => e.WithLandmarks(Thorax.Fiducials()).WithSpacing(isotropicMillimetre))
                                                                             .WithSegmentation(s => s.WithCentroid(Thorax.Inside).WithVoxelCount(400).WithBoundaryVoxelCount(0)))
                                                         .Generate();
        NoduleObservation threeMonthsLater = SameNoduleLater(anyNodule.Latest, anyRepositioning, 90, voxelCount: 500);
        anyNodule.Track(threeMonthsLater);
        NoduleObservation sixMonthsLater = SameNoduleLater(anyNodule.Latest, anyRepositioning, 90, voxelCount: 800);
        anyNodule.Track(sixMonthsLater);

        // Act
        VolumeChange latest       = anyNodule.CompareLatest();
        VolumeChange sinceBaseline = anyNodule.CompareWithBaseline();

        // Assert
        Assert.Equal((90, 300.0, VolumeTrend.Growth), (latest.IntervalDays, latest.DeltaMm3, latest.Trend));
        Assert.Equal((180, 400.0, VolumeTrend.Growth), (sinceBaseline.IntervalDays, sinceBaseline.DeltaMm3, sinceBaseline.Trend));
        Assert.Equal(180.0, sinceBaseline.VolumeDoublingTimeDays!.Value, precision: 6);
    }

    private static NoduleObservation SameNoduleLater(NoduleObservation previous, TranslationTransform repositioning, int daysLater, int voxelCount) {
        CtExam anyExam = Thorax.FollowUpOf(previous.Exam, repositioning, daysLater)
                               .WithSpacing(previous.Exam.Spacing)
                               .Generate();

        return Any.NoduleObservation()
                  .WithExam(anyExam)
                  .WithSegmentation(s => s.WithCentroid(repositioning.Apply(previous.Segmentation.Centroid))
                                          .WithVoxelCount(voxelCount)
                                          .WithBoundaryVoxelCount(0))
                  .Generate();
    }

}
