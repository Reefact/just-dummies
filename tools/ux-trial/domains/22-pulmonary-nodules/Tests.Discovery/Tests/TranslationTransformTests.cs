namespace PulmonaryNodules.Tests.Discovery;

public sealed class TranslationTransformTests {

    [Fact]
    public void A_transform_keeps_its_shifts_and_its_residual() {
        // Act
        TranslationTransform transform = new TranslationTransform(1.5, -2.5, 3.5, 0.75);

        // Assert
        Assert.Equal(1.5, transform.DxMm);
        Assert.Equal(-2.5, transform.DyMm);
        Assert.Equal(3.5, transform.DzMm);
        Assert.Equal(0.75, transform.ResidualErrorMm);
    }

    [Fact]
    public void The_identity_moves_nothing_and_leaves_no_residual() {
        // Arrange
        SpatialPoint anyPoint = Any.SpatialPoint().Generate();

        // Act
        SpatialPoint moved = TranslationTransform.Identity.Apply(anyPoint);

        // Assert
        Assert.Equal(anyPoint, moved);
        Assert.Equal(0.0, TranslationTransform.Identity.ResidualErrorMm);
    }

    [Fact]
    public void Applying_a_transform_shifts_each_coordinate() {
        // Arrange
        double                anyResidualMm = Any.Double().Between(0d, TranslationTransform.MaxResidualErrorMm).Generate();
        TranslationTransform  transform     = new TranslationTransform(10, -20, 30, anyResidualMm);

        // Act
        SpatialPoint moved = transform.Apply(new SpatialPoint(1, 2, 3));

        // Assert
        Assert.Equal(new SpatialPoint(11, -18, 33), moved);
    }

    [Fact]
    public void Applying_a_transform_to_nothing_is_refused() {
        // Arrange
        TranslationTransform anyTransform = Any.TranslationTransform().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => anyTransform.Apply(null!));
    }

    [Fact]
    public void The_inverse_negates_the_shifts_and_keeps_the_residual() {
        // Arrange
        TranslationTransform transform = new TranslationTransform(10, -20, 30, 2.5);

        // Act
        TranslationTransform inverse = transform.Inverse();

        // Assert
        Assert.Equal(-10, inverse.DxMm);
        Assert.Equal(20, inverse.DyMm);
        Assert.Equal(-30, inverse.DzMm);
        Assert.Equal(2.5, inverse.ResidualErrorMm);
    }

    [Fact]
    public void The_inverse_brings_a_shifted_point_back() {
        // Arrange
        TranslationTransform anyTransform = Any.TranslationTransform().WithDxMm(10).WithDyMm(-20).WithDzMm(30).Generate();
        SpatialPoint         origin       = new SpatialPoint(100, 200, 300);

        // Act
        SpatialPoint back = anyTransform.Inverse().Apply(anyTransform.Apply(origin));

        // Assert
        Assert.Equal(100, back.XMm, 9);
        Assert.Equal(200, back.YMm, 9);
        Assert.Equal(300, back.ZMm, 9);
    }

    [Theory]
    [InlineData(300.0)]
    [InlineData(-300.0)]
    public void A_shift_of_three_hundred_mm_is_the_largest_accepted(double shiftMm) {
        // Arrange
        double anyOtherMm = Any.Double().Between(-300d, 300d).Generate();

        // Act
        Exception? onX = Record.Exception(() => new TranslationTransform(shiftMm, anyOtherMm, anyOtherMm, 0));
        Exception? onY = Record.Exception(() => new TranslationTransform(anyOtherMm, shiftMm, anyOtherMm, 0));
        Exception? onZ = Record.Exception(() => new TranslationTransform(anyOtherMm, anyOtherMm, shiftMm, 0));

        // Assert
        Assert.Null(onX);
        Assert.Null(onY);
        Assert.Null(onZ);
    }

    [Theory]
    [InlineData(300.0001)]
    [InlineData(-300.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_shift_beyond_three_hundred_mm_is_refused_on_each_axis(double badMm) {
        // Arrange
        double anyOtherMm = Any.Double().Between(-300d, 300d).Generate();

        // Act
        ArgumentOutOfRangeException onX = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(badMm, anyOtherMm, anyOtherMm, 0));
        ArgumentOutOfRangeException onY = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(anyOtherMm, badMm, anyOtherMm, 0));
        ArgumentOutOfRangeException onZ = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(anyOtherMm, anyOtherMm, badMm, 0));

        // Assert
        Assert.Equal("dxMm", onX.ParamName);
        Assert.Equal("dyMm", onY.ParamName);
        Assert.Equal("dzMm", onZ.ParamName);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(15.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_residual_outside_zero_to_fifteen_mm_is_refused(double badMm) {
        // Arrange
        double anyShiftMm = Any.Double().Between(-300d, 300d).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new TranslationTransform(anyShiftMm, anyShiftMm, anyShiftMm, badMm));

        // Assert
        Assert.Equal("residualErrorMm", exception.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(15.0)]
    public void The_limits_of_the_residual_are_accepted(double residualMm) {
        // Arrange
        double anyShiftMm = Any.Double().Between(-300d, 300d).Generate();

        // Act
        Exception? exception = Record.Exception(() => new TranslationTransform(anyShiftMm, anyShiftMm, anyShiftMm, residualMm));

        // Assert
        Assert.Null(exception);
    }

    // ---- Estimate ----

    private static AnatomicalLandmark Landmark(LandmarkKind kind, double x, double y, double z) {
        return new AnatomicalLandmark(kind, new SpatialPoint(x, y, z));
    }

    [Fact]
    public void Three_landmarks_moved_by_the_same_shift_give_that_shift_and_no_residual() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 50, 20, 10),
            Landmark(LandmarkKind.SternalAngle, -30, 60, -20),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 5, -10, 15),
            Landmark(LandmarkKind.AorticArchApex, 55, 10, 25),
            Landmark(LandmarkKind.SternalAngle, -25, 50, -5),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(5, transform.DxMm, 9);
        Assert.Equal(-10, transform.DyMm, 9);
        Assert.Equal(15, transform.DzMm, 9);
        Assert.Equal(0.0, transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void The_shift_is_the_mean_displacement_and_the_residual_the_rms_distance_left() {
        // Arrange: along x the displacements are +3, -3 and 0; their mean is 0 and each leaves 3, 3 and 0 mm.
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 0, 10, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 20, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 3, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, -3, 10, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 20, 0),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(0.0, transform.DxMm, 9);
        Assert.Equal(Math.Sqrt(6.0), transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void Only_the_landmarks_both_exams_share_take_part_in_the_estimate() {
        // Arrange: the carina and the aortic arch are shared; the sternum and the vertebra are located in one exam only.
        // Two of them are not enough, and the unshared ones must not make up the difference.
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 1, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 11, 0, 0),
            Landmark(LandmarkKind.T4VertebralBody, 21, 0, 0),
        ];

        // Act
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("found 2", exception.Message);
    }

    [Fact]
    public void Extra_landmarks_in_only_one_exam_are_ignored_when_three_are_shared() {
        // Arrange
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 0),
            Landmark(LandmarkKind.LeftDiaphragmDome, 1000, 1000, 1000),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 1, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 11, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 21, 0, 0),
            Landmark(LandmarkKind.RightDiaphragmDome, -1000, -1000, -1000),
        ];

        // Act
        TranslationTransform transform = TranslationTransform.Estimate(baseline, followUp);

        // Assert
        Assert.Equal(1.0, transform.DxMm, 9);
        Assert.Equal(0.0, transform.ResidualErrorMm, 9);
    }

    [Fact]
    public void Fewer_than_three_common_landmarks_cannot_register_two_exams() {
        // Arrange
        AnatomicalLandmark[] twoLandmarks = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
        ];

        // Act
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(twoLandmarks, twoLandmarks));

        // Assert
        Assert.Contains("at least 3", exception.Message);
    }

    [Fact]
    public void No_landmark_at_all_cannot_register_two_exams() {
        // Act / Assert
        Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate([], []));
    }

    [Fact]
    public void A_residual_over_fifteen_mm_makes_the_registration_fail() {
        // Arrange: displacements of +20, -20 and 0 mm along x leave an rms residual of 20 * sqrt(2/3) = 16.3 mm.
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 0, 10, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 20, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 20, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, -20, 10, 0),
            Landmark(LandmarkKind.SternalAngle, 0, 20, 0),
        ];

        // Act
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("residual", exception.Message);
    }

    [Fact]
    public void A_registration_failure_is_an_invalid_operation() {
        // Act
        RegistrationFailedException exception = new RegistrationFailedException("failed");

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }

    [Fact]
    public void A_shift_beyond_three_hundred_mm_makes_the_registration_fail() {
        // Arrange: the whole patient moved 400 mm along z.
        AnatomicalLandmark[] baseline = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 0),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 0),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 0),
        ];
        AnatomicalLandmark[] followUp = [
            Landmark(LandmarkKind.CarinaBifurcation, 0, 0, 400),
            Landmark(LandmarkKind.AorticArchApex, 10, 0, 400),
            Landmark(LandmarkKind.SternalAngle, 20, 0, 400),
        ];

        // Act
        RegistrationFailedException exception = Assert.Throws<RegistrationFailedException>(() => TranslationTransform.Estimate(baseline, followUp));

        // Assert
        Assert.Contains("300", exception.Message);
    }

    [Fact]
    public void Estimating_from_nothing_is_refused() {
        // Arrange
        AnatomicalLandmark[] anyLandmarks = Any.ArrayOf(new AnyAnatomicalLandmark()).WithCount(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(null!, anyLandmarks));
        Assert.Throws<ArgumentNullException>(() => TranslationTransform.Estimate(anyLandmarks, null!));
    }

}
