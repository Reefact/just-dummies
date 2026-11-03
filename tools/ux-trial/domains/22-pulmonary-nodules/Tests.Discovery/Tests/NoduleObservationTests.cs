namespace PulmonaryNodules.Tests.Discovery;

public sealed class NoduleObservationTests {

    [Fact]
    public void An_observation_keeps_its_exam_and_its_segmentation() {
        // Arrange
        VoxelSpacing spacing      = new VoxelSpacing(0.7, 0.7, 0.5);
        CtExam       exam         = Any.CtExam().WithSpacing(spacing).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(spacing).Generate();

        // Act
        NoduleObservation observation = new NoduleObservation(exam, segmentation);

        // Assert
        Assert.Same(exam, observation.Exam);
        Assert.Same(segmentation, observation.Segmentation);
    }

    [Fact]
    public void An_observation_reports_the_volume_its_segmentation_measures() {
        // Arrange
        VoxelSpacing      spacing     = new VoxelSpacing(0.5, 0.5, 0.5);
        NoduleObservation observation = Any.NoduleObservation()
                                           .WithExam(e => e.WithSpacing(spacing))
                                           .WithSegmentation(s => s.WithVoxelCount(800).WithBoundaryVoxelCount(160))
                                           .Generate();

        // Act
        MeasuredVolume volume = observation.Volume;

        // Assert
        Assert.Equal(100.0, volume.VolumeMm3, 9);
        Assert.Equal(10.0, volume.UncertaintyMm3, 9);
    }

    [Fact]
    public void A_segmentation_drawn_on_another_voxel_grid_is_refused() {
        // Arrange
        CtExam       exam         = Any.CtExam().WithSpacing(new VoxelSpacing(0.7, 0.7, 0.5)).Generate();
        Segmentation segmentation = Any.Segmentation().WithSpacing(new VoxelSpacing(0.7, 0.7, 0.4)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new NoduleObservation(exam, segmentation));

        // Assert
        Assert.Equal("segmentation", exception.ParamName);
    }

    [Fact]
    public void An_observation_needs_an_exam_and_a_segmentation() {
        // Arrange
        CtExam       anyExam         = Any.CtExam().Generate();
        Segmentation anySegmentation = Any.Segmentation().Generate();

        // Act
        ArgumentNullException noExam         = Assert.Throws<ArgumentNullException>(() => new NoduleObservation(null!, anySegmentation));
        ArgumentNullException noSegmentation = Assert.Throws<ArgumentNullException>(() => new NoduleObservation(anyExam, null!));

        // Assert
        Assert.Equal("exam", noExam.ParamName);
        Assert.Equal("segmentation", noSegmentation.ParamName);
    }

    [Fact]
    public void An_arbitrary_observation_is_segmented_on_its_own_exams_grid() {
        // Arrange / Act
        Exception? exception = Record.Exception(() => Any.NoduleObservation().Generate());

        // Assert
        Assert.Null(exception);
    }

}
