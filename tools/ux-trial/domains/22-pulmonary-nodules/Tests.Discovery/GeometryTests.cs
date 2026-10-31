using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Discovery;

public sealed class GeometryTests {

    private const double Precision = 1e-9;

    // A point well inside the field of view, so that a plausible patient shift keeps it there.
    private static AnySpatialPoint AnyThoracicPoint() {
        return Any.SpatialPoint()
                  .WithXMm(x => x.Between(-1000.0, 1000.0))
                  .WithYMm(y => y.Between(-1000.0, 1000.0))
                  .WithZMm(z => z.Between(-1000.0, 1000.0));
    }

    [Fact]
    public void A_translation_moves_a_point_by_its_shift_on_each_axis() {
        // Arrange
        TranslationTransform transform = Any.TranslationTransform().WithDxMm(5.0).WithDyMm(-3.0).WithDzMm(12.5).Generate();
        SpatialPoint         point     = new SpatialPoint(10.0, 20.0, 30.0);

        // Act
        SpatialPoint moved = transform.Apply(point);

        // Assert
        Assert.Equal(new SpatialPoint(15.0, 17.0, 42.5), moved);
    }

    [Fact]
    public void The_inverse_translation_undoes_the_shift_and_keeps_the_residual() {
        // Arrange
        TranslationTransform transform = Any.TranslationTransform().WithDxMm(5.0).WithDyMm(-3.0).WithDzMm(12.5).WithResidualErrorMm(1.5).Generate();

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal(-5.0, inverse.DxMm);
        Assert.Equal(3.0, inverse.DyMm);
        Assert.Equal(-12.5, inverse.DzMm);
        Assert.Equal(1.5, inverse.ResidualErrorMm);
    }

    [Fact]
    public void Registration_recovers_a_rigid_patient_shift_with_no_residual() {
        // Arrange
        SpatialPoint anyCarina = AnyThoracicPoint().Generate();
        SpatialPoint anyAorta  = AnyThoracicPoint().Generate();
        SpatialPoint anyT4     = AnyThoracicPoint().Generate();
        AnatomicalLandmark[] baseline = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, anyCarina),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, anyAorta),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, anyT4),
        ];
        AnatomicalLandmark[] followUp = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, Shift(anyCarina, 4.0, -2.0, 15.0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, Shift(anyAorta, 4.0, -2.0, 15.0)),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, Shift(anyT4, 4.0, -2.0, 15.0)),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(4.0, transform.DxMm, Precision);
        Assert.Equal(-2.0, transform.DyMm, Precision);
        Assert.Equal(15.0, transform.DzMm, Precision);
        Assert.Equal(0.0, transform.ResidualErrorMm, Precision);
    }

    [Fact]
    public void Registration_uses_only_the_landmarks_both_exams_share() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(0.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(10.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, new SpatialPoint(0.0, 10.0, 0.0)),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate(),
        ];
        AnatomicalLandmark[] followUp = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(1.0, 1.0, 1.0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(11.0, 1.0, 1.0)),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, new SpatialPoint(1.0, 11.0, 1.0)),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.RightDiaphragmDome).Generate(),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(1.0, transform.DxMm, Precision);
        Assert.Equal(1.0, transform.DyMm, Precision);
        Assert.Equal(1.0, transform.DzMm, Precision);
    }

    [Fact]
    public void Registration_fails_with_fewer_than_three_shared_landmarks() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.AorticArchApex).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.SternalAngle).Generate(),
        ];
        AnatomicalLandmark[] followUp = [
            Any.AnatomicalLandmark().WithKind(LandmarkKind.CarinaBifurcation).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.AorticArchApex).Generate(),
            Any.AnatomicalLandmark().WithKind(LandmarkKind.T4VertebralBody).Generate(),
        ];

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("found 2", failure.Message);
    }

    [Fact]
    public void Registration_fails_when_the_landmarks_disagree_beyond_the_residual_tolerance() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(0.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(0.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, new SpatialPoint(0.0, 0.0, 0.0)),
        ];
        AnatomicalLandmark[] followUp = [
            new AnatomicalLandmark(LandmarkKind.CarinaBifurcation, new SpatialPoint(-30.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.AorticArchApex, new SpatialPoint(0.0, 0.0, 0.0)),
            new AnatomicalLandmark(LandmarkKind.T4VertebralBody, new SpatialPoint(30.0, 0.0, 0.0)),
        ];

        // Act
        RegistrationFailedException failure = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("residual", failure.Message);
    }

    [Fact]
    public void A_voxel_volume_is_the_product_of_its_three_spacings() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.5, 0.8, 1.25);

        // Act
        double volume = spacing.VoxelVolumeMm3;

        // Assert
        Assert.Equal(0.5, volume, Precision);
    }

    [Fact]
    public void A_slice_spacing_of_1_25_mm_is_suitable_for_volumetry() {
        // Arrange
        VoxelSpacing anyThinSliceSpacing = Any.VoxelSpacing().WithSliceMm(1.25).Generate();

        // Act
        bool suitable = anyThinSliceSpacing.IsSuitableForVolumetry;

        // Assert
        Assert.True(suitable);
    }

    [Fact]
    public void A_slice_spacing_of_2_5_mm_is_too_coarse_for_volumetry() {
        // Arrange
        VoxelSpacing anyThickSliceSpacing = Any.VoxelSpacing().WithSliceMm(2.5).Generate();

        // Act
        bool suitable = anyThickSliceSpacing.IsSuitableForVolumetry;

        // Assert
        Assert.False(suitable);
    }

    private static SpatialPoint Shift(SpatialPoint point, double dx, double dy, double dz) {
        return new SpatialPoint(point.XMm + dx, point.YMm + dy, point.ZMm + dz);
    }

}
