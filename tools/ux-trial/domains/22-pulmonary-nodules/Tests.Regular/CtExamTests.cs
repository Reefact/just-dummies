using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class CtExamTests {

    [Theory]
    [InlineData("CT1234567")]
    [InlineData("MR12345678")]
    [InlineData("ct12345678")]
    public void A_malformed_accession_number_is_rejected(string accessionNumber) {
        // Arrange
        AnyCtExam anyExam = Arbitrary.CtExam().WithAccessionNumber(accessionNumber);

        // Act
        FormatException thrown = Assert.Throws<FormatException>(() => anyExam.Generate());

        // Assert
        Assert.Contains(accessionNumber, thrown.Message);
    }

    [Fact]
    public void A_landmark_located_twice_in_one_exam_is_rejected() {
        // Arrange
        SpatialPoint anyPosition      = Arbitrary.SpatialPoint().Generate();
        SpatialPoint anyOtherPosition = Arbitrary.SpatialPoint().Generate();
        AnyCtExam    anyExam = Arbitrary.CtExam().WithLandmarks([
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, anyPosition),
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, anyOtherPosition),
        ]);

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => anyExam.Generate());

        // Assert
        Assert.Contains(nameof(LandmarkKind.CarinaBifurcation), thrown.Message);
    }

    [Fact]
    public void A_slice_spacing_coarser_than_the_slice_thickness_is_rejected() {
        // Arrange
        AnyCtExam anyExam = Arbitrary.CtExam()
                                     .WithSpacing(Arbitrary.VoxelSpacing().WithSliceMm(2.5).Generate())
                                     .WithSliceThicknessMm(1.25);

        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => anyExam.Generate());

        // Assert
        Assert.Equal("spacing", thrown.ParamName);
    }

    [Fact]
    public void Registering_two_exams_maps_the_baseline_landmarks_onto_the_follow_up() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = Scans.AnyLandmarks(5);
        CtExam anyBaseline = Arbitrary.CtExam().WithLandmarks(anyLandmarks).Generate();
        CtExam anyFollowUp = Arbitrary.CtExam().WithLandmarks(Scans.Shifted(anyLandmarks, -8.0, 0.0, 15.0)).Generate();

        // Act
        TranslationTransform transform = anyBaseline.RegisterTo(anyFollowUp);

        // Assert
        Assert.Equal(-8.0, transform.DxMm, 6);
        Assert.Equal(0.0, transform.DyMm, 6);
        Assert.Equal(15.0, transform.DzMm, 6);
    }

}
