using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

using Xunit;

namespace PulmonaryNodules.Tests.Expert;

public sealed class CtExamTests {

    [Fact]
    public void An_accession_number_is_CT_followed_by_eight_digits() {
        // Arrange — seven digits.
        AnyCtExam anyExam = Any.CtExam().WithAccessionNumber("CT1234567");

        // Act / Assert
        Assert.Throws<FormatException>(() => anyExam.Generate());
    }

    [Fact]
    public void A_landmark_kind_is_located_once_per_exam() {
        // Arrange
        AnatomicalLandmark       anyCarina      = Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate();
        AnatomicalLandmark       anyOtherCarina = Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate();
        AnyCtExam                anyExam        = Any.CtExam().WithLandmarks([anyCarina, anyOtherCarina]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyExam.Generate());

        // Assert
        Assert.Contains("CarinaBifurcation", error.Message);
    }

    [Fact]
    public void Slices_spaced_further_apart_than_they_are_thick_leave_gaps_and_are_refused() {
        // Arrange
        AnyCtExam anyGappedExam = Any.CtExam()
                                     .WithSpacing(s => s.WithSliceMm(2.5))
                                     .WithSliceThicknessMm(1.0);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyGappedExam.Generate());

        // Assert
        Assert.Equal("spacing", error.ParamName);
    }

    [Fact]
    public void A_segmentation_drawn_on_another_acquisition_grid_does_not_belong_to_the_exam() {
        // Arrange
        CtExam       anyExam         = Any.CtExam().WithSpacing(s => s.WithSliceMm(1.0)).Generate();
        Segmentation anySegmentation = Any.Segmentation().WithSpacing(s => s.WithSliceMm(0.5)).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new NoduleObservation(anyExam, anySegmentation));

        // Assert
        Assert.Equal("segmentation", error.ParamName);
    }

}
