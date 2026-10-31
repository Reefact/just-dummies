using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Discovery;

public sealed class ExamTests {

    [Fact]
    public void An_exam_rejects_an_accession_number_that_is_not_CT_followed_by_eight_digits() {
        // Arrange
        AnyCtExam anyExamWithRadiographAccession = Any.CtExam().WithAccessionNumber("XR12345678");

        // Act
        FormatException failure = Assert.Throws<FormatException>(() => anyExamWithRadiographAccession.Generate());

        // Assert
        Assert.Contains("XR12345678", failure.Message);
    }

    [Fact]
    public void An_exam_rejects_a_landmark_located_twice() {
        // Arrange
        AnatomicalLandmark[] landmarks = [
            Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate(),
        ];
        AnyCtExam anyExam = Any.CtExam().WithLandmarks(landmarks);

        // Act
        ArgumentException failure = Assert.Throws<ArgumentException>(() => anyExam.Generate());

        // Assert
        Assert.Contains("CarinaBifurcation", failure.Message);
    }

    [Fact]
    public void An_exam_rejects_a_slice_spacing_wider_than_its_slice_thickness() {
        // Arrange
        VoxelSpacing anyGappedSpacing = Any.VoxelSpacing().WithSliceMm(2.0).Generate();
        AnyCtExam    anyExam          = Any.CtExam().WithSpacing(anyGappedSpacing).WithSliceThicknessMm(1.0);

        // Act
        ArgumentException failure = Assert.Throws<ArgumentException>(() => anyExam.Generate());

        // Assert
        Assert.Equal("spacing", failure.ParamName);
    }

    [Fact]
    public void A_segmentation_drawn_on_another_spacing_does_not_belong_to_the_exam() {
        // Arrange
        CtExam       anyExam         = Any.CtExam().WithSpacing(new VoxelSpacing(0.7, 0.7, 0.5)).Generate();
        Segmentation anySegmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(0.7, 0.7, 1.0)).Generate();

        // Act
        ArgumentException failure = Assert.Throws<ArgumentException>(() => new NoduleObservation(anyExam, anySegmentation));

        // Assert
        Assert.Equal("segmentation", failure.ParamName);
    }

}
