using PulmonaryNodules.Domain;
using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class RegistrationTests {

    [Fact]
    public void A_patient_shifted_rigidly_is_registered_by_that_shift_with_no_residual() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyBaselineLandmarks = Scans.AnyLandmarks(4);
        IReadOnlyList<AnatomicalLandmark> followUpLandmarks    = Scans.Shifted(anyBaselineLandmarks, 5.0, -3.0, 12.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks);

        // Assert
        Assert.Equal(5.0, transform.DxMm, 6);
        Assert.Equal(-3.0, transform.DyMm, 6);
        Assert.Equal(12.0, transform.DzMm, 6);
        Assert.Equal(0.0, transform.ResidualErrorMm, 6);
    }

    [Fact]
    public void Exams_sharing_only_two_landmarks_cannot_be_registered() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyBaselineLandmarks = Scans.AnyLandmarks(4);
        IReadOnlyList<AnatomicalLandmark> followUpLandmarks    = Scans.Shifted(anyBaselineLandmarks.Take(2), 5.0, -3.0, 12.0);

        // Act
        RegistrationFailedException thrown = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks));

        // Assert
        Assert.Contains("found 2", thrown.Message);
    }

    [Fact]
    public void Landmarks_that_moved_inconsistently_cannot_be_registered_reliably() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyBaselineLandmarks = Scans.AnyLandmarks(3);
        // Two landmarks stay put while the third moved 60 mm: the RMS residual is about 28 mm.
        IReadOnlyList<AnatomicalLandmark> followUpLandmarks = Scans.Shifted(anyBaselineLandmarks.Take(2), 0.0, 0.0, 0.0)
                                                                   .Concat(Scans.Shifted(anyBaselineLandmarks.Skip(2), 60.0, 0.0, 0.0))
                                                                   .ToList();

        // Act
        RegistrationFailedException thrown = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks));

        // Assert
        Assert.Contains("residual", thrown.Message);
    }

    [Fact]
    public void Applying_a_transform_translates_the_point() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10.0, -20.0, 5.0, 1.5);

        // Act
        SpatialPoint mapped = transform.Apply(new SpatialPoint(100.0, 50.0, -200.0));

        // Assert
        Assert.Equal(new SpatialPoint(110.0, 30.0, -195.0), mapped);
    }

    [Fact]
    public void The_inverse_negates_the_shift_and_keeps_the_residual() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10.0, -20.0, 5.0, 1.5);

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal((-10.0, 20.0, -5.0, 1.5), (inverse.DxMm, inverse.DyMm, inverse.DzMm, inverse.ResidualErrorMm));
    }

}
