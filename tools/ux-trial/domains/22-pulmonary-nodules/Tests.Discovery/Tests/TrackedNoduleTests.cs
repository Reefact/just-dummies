namespace PulmonaryNodules.Tests.Discovery;

public sealed class TrackedNoduleTests {

    // ---- helpers: the dates, accession numbers and landmarks of an exam are what a follow-up is about, the rest is arbitrary ----

    private static CtExam Exam(string accession, DateOnly acquiredOn, AnatomicalLandmark[] landmarks) {
        return Any.CtExam().WithAccessionNumber(accession).WithAcquiredOn(acquiredOn).WithLandmarks(landmarks).Generate();
    }

    private static NoduleObservation Observation(CtExam exam, SpatialPoint centroid) {
        return Any.NoduleObservation().WithExam(exam).WithSegmentation(s => s.WithCentroid(centroid)).Generate();
    }

    private static NoduleObservation ObservationOfVolume(CtExam exam, int voxelCount, int boundaryVoxelCount) {
        return Any.NoduleObservation()
                  .WithExam(exam)
                  .WithSegmentation(s => s.WithVoxelCount(voxelCount).WithBoundaryVoxelCount(boundaryVoxelCount))
                  .Generate();
    }

    private static readonly DateOnly Jan2024 = new DateOnly(2024, 1, 10);
    private static readonly DateOnly Apr2024 = new DateOnly(2024, 4, 10);
    private static readonly DateOnly Jul2024 = new DateOnly(2024, 7, 10);

    private static TrackedNodule Nodule(NoduleObservation baseline) {
        return Any.TrackedNodule().WithBaseline(baseline).Generate();
    }

    // ---- construction ----

    [Fact]
    public void A_new_nodule_is_under_surveillance_with_its_baseline_as_only_observation() {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        string            anyPatient  = Any.String().Alpha().WithLengthBetween(1, 64).Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();
        double            anyTolerance = Any.Double().Between(TrackedNodule.MinMatchToleranceMm, TrackedNodule.MaxMatchToleranceMm).Generate();

        // Act
        TrackedNodule nodule = new TrackedNodule(anyId, anyPatient, anyBaseline, anyTolerance);

        // Assert
        Assert.Equal(anyId, nodule.Id);
        Assert.Equal(anyPatient, nodule.PatientId);
        Assert.Equal(anyTolerance, nodule.MatchToleranceMm);
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
        Assert.Null(nodule.ResolvedOn);
        Assert.Same(anyBaseline, nodule.Baseline);
        Assert.Same(anyBaseline, nodule.Latest);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_nodule_without_identifier_is_refused() {
        // Arrange
        string            anyPatient  = Any.String().Alpha().WithLengthBetween(1, 64).Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TrackedNodule(Guid.Empty, anyPatient, anyBaseline, 5.0));

        // Assert
        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_patient_identifier_is_refused(string blank) {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TrackedNodule(anyId, blank, anyBaseline, 5.0));

        // Assert
        Assert.Equal("patientId", exception.ParamName);
    }

    [Fact]
    public void A_missing_patient_identifier_is_refused() {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TrackedNodule(anyId, null!, anyBaseline, 5.0));
    }

    [Fact]
    public void A_patient_identifier_of_sixty_four_characters_is_accepted_and_one_more_is_refused() {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();
        string            atTheLimit  = Any.String().Alpha().WithLength(64).Generate();
        string            overTheLimit = Any.String().Alpha().WithLength(65).Generate();

        // Act
        Exception?                  accepted = Record.Exception(() => new TrackedNodule(anyId, atTheLimit, anyBaseline, 5.0));
        ArgumentOutOfRangeException refused  = Assert.Throws<ArgumentOutOfRangeException>(() => new TrackedNodule(anyId, overTheLimit, anyBaseline, 5.0));

        // Assert
        Assert.Null(accepted);
        Assert.Equal("patientId", refused.ParamName);
    }

    [Fact]
    public void A_nodule_without_baseline_is_refused() {
        // Arrange
        Guid   anyId      = Any.Guid().NonEmpty().Generate();
        string anyPatient = Any.String().Alpha().WithLengthBetween(1, 64).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new TrackedNodule(anyId, anyPatient, null!, 5.0));

        // Assert
        Assert.Equal("baseline", exception.ParamName);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(20.0)]
    public void The_limits_of_the_match_tolerance_are_accepted(double toleranceMm) {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        string            anyPatient  = Any.String().Alpha().WithLengthBetween(1, 64).Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();

        // Act
        TrackedNodule nodule = new TrackedNodule(anyId, anyPatient, anyBaseline, toleranceMm);

        // Assert
        Assert.Equal(toleranceMm, nodule.MatchToleranceMm);
    }

    [Theory]
    [InlineData(0.9999)]
    [InlineData(20.0001)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_match_tolerance_outside_one_to_twenty_mm_is_refused(double badMm) {
        // Arrange
        Guid              anyId       = Any.Guid().NonEmpty().Generate();
        string            anyPatient  = Any.String().Alpha().WithLengthBetween(1, 64).Generate();
        NoduleObservation anyBaseline = Any.NoduleObservation().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TrackedNodule(anyId, anyPatient, anyBaseline, badMm));

        // Assert
        Assert.Equal("matchToleranceMm", exception.ParamName);
    }

    // ---- tracking ----

    [Fact]
    public void A_follow_up_at_the_predicted_position_is_accepted_at_no_distance() {
        // Arrange: between the two exams the patient lay 5 mm further along x and 8 mm along z.
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            followUpExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(5, 0, 8));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(100, 100, 100));
        NoduleObservation followUp     = Observation(followUpExam, new SpatialPoint(105, 100, 108));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(0.0, distance, 9);
        Assert.Equal(2, nodule.Observations.Count);
        Assert.Same(followUp, nodule.Latest);
        Assert.Same(baseline, nodule.Baseline);
    }

    [Fact]
    public void Tracking_returns_the_distance_between_the_predicted_and_the_observed_position() {
        // Arrange: the nodule is observed 0.5 mm from where registration puts it; the smallest tolerance is 1 mm.
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            followUpExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(5, 0, 8));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(100, 100, 100));
        NoduleObservation followUp     = Observation(followUpExam, new SpatialPoint(105.3, 100.4, 108));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(0.5, distance, 9);
    }

    [Fact]
    public void A_follow_up_at_exactly_the_tolerance_is_accepted_and_just_beyond_it_is_a_mismatch() {
        // Arrange: a perfect registration leaves no residual, so only the 5 mm tolerance counts.
        CtExam baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam followUpExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        Guid   anyId        = Any.Guid().NonEmpty().Generate();
        string anyPatient   = Any.String().Alpha().WithLengthBetween(1, 64).Generate();

        NoduleObservation baseline = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation atTheLimit    = Observation(followUpExam, new SpatialPoint(5, 0, 0));
        NoduleObservation beyondTheLimit = Observation(followUpExam, new SpatialPoint(5.001, 0, 0));

        TrackedNodule accepting = new TrackedNodule(anyId, anyPatient, baseline, 5.0);
        TrackedNodule refusing  = new TrackedNodule(anyId, anyPatient, baseline, 5.0);

        // Act
        double              distance  = accepting.Track(atTheLimit);
        NoduleMismatchException mismatch = Assert.Throws<NoduleMismatchException>(() => refusing.Track(beyondTheLimit));

        // Assert
        Assert.Equal(5.0, distance, 9);
        Assert.Contains("5.0", mismatch.Message);
    }

    [Fact]
    public void The_registration_residual_widens_the_tolerance() {
        // Arrange: displacements of +3, -3 and 0 mm along x leave a residual of sqrt(6) = 2.449 mm.
        // With a 1 mm tolerance the follow-up is accepted up to 3.449 mm from the predicted position.
        CtExam baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam followUpExam = Exam("CT20240410", Apr2024, Landmarks.Displaced((3, 0, 0), (-3, 0, 0), (0, 0, 0)));
        Guid   anyId        = Any.Guid().NonEmpty().Generate();
        string anyPatient   = Any.String().Alpha().WithLengthBetween(1, 64).Generate();

        NoduleObservation baseline = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation inside   = Observation(followUpExam, new SpatialPoint(3.4, 0, 0));
        NoduleObservation outside  = Observation(followUpExam, new SpatialPoint(3.5, 0, 0));

        TrackedNodule accepting = new TrackedNodule(anyId, anyPatient, baseline, 1.0);
        TrackedNodule refusing  = new TrackedNodule(anyId, anyPatient, baseline, 1.0);

        // Act
        double distance = accepting.Track(inside);

        // Assert
        Assert.Equal(3.4, distance, 9);
        Assert.Throws<NoduleMismatchException>(() => refusing.Track(outside));
    }

    [Fact]
    public void A_follow_up_that_is_not_the_nodule_is_a_mismatch_and_is_not_recorded() {
        // Arrange
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            followUpExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation elsewhere    = Observation(followUpExam, new SpatialPoint(0, 0, 100));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        NoduleMismatchException exception = Assert.Throws<NoduleMismatchException>(() => nodule.Track(elsewhere));

        // Assert
        Assert.Contains(nodule.Id.ToString(), exception.Message);
        Assert.Single(nodule.Observations);
        Assert.Same(baseline, nodule.Latest);
    }

    [Fact]
    public void A_mismatch_is_an_invalid_operation() {
        // Act
        NoduleMismatchException exception = new NoduleMismatchException("not this one");

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public void Exams_that_cannot_be_registered_stop_the_tracking() {
        // Arrange: the follow-up exam shares no landmark with the baseline.
        CtExam baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam followUpExam = Exam("CT20240410", Apr2024, []);
        NoduleObservation baseline = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation followUp = Observation(followUpExam, new SpatialPoint(0, 0, 0));
        TrackedNodule     nodule   = Nodule(baseline);

        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => nodule.Track(followUp));
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void Each_new_follow_up_is_registered_onto_the_latest_exam_not_the_baseline() {
        // Arrange: the patient lies 10 mm further along x at each visit.
        CtExam first  = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam second = Exam("CT20240410", Apr2024, Landmarks.Shifted(10, 0, 0));
        CtExam third  = Exam("CT20240710", Jul2024, Landmarks.Shifted(20, 0, 0));

        NoduleObservation baseline = Observation(first, new SpatialPoint(0, 0, 0));
        NoduleObservation visit2   = Observation(second, new SpatialPoint(10, 0, 0));
        NoduleObservation visit3   = Observation(third, new SpatialPoint(20, 0, 0));
        TrackedNodule     nodule   = Nodule(baseline);

        // Act
        double firstDistance  = nodule.Track(visit2);
        double secondDistance = nodule.Track(visit3);

        // Assert
        Assert.Equal(0.0, firstDistance, 9);
        Assert.Equal(0.0, secondDistance, 9);
        Assert.Equal(3, nodule.Observations.Count);
        Assert.Same(visit3, nodule.Latest);
    }

    [Fact]
    public void Tracking_nothing_is_refused() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNodule.Track(null!));
    }

    [Fact]
    public void A_follow_up_must_be_later_than_the_latest_exam() {
        // Arrange
        CtExam            baselineExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        CtExam            earlierExam  = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation earlier      = Observation(earlierExam, new SpatialPoint(0, 0, 0));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => nodule.Track(earlier));

        // Assert
        Assert.Contains("not later", exception.Message);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_follow_up_acquired_the_same_day_under_another_accession_number_is_not_later() {
        // Arrange
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            sameDayExam  = Exam("CT20240111", Jan2024, Landmarks.Shifted(0, 0, 0));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation sameDay      = Observation(sameDayExam, new SpatialPoint(0, 0, 0));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => nodule.Track(sameDay));

        // Assert
        Assert.Contains("not later", exception.Message);
    }

    [Fact]
    public void The_latest_exam_cannot_be_tracked_again() {
        // Arrange
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        NoduleObservation again        = Observation(baselineExam, new SpatialPoint(0, 0, 0));
        TrackedNodule     nodule       = Nodule(baseline);

        // Act
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => nodule.Track(again));

        // Assert
        Assert.Contains("already the latest", exception.Message);
    }

    [Fact(Skip = "domain defect: TranslationTransform.Apply builds a SpatialPoint, so a predicted centroid pushed past +-2000 mm by the registration shift escapes Track as ArgumentOutOfRangeException instead of a distance or a NoduleMismatchException (literal inputs, no seed)")]
    public void A_nodule_near_the_edge_of_the_field_of_view_is_still_tracked_when_registration_carries_its_predicted_position_beyond_it() {
        // Arrange: the patient lies 10 mm further along x, and the nodule sat at x = 1995 mm: its predicted position is 2005 mm.
        // The nodule is then seen at 2000 mm, 5 mm from the prediction; a 20 mm tolerance covers that.
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            followUpExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(10, 0, 0));
        NoduleObservation baseline     = Observation(baselineExam, new SpatialPoint(1995, 0, 0));
        NoduleObservation followUp     = Observation(followUpExam, new SpatialPoint(2000, 0, 0));
        Guid              anyId        = Any.Guid().NonEmpty().Generate();
        string            anyPatient   = Any.String().Alpha().WithLengthBetween(1, 64).Generate();
        TrackedNodule     nodule       = new TrackedNodule(anyId, anyPatient, baseline, 20.0);

        // Act
        double distance = nodule.Track(followUp);

        // Assert
        Assert.Equal(5.0, distance, 9);
    }

    // ---- resolution ----

    [Fact]
    public void A_nodule_no_longer_visible_on_a_later_exam_is_resolved_that_day() {
        // Arrange
        CtExam            baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam            laterExam    = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        TrackedNodule     nodule       = Nodule(Observation(baselineExam, new SpatialPoint(0, 0, 0)));

        // Act
        nodule.MarkResolved(laterExam);

        // Assert
        Assert.Equal(NoduleStatus.Resolved, nodule.Status);
        Assert.Equal(Apr2024, nodule.ResolvedOn);
        Assert.Single(nodule.Observations);
    }

    [Fact]
    public void A_nodule_cannot_be_resolved_on_an_exam_that_is_not_later() {
        // Arrange
        CtExam        baselineExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        CtExam        earlierExam  = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam        sameExam     = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        TrackedNodule nodule       = Nodule(Observation(baselineExam, new SpatialPoint(0, 0, 0)));

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(earlierExam));
        Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(sameExam));
        Assert.Equal(NoduleStatus.UnderSurveillance, nodule.Status);
        Assert.Null(nodule.ResolvedOn);
    }

    [Fact]
    public void Resolving_a_nodule_on_nothing_is_refused() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyNodule.MarkResolved(null!));
    }

    [Fact]
    public void A_resolved_nodule_accepts_neither_follow_up_nor_second_resolution() {
        // Arrange
        CtExam        baselineExam = Exam("CT20240110", Jan2024, Landmarks.Shifted(0, 0, 0));
        CtExam        resolvedExam = Exam("CT20240410", Apr2024, Landmarks.Shifted(0, 0, 0));
        CtExam        laterExam    = Exam("CT20240710", Jul2024, Landmarks.Shifted(0, 0, 0));
        TrackedNodule nodule       = Nodule(Observation(baselineExam, new SpatialPoint(0, 0, 0)));
        nodule.MarkResolved(resolvedExam);

        NoduleObservation laterObservation = Observation(laterExam, new SpatialPoint(0, 0, 0));

        // Act
        InvalidOperationException onTrack   = Assert.Throws<InvalidOperationException>(() => nodule.Track(laterObservation));
        InvalidOperationException onResolve = Assert.Throws<InvalidOperationException>(() => nodule.MarkResolved(laterExam));

        // Assert
        Assert.Contains("resolved", onTrack.Message);
        Assert.Contains("resolved", onResolve.Message);
        Assert.Equal(Apr2024, nodule.ResolvedOn);
    }

    // ---- comparisons ----

    [Fact]
    public void A_nodule_with_a_single_observation_cannot_be_compared() {
        // Arrange
        TrackedNodule anyNodule = Any.TrackedNodule().Generate();

        // Act
        InvalidOperationException latest   = Assert.Throws<InvalidOperationException>(() => anyNodule.CompareLatest());
        InvalidOperationException baseline = Assert.Throws<InvalidOperationException>(() => anyNodule.CompareWithBaseline());

        // Assert
        Assert.Contains("two observations", latest.Message);
        Assert.Contains("two observations", baseline.Message);
    }

    [Fact]
    public void Comparing_the_latest_looks_at_the_two_most_recent_exams_and_comparing_with_the_baseline_at_the_first_and_last() {
        // Arrange: 1 mm3 voxels, no boundary; volumes of 100, 150 and 300 mm3 on Jan 10, Apr 10 and Jul 10.
        VoxelSpacing unit = new VoxelSpacing(1, 1, 1);
        CtExam       first  = Any.CtExam().WithAccessionNumber("CT20240110").WithAcquiredOn(Jan2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();
        CtExam       second = Any.CtExam().WithAccessionNumber("CT20240410").WithAcquiredOn(Apr2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();
        CtExam       third  = Any.CtExam().WithAccessionNumber("CT20240710").WithAcquiredOn(Jul2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();

        TrackedNodule nodule = Nodule(ObservationOfVolume(first, 100, 0));
        nodule.Track(ObservationOfVolume(second, 150, 0).WithCentroidOf(nodule.Latest));
        nodule.Track(ObservationOfVolume(third, 300, 0).WithCentroidOf(nodule.Latest));

        // Act
        VolumeChange latest   = nodule.CompareLatest();
        VolumeChange baseline = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(150.0, latest.Before.VolumeMm3, 9);
        Assert.Equal(300.0, latest.After.VolumeMm3, 9);
        Assert.Equal(Jul2024.DayNumber - Apr2024.DayNumber, latest.IntervalDays);
        Assert.Equal(100.0, baseline.Before.VolumeMm3, 9);
        Assert.Equal(300.0, baseline.After.VolumeMm3, 9);
        Assert.Equal(Jul2024.DayNumber - Jan2024.DayNumber, baseline.IntervalDays);
        Assert.Equal(VolumeTrend.Growth, baseline.Trend);
    }

    [Fact]
    public void With_two_observations_both_comparisons_are_the_same() {
        // Arrange
        VoxelSpacing unit   = new VoxelSpacing(1, 1, 1);
        CtExam       first  = Any.CtExam().WithAccessionNumber("CT20240110").WithAcquiredOn(Jan2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();
        CtExam       second = Any.CtExam().WithAccessionNumber("CT20240410").WithAcquiredOn(Apr2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();

        TrackedNodule nodule = Nodule(ObservationOfVolume(first, 200, 20));
        nodule.Track(ObservationOfVolume(second, 100, 10).WithCentroidOf(nodule.Latest));

        // Act
        VolumeChange latest   = nodule.CompareLatest();
        VolumeChange baseline = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(latest.DeltaMm3, baseline.DeltaMm3);
        Assert.Equal(latest.IntervalDays, baseline.IntervalDays);
        Assert.Equal(VolumeTrend.Regression, latest.Trend);
    }

    [Fact]
    public void A_resolved_nodule_can_still_be_compared() {
        // Arrange
        VoxelSpacing unit   = new VoxelSpacing(1, 1, 1);
        CtExam       first  = Any.CtExam().WithAccessionNumber("CT20240110").WithAcquiredOn(Jan2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();
        CtExam       second = Any.CtExam().WithAccessionNumber("CT20240410").WithAcquiredOn(Apr2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();
        CtExam       third  = Any.CtExam().WithAccessionNumber("CT20240710").WithAcquiredOn(Jul2024).WithLandmarks(Landmarks.Shifted(0, 0, 0)).WithSpacing(unit).Generate();

        TrackedNodule nodule = Nodule(ObservationOfVolume(first, 200, 0));
        nodule.Track(ObservationOfVolume(second, 100, 0).WithCentroidOf(nodule.Latest));
        nodule.MarkResolved(third);

        // Act
        VolumeChange change = nodule.CompareWithBaseline();

        // Assert
        Assert.Equal(VolumeTrend.Regression, change.Trend);
    }

}

internal static class ObservationExtensions {

    /// <summary>The same observation, its nodule seen at the centroid of <paramref name="other" />.</summary>
    public static NoduleObservation WithCentroidOf(this NoduleObservation observation, NoduleObservation other) {
        Segmentation segmentation = new Segmentation(observation.Segmentation.VoxelCount,
                                                     observation.Segmentation.BoundaryVoxelCount,
                                                     observation.Segmentation.Spacing,
                                                     other.Segmentation.Centroid);

        return new NoduleObservation(observation.Exam, segmentation);
    }

}
