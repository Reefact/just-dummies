namespace PulmonaryNodules.Tests.Discovery;

public sealed class VoxelSpacingTests {

    [Fact]
    public void A_voxel_spacing_keeps_its_three_axes() {
        // Act
        VoxelSpacing spacing = new VoxelSpacing(0.7, 0.8, 1.0);

        // Assert
        Assert.Equal(0.7, spacing.RowMm);
        Assert.Equal(0.8, spacing.ColumnMm);
        Assert.Equal(1.0, spacing.SliceMm);
    }

    [Fact]
    public void The_voxel_volume_is_the_product_of_its_three_spacings() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.5, 0.5, 2.0);

        // Act
        double volume = spacing.VoxelVolumeMm3;

        // Assert
        Assert.Equal(0.5, volume, 12);
    }

    [Theory]
    [InlineData(0.1, 0.1, 0.1)]
    [InlineData(2.0, 2.0, 10.0)]
    public void The_limits_of_each_axis_are_accepted(double rowMm, double columnMm, double sliceMm) {
        // Act
        Exception? exception = Record.Exception(() => new VoxelSpacing(rowMm, columnMm, sliceMm));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(2.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1.0)]
    public void An_in_plane_spacing_outside_its_range_is_refused_on_both_axes(double badMm) {
        // Arrange
        double anySliceMm = Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxSliceSpacingMm).Generate();
        double anyValidMm = Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm).Generate();

        // Act
        ArgumentOutOfRangeException onRow    = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(badMm, anyValidMm, anySliceMm));
        ArgumentOutOfRangeException onColumn = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(anyValidMm, badMm, anySliceMm));

        // Assert
        Assert.Equal("rowMm", onRow.ParamName);
        Assert.Equal("columnMm", onColumn.ParamName);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(10.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_slice_spacing_outside_its_range_is_refused(double badMm) {
        // Arrange
        double anyInPlaneMm = Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(anyInPlaneMm, anyInPlaneMm, badMm));

        // Assert
        Assert.Equal("sliceMm", exception.ParamName);
    }

    [Fact]
    public void The_slice_axis_has_a_wider_range_than_the_in_plane_axes() {
        // Act
        Exception? exception = Record.Exception(() => new VoxelSpacing(2.0, 2.0, 5.0));

        // Assert
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0.1, true)]
    [InlineData(1.0, true)]
    [InlineData(1.25, true)]
    [InlineData(1.2501, false)]
    [InlineData(5.0, false)]
    public void A_volume_is_suitable_for_volumetry_up_to_a_slice_spacing_of_one_and_a_quarter_mm(double sliceMm, bool expected) {
        // Arrange
        double anyInPlaneMm = Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm).Generate();
        VoxelSpacing spacing = new VoxelSpacing(anyInPlaneMm, anyInPlaneMm, sliceMm);

        // Act
        bool suitable = spacing.IsSuitableForVolumetry;

        // Assert
        Assert.Equal(expected, suitable);
    }

    [Fact]
    public void Two_spacings_with_the_same_axes_are_equal() {
        // Arrange
        VoxelSpacing first  = new VoxelSpacing(0.7, 0.7, 1.0);
        VoxelSpacing second = new VoxelSpacing(0.7, 0.7, 1.0);
        VoxelSpacing other  = new VoxelSpacing(0.7, 0.7, 1.5);

        // Act / Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void A_spacing_is_never_equal_to_nothing() {
        // Arrange
        VoxelSpacing anySpacing = Any.VoxelSpacing().Generate();

        // Act / Assert
        Assert.False(anySpacing.Equals(null));
    }

}
