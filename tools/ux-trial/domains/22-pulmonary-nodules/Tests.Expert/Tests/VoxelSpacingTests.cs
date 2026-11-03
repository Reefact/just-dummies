using JustDummies;
using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Generators;
using Xunit;

namespace PulmonaryNodules.Tests.Expert.Tests;

public sealed class VoxelSpacingTests {

    [Fact]
    public void A_voxel_holds_the_product_of_its_three_edges() {
        // Arrange
        VoxelSpacing spacing = new VoxelSpacing(0.5, 0.8, 2.0);

        // Act
        double volume = spacing.VoxelVolumeMm3;

        // Assert
        Assert.Equal(0.8, volume, precision: 12);
    }

    [Theory]
    [InlineData(0.1, true)]
    [InlineData(1.0, true)]
    [InlineData(1.25, true)]
    [InlineData(1.2501, false)]
    [InlineData(2.5, false)]
    [InlineData(10.0, false)]
    public void Volumetry_needs_slices_no_thicker_than_1_25_mm(double sliceMm, bool expectedSuitable) {
        // Arrange
        // The in-plane resolution is left to the draw: the rule looks at the slice spacing alone.
        VoxelSpacing spacing = Any.VoxelSpacing().WithSliceMm(sliceMm).Generate();

        // Act
        bool suitable = spacing.IsSuitableForVolumetry;

        // Assert
        Assert.Equal(expectedSuitable, suitable);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(2.0)]
    public void An_in_plane_spacing_on_either_edge_of_its_range_is_accepted(double edgeMm) {
        // Act
        VoxelSpacing spacing = new VoxelSpacing(edgeMm, edgeMm, 1.0);

        // Assert
        Assert.Equal(edgeMm, spacing.RowMm);
        Assert.Equal(edgeMm, spacing.ColumnMm);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(10.0)]
    public void A_slice_spacing_on_either_edge_of_its_range_is_accepted(double edgeMm) {
        // Act
        VoxelSpacing spacing = new VoxelSpacing(1.0, 1.0, edgeMm);

        // Assert
        Assert.Equal(edgeMm, spacing.SliceMm);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(2.0001)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    public void An_in_plane_spacing_outside_0_1_to_2_mm_is_refused_on_the_row_and_on_the_column(double outsideMm) {
        // Arrange
        VoxelSpacing anyValid = Any.VoxelSpacing().Generate();

        // Act
        ArgumentOutOfRangeException rowRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(outsideMm, anyValid.ColumnMm, anyValid.SliceMm));
        ArgumentOutOfRangeException columnRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(anyValid.RowMm, outsideMm, anyValid.SliceMm));

        // Assert
        Assert.Equal("rowMm", rowRefusal.ParamName);
        Assert.Equal("columnMm", columnRefusal.ParamName);
    }

    [Theory]
    [InlineData(0.0999)]
    [InlineData(10.0001)]
    [InlineData(0.0)]
    [InlineData(double.NaN)]
    public void A_slice_spacing_outside_0_1_to_10_mm_is_refused(double outsideMm) {
        // Arrange
        VoxelSpacing anyValid = Any.VoxelSpacing().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(anyValid.RowMm, anyValid.ColumnMm, outsideMm));

        // Assert
        Assert.Equal("sliceMm", refusal.ParamName);
    }

    [Fact]
    public void A_slice_spacing_wider_than_the_in_plane_limit_is_fine_for_the_slice_axis_only() {
        // Arrange
        double justAboveInPlaneLimit = 2.5;
        VoxelSpacing anyValid = Any.VoxelSpacing().Generate();

        // Act
        VoxelSpacing coarseSlices = new VoxelSpacing(anyValid.RowMm, anyValid.ColumnMm, justAboveInPlaneLimit);
        ArgumentOutOfRangeException inPlaneRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => new VoxelSpacing(justAboveInPlaneLimit, anyValid.ColumnMm, anyValid.SliceMm));

        // Assert
        Assert.Equal(justAboveInPlaneLimit, coarseSlices.SliceMm);
        Assert.Equal("rowMm", inPlaneRefusal.ParamName);
    }

    [Fact]
    public void Spacings_with_the_same_three_edges_are_equal_and_hash_alike() {
        // Arrange
        VoxelSpacing anySpacing = Any.VoxelSpacing().Generate();
        VoxelSpacing sameEdges = new VoxelSpacing(anySpacing.RowMm, anySpacing.ColumnMm, anySpacing.SliceMm);

        // Act
        bool equal = anySpacing.Equals(sameEdges);
        bool sameHash = anySpacing.GetHashCode() == sameEdges.GetHashCode();

        // Assert
        Assert.True(equal);
        Assert.True(sameHash);
    }

    [Theory]
    [InlineData(0.6, 0.5, 0.5)]
    [InlineData(0.5, 0.6, 0.5)]
    [InlineData(0.5, 0.5, 0.6)]
    public void Spacings_differing_on_one_edge_are_not_equal(double rowMm, double columnMm, double sliceMm) {
        // Arrange
        VoxelSpacing reference = new VoxelSpacing(0.5, 0.5, 0.5);
        VoxelSpacing other = new VoxelSpacing(rowMm, columnMm, sliceMm);

        // Act
        bool equal = reference.Equals(other);

        // Assert
        Assert.False(equal);
    }

}
