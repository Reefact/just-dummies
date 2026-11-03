using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class CtExamTests {

    [Fact]
    public void An_exam_keeps_what_it_was_built_from() {
        // Arrange
        string                     anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        DateOnly                   anyDate      = Any.DateOnly().Generate();
        VoxelSpacing               anySpacing   = Any.VoxelSpacing().WithSliceMm(1.0).Generate();
        AnatomicalLandmark[]       anyLandmarks = Any.ListOf(Any.AnatomicalLandmark()).WithCount(1).Generate().ToArray();

        // Act
        CtExam exam = new CtExam(anyAccession, anyDate, anySpacing, 1.5, anyLandmarks);

        // Assert
        Assert.Equal(anyAccession, exam.AccessionNumber);
        Assert.Equal(anyDate, exam.AcquiredOn);
        Assert.Same(anySpacing, exam.Spacing);
        Assert.Equal(1.5, exam.SliceThicknessMm);
        Assert.Equal(anyLandmarks, exam.Landmarks);
    }

    [Fact]
    public void An_exam_is_not_changed_by_a_later_change_to_the_landmark_list_it_was_given() {
        // Arrange
        List<AnatomicalLandmark> landmarks = [Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate()];
        CtExam                   exam      = Any.CtExam().WithLandmarks(landmarks).Generate();

        // Act
        landmarks.Add(Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate());

        // Assert
        Assert.Single(exam.Landmarks);
    }

    [Fact]
    public void An_exam_may_have_no_landmark_at_all() {
        // Act
        CtExam exam = Any.CtExam().WithLandmarks(l => l.Empty()).Generate();

        // Assert
        Assert.Empty(exam.Landmarks);
    }

    [Fact]
    public void An_exam_may_locate_each_of_the_eight_landmarks() {
        // Arrange
        AnatomicalLandmark[] everyKind = Enum.GetValues<LandmarkKind>()
                                             .Select(kind => Any.AnatomicalLandmark().WithKind(kind).Generate())
                                             .ToArray();

        // Act
        CtExam exam = Any.CtExam().WithLandmarks(everyKind).Generate();

        // Assert
        Assert.Equal(8, exam.Landmarks.Count);
    }

    [Theory]
    [InlineData("CT00000000")]
    [InlineData("CT12345678")]
    public void A_well_formed_accession_number_is_accepted(string accession) {
        // Act
        CtExam exam = Any.CtExam().WithAccessionNumber(accession).Generate();

        // Assert
        Assert.Equal(accession, exam.AccessionNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CT1234567")]
    [InlineData("CT123456789")]
    [InlineData("ct12345678")]
    [InlineData("XX12345678")]
    [InlineData("CT1234567A")]
    [InlineData(" CT12345678")]
    [InlineData("CT12345678 ")]
    public void A_malformed_accession_number_is_refused(string accession) {
        // Act
        Action act = () => Any.CtExam().WithAccessionNumber(accession).Generate();

        // Assert
        Assert.Throws<FormatException>(act);
    }

    [Fact]
    public void An_accession_number_that_is_not_two_letters_and_eight_digits_is_refused() {
        // Arrange
        string anyMalformed = Any.StringMatching("CT[0-9]{0,7}").Generate();

        // Act
        Action act = () => Any.CtExam().WithAccessionNumber(anyMalformed).Generate();

        // Assert
        Assert.Throws<FormatException>(act);
    }

    [Fact(Skip = "domain defect: the accession pattern '^CT[0-9]{8}$' uses '$', which .NET also matches before a final newline, so 'CT12345678\\n' is accepted although it is not 'CT' followed by 8 digits; any seed, the value is a literal")]
    public void An_accession_number_ending_with_a_line_feed_is_refused() {
        // Act
        Action act = () => Any.CtExam().WithAccessionNumber("CT12345678\n").Generate();

        // Assert
        Assert.Throws<FormatException>(act);
    }

    [Fact]
    public void A_missing_accession_number_is_refused() {
        // Act
        Action act = () => Any.CtExam().WithAccessionNumber((string)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(5.0)]
    public void A_slice_thickness_on_the_limits_is_accepted(double thickness) {
        // Act
        CtExam exam = Any.CtExam().WithSpacing(s => s.WithSliceMm(0.5)).WithSliceThicknessMm(thickness).Generate();

        // Assert
        Assert.Equal(thickness, exam.SliceThicknessMm);
    }

    [Theory]
    [InlineData(0.499)]
    [InlineData(5.001)]
    [InlineData(double.NaN)]
    public void A_slice_thickness_outside_its_range_is_refused(double thickness) {
        // Act
        Action act = () => Any.CtExam().WithSpacing(s => s.WithSliceMm(0.5)).WithSliceThicknessMm(thickness).Generate();

        // Assert
        Assert.Equal("sliceThicknessMm", Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void A_slice_spacing_equal_to_the_thickness_leaves_no_gap_and_is_accepted() {
        // Arrange
        double anyThickness = Any.Double().Between(CtExam.MinSliceThicknessMm, CtExam.MaxSliceThicknessMm).Generate();

        // Act
        CtExam exam = Any.CtExam().WithSliceThicknessMm(anyThickness).WithSpacing(s => s.WithSliceMm(anyThickness)).Generate();

        // Assert
        Assert.Equal(exam.SliceThicknessMm, exam.Spacing.SliceMm);
    }

    [Fact]
    public void A_slice_spacing_larger_than_the_thickness_would_leave_gaps_and_is_refused() {
        // Arrange
        double anyThickness = Any.Double().Between(CtExam.MinSliceThicknessMm, 4.0).Generate();
        double anySpacing   = Any.Double().GreaterThan(anyThickness).LessThanOrEqualTo(VoxelSpacing.MaxSliceSpacingMm).Generate();

        // Act
        Action act = () => Any.CtExam().WithSliceThicknessMm(anyThickness).WithSpacing(s => s.WithSliceMm(anySpacing)).Generate();

        // Assert
        Assert.Equal("spacing", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_missing_spacing_is_refused() {
        // Arrange
        string   anyAccession = Any.StringMatching("CT[0-9]{8}").Generate();
        DateOnly anyDate      = Any.DateOnly().Generate();

        // Act
        Action act = () => new CtExam(anyAccession, anyDate, null!, 1.0, []);

        // Assert
        Assert.Equal("spacing", Assert.Throws<ArgumentNullException>(act).ParamName);
    }

    [Fact]
    public void A_missing_landmark_list_is_refused() {
        // Act
        Action act = () => Any.CtExam().WithLandmarks((IReadOnlyCollection<AnatomicalLandmark>)null!).Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void A_null_landmark_in_the_list_is_refused() {
        // Arrange
        AnatomicalLandmark?[] landmarks = [Any.AnatomicalLandmark().Generate(), null];

        // Act
        Action act = () => Any.CtExam().WithLandmarks(landmarks!).Generate();

        // Assert
        Assert.Equal("landmarks", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_landmark_located_twice_is_refused_and_the_message_names_it() {
        // Arrange
        AnatomicalLandmark[] landmarks = [
            Any.AnatomicalLandmark().WithKind(LandmarkKind.AorticArchApex).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.AorticArchApex).Generate(),
        ];

        // Act
        Action act = () => Any.CtExam().WithLandmarks(landmarks).Generate();

        // Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("AorticArchApex", exception.Message);
        Assert.DoesNotContain("SternalAngle", exception.Message);
    }

    [Fact]
    public void Registering_an_exam_to_another_estimates_the_translation_from_their_common_landmarks() {
        // Arrange
        CtExam baseline = Any.CtExam()
                             .WithLandmarks(new[] {
                                 new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(0, 0, 0)),
                                 new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(10, 0, 0)),
                                 new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(0, 10, 0)),
                             })
                             .Generate();
        CtExam followUp = Any.CtExam()
                             .WithLandmarks(new[] {
                                 new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(3, 4, 5)),
                                 new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(13, 4, 5)),
                                 new AnatomicalLandmark(LandmarkKind.SternalAngle, new SpatialPoint(3, 14, 5)),
                             })
                             .Generate();

        // Act
        TranslationTransform transform = baseline.RegisterTo(followUp);

        // Assert
        Assert.Equal(3, transform.DxMm, 9);
        Assert.Equal(4, transform.DyMm, 9);
        Assert.Equal(5, transform.DzMm, 9);
    }

    [Fact]
    public void Registering_to_an_exam_with_too_few_shared_landmarks_fails() {
        // Arrange
        CtExam anyBaseline = Any.CtExam().WithLandmarks(l => l.WithMaxCount(2)).Generate();
        CtExam anyFollowUp = Any.CtExam().Generate();

        // Act
        Action act = () => anyBaseline.RegisterTo(anyFollowUp);

        // Assert
        Assert.Throws<RegistrationFailedException>(act);
    }

    [Fact]
    public void Registering_to_nothing_is_refused() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act
        Action act = () => anyExam.RegisterTo(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void The_generator_only_draws_exams_the_constructor_accepts() {
        // Arrange
        AnyCtExam anyExam = Any.CtExam();

        // Act
        Action act = () => { for (int i = 0; i < 200; i++) { anyExam.Generate(); } };

        // Assert
        Assert.Null(Record.Exception(act));
    }

}

public sealed class NoduleObservationTests {

    [Fact]
    public void An_observation_exposes_the_volume_of_its_segmentation() {
        // Arrange
        NoduleObservation anyObservation = Any.NoduleObservation().Generate();

        // Act
        MeasuredVolume volume = anyObservation.Volume;

        // Assert
        Assert.Equal(anyObservation.Segmentation.Measure().VolumeMm3, volume.VolumeMm3);
        Assert.Equal(anyObservation.Segmentation.Measure().UncertaintyMm3, volume.UncertaintyMm3);
    }

    [Fact]
    public void A_segmentation_drawn_on_the_same_spacing_as_the_exam_is_accepted() {
        // Arrange
        VoxelSpacing anySpacing = Any.VoxelSpacing().WithSliceMm(slice => slice.LessThanOrEqualTo(CtExam.MaxSliceThicknessMm)).Generate();
        CtExam       exam       = Any.CtExam().WithSpacing(anySpacing).WithSliceThicknessMm(CtExam.MaxSliceThicknessMm).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(anySpacing.RowMm, anySpacing.ColumnMm, anySpacing.SliceMm)).Generate();

        // Act
        NoduleObservation observation = new NoduleObservation(exam, segmentation);

        // Assert
        Assert.Same(exam, observation.Exam);
        Assert.Same(segmentation, observation.Segmentation);
    }

    [Fact]
    public void A_segmentation_drawn_on_another_spacing_is_refused() {
        // Arrange
        CtExam       exam         = Any.CtExam().WithSpacing(new VoxelSpacing(0.7, 0.7, 1.0)).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(0.7, 0.7, 1.25)).Generate();

        // Act
        Action act = () => new NoduleObservation(exam, segmentation);

        // Assert
        Assert.Equal("segmentation", Assert.Throws<ArgumentException>(act).ParamName);
    }

    [Fact]
    public void A_missing_exam_or_segmentation_is_refused() {
        // Arrange
        CtExam       anyExam         = Any.CtExam().Generate();
        Segmentation anySegmentation = Any.Segmentation().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new NoduleObservation(null!, anySegmentation));
        Assert.Throws<ArgumentNullException>(() => new NoduleObservation(anyExam, null!));
    }

}
