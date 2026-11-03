using JustDummies;

using PulmonaryNodules.Domain;

using Xunit;

namespace PulmonaryNodules.Tests.Regular;

public sealed class TranslationTransformTests {

    [Fact]
    public void Applying_a_transform_shifts_each_coordinate_by_its_own_offset() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10, -20, 5, 1);
        SpatialPoint         point     = new SpatialPoint(100, 100, 100);

        // Act
        SpatialPoint moved = transform.Apply(point);

        // Assert
        Assert.Equal(new SpatialPoint(110, 80, 105), moved);
    }

    [Fact]
    public void The_inverse_negates_the_shifts_and_keeps_the_residual() {
        // Arrange
        double                anyResidual = Any.Double().Between(0, TranslationTransform.MaxResidualErrorMm).Generate();
        TranslationTransform  transform   = new TranslationTransform(10, -20, 5, anyResidual);

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal(-10, inverse.DxMm);
        Assert.Equal(20, inverse.DyMm);
        Assert.Equal(-5, inverse.DzMm);
        Assert.Equal(anyResidual, inverse.ResidualErrorMm);
    }

    [Fact]
    public void Applying_a_transform_then_its_inverse_returns_to_the_starting_point() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10, -20, 5, 1);
        SpatialPoint         point     = new SpatialPoint(100, 100, 100);

        // Act
        SpatialPoint back = transform.Inverse().Apply(transform.Apply(point));

        // Assert
        Assert.Equal(point, back);
    }

    [Fact]
    public void The_identity_moves_nothing() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        SpatialPoint moved = TranslationTransform.Identity.Apply(anyPoint);

        // Assert
        Assert.Equal(anyPoint, moved);
        Assert.Equal(0, TranslationTransform.Identity.ResidualErrorMm);
    }

    [Fact]
    public void Applying_a_transform_to_null_is_refused() {
        // Arrange
        TranslationTransform anyTransform = Any.TranslationTransform().Generate();

        // Act
        Action act = () => anyTransform.Apply(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(300.0, -300.0, 300.0, 15.0)]
    [InlineData(0.0, 0.0, 0.0, 0.0)]
    public void The_limits_of_a_transform_are_accepted(double dx, double dy, double dz, double residual) {
        // Act
        TranslationTransform transform = new TranslationTransform(dx, dy, dz, residual);

        // Assert
        Assert.Equal(residual, transform.ResidualErrorMm);
    }

    [Theory]
    [InlineData(300.001, 0.0, 0.0, 0.0, "dxMm")]
    [InlineData(0.0, -300.001, 0.0, 0.0, "dyMm")]
    [InlineData(0.0, 0.0, 300.001, 0.0, "dzMm")]
    [InlineData(double.NaN, 0.0, 0.0, 0.0, "dxMm")]
    [InlineData(0.0, 0.0, 0.0, -0.001, "residualErrorMm")]
    [InlineData(0.0, 0.0, 0.0, 15.001, "residualErrorMm")]
    [InlineData(0.0, 0.0, 0.0, double.NaN, "residualErrorMm")]
    public void A_shift_or_residual_beyond_its_limit_is_refused(double dx, double dy, double dz, double residual, string parameter) {
        // Act
        Action act = () => new TranslationTransform(dx, dy, dz, residual);

        // Assert
        Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(act).ParamName);
    }

    [Fact]
    public void Estimating_from_landmarks_that_moved_together_gives_that_shift_and_no_residual() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 20, 30),
            Landmark(LandmarkKind.SternalAngle, -50, 5, 12),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 4, -2, 1),
            Landmark(LandmarkKind.AorticArchApex, 14, 18, 31),
            Landmark(LandmarkKind.SternalAngle, -46, 3, 13),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(4, transform.DxMm, 9);
        Assert.Equal(-2, transform.DyMm, 9);
        Assert.Equal(1, transform.DzMm, 9);
        Assert.Equal(0, transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void Estimating_takes_the_mean_displacement_and_the_root_mean_square_of_what_is_left() {
        // Arrange: displacements along x are +2, +4, +6 -> mean 4, leftovers -2, 0, +2 -> rms sqrt(8/3)
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 2, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 14, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 26, 0, 0),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(4, transform.DxMm, 9);
        Assert.Equal(Math.Sqrt(8.0 / 3.0), transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void Landmarks_present_in_only_one_exam_do_not_take_part_in_the_estimate() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 0),
            Landmark(LandmarkKind.T4VertebralBody, 1000, 1000, 1000),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.SternalAngle, 25, 0, 0),
            Landmark(LandmarkKind.LeftDiaphragmDome, -1000, -1000, -1000),
            Landmark(LandmarkKind.CarinaBifurcation, 5, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 15, 0, 0),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(5, transform.DxMm, 9);
        Assert.Equal(0, transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void Fewer_than_three_common_landmarks_cannot_register_two_exams() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyBaseline = AnyLandmarks(LandmarkKind.CarinaBifurcation, LandmarkKind.AorticArchApex, LandmarkKind.SternalAngle);
        IReadOnlyList<AnatomicalLandmark> anyFollowUp = AnyLandmarks(LandmarkKind.CarinaBifurcation, LandmarkKind.AorticArchApex, LandmarkKind.T4VertebralBody);

        // Act
        Action act = () => TranslationTransform.Estimate(anyBaseline, anyFollowUp);

        // Assert
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(act);
        Assert.Contains("found 2", exception.Message);
    }

    [Fact]
    public void Exactly_three_common_landmarks_are_enough() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = AnyLandmarks(LandmarkKind.CarinaBifurcation, LandmarkKind.AorticArchApex, LandmarkKind.SternalAngle);

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(anyLandmarks, anyLandmarks);

        // Assert
        Assert.Equal(0, transform.ResidualErrorMm, 9);
        Assert.Equal(0, transform.DxMm, 9);
    }

    [Fact]
    public void Landmarks_that_moved_inconsistently_leave_too_large_a_residual() {
        // Arrange: three landmarks pushed 40 mm in opposite directions along x -> rms ~ 32.7 mm
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 0, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 0, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 40, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, -40, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 40, 0, 0),
        ];

        // Act
        Action act = () => TranslationTransform.Estimate(baseline, followUp);

        // Assert
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(act);
        Assert.Contains("exceeds the 15", exception.Message);
    }

    [Fact]
    public void A_residual_of_exactly_the_tolerance_is_still_registered() {
        // Arrange: x displacements -15, +15, 0 -> mean 0, rms = sqrt(450/3) = 12.25; use 4 landmarks for a rms of exactly 15
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 0, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 0, 0),
            Landmark(LandmarkKind.T4VertebralBody, 0, 0, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 15, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, -15, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 15, 0, 0),
            Landmark(LandmarkKind.T4VertebralBody, -15, 0, 0),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(15, transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void A_shift_beyond_300_mm_on_an_axis_is_not_a_plausible_registration() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyBaseline = AnyLandmarks(LandmarkKind.CarinaBifurcation, LandmarkKind.AorticArchApex, LandmarkKind.SternalAngle);
        IReadOnlyList<AnatomicalLandmark> farFollowUp = anyBaseline.Select(l => Landmark(l.Kind, l.Position.XMm, l.Position.YMm, l.Position.ZMm - 350)).ToList();

        // Act
        Action act = () => TranslationTransform.Estimate(anyBaseline, farFollowUp);

        // Assert
        Assert.Throws<RegistrationFailedException>(act);
    }

    [Fact]
    public void Estimating_with_a_missing_landmark_list_is_refused() {
        // Arrange
        IReadOnlyList<AnatomicalLandmark> anyLandmarks = AnyLandmarks(LandmarkKind.CarinaBifurcation);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(null!, anyLandmarks));
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(anyLandmarks, null!));
    }

    private static AnatomicalLandmark Landmark(LandmarkKind kind, double x, double y, double z) =>
        new AnatomicalLandmark(kind, new SpatialPoint(x, y, z));

    private static IReadOnlyList<AnatomicalLandmark> AnyLandmarks(params LandmarkKind[] kinds) =>
        kinds.Select(kind => Any.AnatomicalLandmark()
                                .WithKind(kind)
                                .WithPosition(position => position.WithXMm(x => x.Between(-1000, 1000))
                                                                  .WithYMm(y => y.Between(-1000, 1000))
                                                                  .WithZMm(z => z.Between(-1000, 1000)))
                                .Generate())
             .ToList();

}
