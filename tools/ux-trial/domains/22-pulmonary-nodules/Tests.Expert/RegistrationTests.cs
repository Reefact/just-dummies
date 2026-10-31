using System.Collections.Generic;
using System.Linq;

using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

using Xunit;

namespace PulmonaryNodules.Tests.Expert;

public sealed class RegistrationTests {

    private static List<AnatomicalLandmark> Moved(IEnumerable<AnatomicalLandmark> landmarks, double dxMm, double dyMm, double dzMm) {
        return landmarks.Select(l => new AnatomicalLandmark(l.Kind, l.Position.Offset(dxMm, dyMm, dzMm))).ToList();
    }

    [Fact]
    public void Registration_recovers_the_shift_of_a_patient_repositioned_between_two_exams() {
        // Arrange
        IReadOnlyCollection<AnatomicalLandmark> anyBaselineLandmarks = Thorax.Fiducials().Generate();
        List<AnatomicalLandmark>                followUpLandmarks    = Moved(anyBaselineLandmarks, 12.0, -8.0, 30.0);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks);

        // Assert
        Assert.Equal(12.0, transform.DxMm, precision: 9);
        Assert.Equal(-8.0, transform.DyMm, precision: 9);
        Assert.Equal(30.0, transform.DzMm, precision: 9);
        Assert.Equal(0.0, transform.ResidualErrorMm, precision: 9);
    }

    [Fact]
    public void A_landmark_located_in_only_one_exam_takes_no_part_in_the_registration() {
        // Arrange — the follow-up locates one more structure than the baseline, anywhere in the frame.
        ISet<LandmarkKind> anySharedKinds = Any.SetOf(Any.Enum<LandmarkKind>()).WithCount(3).Generate();
        LandmarkKind       anyExtraKind   = Any.Enum<LandmarkKind>().Except(anySharedKinds.ToArray()).Generate();

        List<AnatomicalLandmark> anyBaselineLandmarks = anySharedKinds.Select(kind => Any.AnatomicalLandmark().WithKind(kind).WithPosition(Thorax.Inside).Generate()).ToList();
        List<AnatomicalLandmark> followUpLandmarks    = Moved(anyBaselineLandmarks, 5.0, 5.0, -10.0);
        followUpLandmarks.Add(Any.AnatomicalLandmark().WithKind(anyExtraKind).Generate());

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks);

        // Assert
        Assert.Equal(5.0, transform.DxMm, precision: 9);
        Assert.Equal(5.0, transform.DyMm, precision: 9);
        Assert.Equal(-10.0, transform.DzMm, precision: 9);
    }

    [Fact]
    public void Two_shared_landmarks_are_not_enough_to_register_two_exams() {
        // Arrange
        List<AnatomicalLandmark> anyBaselineLandmarks = Any.ListOf(Any.AnatomicalLandmark().WithPosition(Thorax.Inside))
                                                           .Distinct(LandmarkKindComparer.Instance)
                                                           .WithCount(2)
                                                           .Generate();
        TranslationTransform     anyRepositioning     = Thorax.PatientRepositioning().Generate();
        List<AnatomicalLandmark> followUpLandmarks    = anyBaselineLandmarks.Select(l => new AnatomicalLandmark(l.Kind, anyRepositioning.Apply(l.Position))).ToList();

        // Act
        RegistrationFailedException error = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks));

        // Assert
        Assert.Contains("found 2", error.Message);
    }

    [Fact]
    public void Landmarks_that_disagree_by_more_than_15_mm_cannot_be_registered_reliably() {
        // Arrange — two landmarks moved 30 mm in opposite directions: an RMS residual of about 24.5 mm.
        IReadOnlyCollection<AnatomicalLandmark> anyBaselineLandmarks = Any.ListOf(Any.AnatomicalLandmark().WithPosition(Thorax.Inside))
                                                                          .Distinct(LandmarkKindComparer.Instance)
                                                                          .WithCount(3)
                                                                          .Generate();
        AnatomicalLandmark[]     baseline          = anyBaselineLandmarks.ToArray();
        List<AnatomicalLandmark> followUpLandmarks = [
            new AnatomicalLandmark(baseline[0].Kind, baseline[0].Position.Offset(30.0, 0.0, 0.0)),
            new AnatomicalLandmark(baseline[1].Kind, baseline[1].Position.Offset(-30.0, 0.0, 0.0)),
            baseline[2]
        ];

        // Act
        RegistrationFailedException error = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(anyBaselineLandmarks, followUpLandmarks));

        // Assert
        Assert.Contains("24.5 mm", error.Message);
    }

    [Fact]
    public void The_inverse_of_a_registration_maps_back_with_the_same_residual() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10.0, -20.0, 30.0, 1.5);

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal((-10.0, 20.0, -30.0, 1.5), (inverse.DxMm, inverse.DyMm, inverse.DzMm, inverse.ResidualErrorMm));
    }

}
