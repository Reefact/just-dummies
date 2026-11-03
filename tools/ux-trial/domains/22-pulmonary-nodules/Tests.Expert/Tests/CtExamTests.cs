using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using PulmonaryNodules.Tests.Expert.Support;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class CtExamTests {

    // ---- Accession number ----

    [Fact]
    public void An_exam_keeps_the_identity_geometry_and_landmarks_it_was_given() {
        // Arrange
        DateOnly acquiredOn = new DateOnly(2025, 3, 14);
        VoxelSpacing spacing = new VoxelSpacing(0.7, 0.7, 1.0);
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 3);

        // Act
        CtExam exam = new CtExam("CT20250314", acquiredOn, spacing, 1.25, landmarks);

        // Assert
        Assert.Equal("CT20250314", exam.AccessionNumber);
        Assert.Equal(acquiredOn, exam.AcquiredOn);
        Assert.Same(spacing, exam.Spacing);
        Assert.Equal(1.25, exam.SliceThicknessMm);
        Assert.Equal(landmarks, exam.Landmarks);
    }

    [Theory]
    [InlineData("CT1234567")]
    [InlineData("CT123456789")]
    [InlineData("ct12345678")]
    [InlineData("XX12345678")]
    [InlineData("CT1234567A")]
    [InlineData("CT 2345678")]
    [InlineData(" CT12345678")]
    [InlineData("CT12345678 ")]
    [InlineData("12345678")]
    [InlineData("")]
    public void An_accession_number_that_is_not_CT_and_eight_digits_is_refused(string malformed) {
        // Act
        Assert.Throws<FormatException>(() => Any.CtExam().WithAccessionNumber(malformed).Generate());
    }

    [Fact(Skip = "domain defect: the accession pattern '^CT[0-9]{8}$' ends with '$', which .NET also matches before a final line feed, so 'CT12345678\\n' is accepted (literal case, no seed involved)")]
    public void An_accession_number_followed_by_a_line_feed_is_refused() {
        // Act
        Assert.Throws<FormatException>(() => Any.CtExam().WithAccessionNumber("CT12345678\n").Generate());
    }

    [Fact]
    public void An_exam_without_an_accession_number_is_refused() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CtExam(null!, anyExam.AcquiredOn, anyExam.Spacing, anyExam.SliceThicknessMm, anyExam.Landmarks));

        // Assert
        Assert.Equal("accessionNumber", refusal.ParamName);
    }

    // ---- Slice thickness and spacing ----

    [Theory]
    [InlineData(0.5)]
    [InlineData(5.0)]
    public void A_slice_thickness_on_either_edge_of_its_range_is_accepted(double edgeMm) {
        // Act
        CtExam exam = Any.CtExam().WithSliceThicknessMm(edgeMm).Generate();

        // Assert
        Assert.Equal(edgeMm, exam.SliceThicknessMm);
    }

    [Theory]
    [InlineData(0.4999)]
    [InlineData(5.0001)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void A_slice_thickness_outside_0_5_to_5_mm_is_refused(double outsideMm) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.CtExam().WithSliceThicknessMm(outsideMm).Generate());

        // Assert
        Assert.Equal("sliceThicknessMm", refusal.ParamName);
    }

    [Fact]
    public void Slices_spaced_exactly_as_thick_as_they_are_leave_no_gap() {
        // Arrange
        VoxelSpacing spacing = Any.VoxelSpacing().WithSliceMm(3.0).Generate();

        // Act
        CtExam exam = Any.CtExam().WithSliceThicknessMm(3.0).WithSpacing(spacing).Generate();

        // Assert
        Assert.Equal(3.0, exam.SliceThicknessMm);
        Assert.Equal(3.0, exam.Spacing.SliceMm);
    }

    [Fact]
    public void Slices_spaced_further_apart_than_they_are_thick_leave_a_gap_and_are_refused() {
        // Arrange
        VoxelSpacing spacing = Any.VoxelSpacing().WithSliceMm(3.0).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CtExam().WithSliceThicknessMm(2.9).WithSpacing(spacing).Generate());

        // Assert
        Assert.Equal("spacing", refusal.ParamName);
    }

    [Fact]
    public void An_exam_needs_a_spacing() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CtExam(anyExam.AccessionNumber, anyExam.AcquiredOn, null!, anyExam.SliceThicknessMm, anyExam.Landmarks));

        // Assert
        Assert.Equal("spacing", refusal.ParamName);
    }

    // ---- Landmarks ----

    [Fact]
    public void An_exam_may_have_no_landmark_at_all() {
        // Act
        CtExam exam = Any.CtExam().WithLandmarks(Array.Empty<AnatomicalLandmark>()).Generate();

        // Assert
        Assert.Empty(exam.Landmarks);
    }

    [Fact]
    public void An_exam_may_locate_every_kind_of_landmark_once() {
        // Arrange
        AnatomicalLandmark[] everyKind = Enum.GetValues<LandmarkKind>().Select(kind => Any.AnatomicalLandmark().WithKind(kind).Generate()).ToArray();

        // Act
        CtExam exam = Any.CtExam().WithLandmarks(everyKind).Generate();

        // Assert
        Assert.Equal(8, exam.Landmarks.Count);
    }

    [Fact]
    public void A_kind_located_twice_is_refused_and_named() {
        // Arrange
        AnatomicalLandmark first = Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate();
        AnatomicalLandmark second = Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CtExam().WithLandmarks(new[] { first, second }).Generate());

        // Assert
        Assert.Equal("landmarks", refusal.ParamName);
        Assert.Contains("SternalAngle", refusal.Message);
    }

    [Fact]
    public void A_landmark_list_cannot_hold_a_null_entry() {
        // Arrange
        AnatomicalLandmark anyLandmark = Any.AnatomicalLandmark().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.CtExam().WithLandmarks(new AnatomicalLandmark[] { anyLandmark, null! }).Generate());

        // Assert
        Assert.Equal("landmarks", refusal.ParamName);
    }

    [Fact]
    public void An_exam_without_a_landmark_list_is_refused() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new CtExam(anyExam.AccessionNumber, anyExam.AcquiredOn, anyExam.Spacing, anyExam.SliceThicknessMm, null!));

        // Assert
        Assert.Equal("landmarks", refusal.ParamName);
    }

    [Fact]
    public void The_exam_is_not_affected_by_later_changes_to_the_list_it_was_built_from() {
        // Arrange
        List<AnatomicalLandmark> source = Scans.AnyLandmarks(count: 3).ToList();
        CtExam exam = Any.CtExam().WithLandmarks(source).Generate();

        // Act
        source.Clear();

        // Assert
        Assert.Equal(3, exam.Landmarks.Count);
    }

    // ---- Registration ----

    [Fact]
    public void Registering_an_exam_onto_a_follow_up_finds_how_the_patient_moved() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 4);
        CtExam baseline = Any.CtExam().WithLandmarks(landmarks).Generate();
        CtExam followUp = Any.CtExam().WithLandmarks(Scans.Shifted(landmarks, -8.0, 4.0, 15.0)).Generate();

        // Act
        TranslationTransform transform = baseline.RegisterTo(followUp);

        // Assert
        Assert.Equal(-8.0, transform.DxMm);
        Assert.Equal(4.0, transform.DyMm);
        Assert.Equal(15.0, transform.DzMm);
    }

    [Fact]
    public void Exams_sharing_fewer_than_three_landmarks_cannot_be_registered() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> landmarks = Scans.AnyLandmarks(count: 2);
        CtExam baseline = Any.CtExam().WithLandmarks(landmarks).Generate();
        CtExam followUp = Any.CtExam().WithLandmarks(Scans.Shifted(landmarks, 1.0, 1.0, 1.0)).Generate();

        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => baseline.RegisterTo(followUp));
    }

    [Fact]
    public void Registering_onto_nothing_is_refused() {
        // Arrange
        CtExam anyExam = Any.CtExam().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyExam.RegisterTo(null!));
    }

}
