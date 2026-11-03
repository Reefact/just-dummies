using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class NoduleObservationTests {

    [Fact]
    public void An_observation_keeps_the_exam_and_the_segmentation_it_was_made_on() {
        // Arrange
        VoxelSpacing sharedSpacing = new VoxelSpacing(0.5, 0.5, 1.0);
        CtExam exam = Any.CtExam().WithSpacing(sharedSpacing).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(sharedSpacing).Generate();

        // Act
        NoduleObservation observation = new NoduleObservation(exam, segmentation);

        // Assert
        Assert.Same(exam, observation.Exam);
        Assert.Same(segmentation, observation.Segmentation);
    }

    [Fact]
    public void A_segmentation_drawn_on_an_equal_but_distinct_spacing_is_accepted() {
        // Arrange
        CtExam exam = Any.CtExam().WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0)).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0)).Generate();

        // Act
        NoduleObservation observation = new NoduleObservation(exam, segmentation);

        // Assert
        Assert.Same(segmentation, observation.Segmentation);
    }

    [Theory]
    [InlineData(0.6, 0.5, 1.0)]
    [InlineData(0.5, 0.6, 1.0)]
    [InlineData(0.5, 0.5, 1.5)]
    public void A_segmentation_drawn_on_another_spacing_than_the_exams_is_refused(double rowMm, double columnMm, double sliceMm) {
        // Arrange
        CtExam exam = Any.CtExam().WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0)).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(rowMm, columnMm, sliceMm)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new NoduleObservation(exam, segmentation));

        // Assert
        Assert.Equal("segmentation", refusal.ParamName);
    }

    [Fact]
    public void The_volume_of_an_observation_is_the_one_its_segmentation_measures() {
        // Arrange
        // 100 unit voxels, 10 of them on the boundary: 100 mm3, uncertain by 5 mm3.
        VoxelSpacing unitCube = new VoxelSpacing(1.0, 1.0, 1.0);
        NoduleObservation observation = Any.NoduleObservation()
                                           .WithExam(exam => exam.WithSpacing(unitCube))
                                           .WithSegmentation(segmentation => segmentation.WithSpacing(unitCube).WithVoxelCount(100).WithBoundaryVoxelCount(10))
                                           .Generate();

        // Act
        MeasuredVolume volume = observation.Volume;

        // Assert
        Assert.Equal(100.0, volume.VolumeMm3);
        Assert.Equal(5.0, volume.UncertaintyMm3);
    }

    [Fact]
    public void An_observation_needs_both_an_exam_and_a_segmentation() {
        // Arrange
        NoduleObservation anyObservation = Any.NoduleObservation().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new NoduleObservation(null!, anyObservation.Segmentation));
        Assert.Throws<ArgumentNullException>(() => new NoduleObservation(anyObservation.Exam, null!));
    }

}
