using JustDummies;

using PulmonaryNodules.Domain;
using PulmonaryNodules.Tests.Expert.Arbitrary;

using Xunit;

namespace PulmonaryNodules.Tests.Expert;

public sealed class MeasurementTests {

    [Fact]
    public void A_measured_volume_spans_its_uncertainty_on_both_sides() {
        // Arrange
        MeasuredVolume volume = new MeasuredVolume(500.0, 20.0);

        // Act
        (double lower, double upper) = (volume.LowerBoundMm3, volume.UpperBoundMm3);

        // Assert
        Assert.Equal(480.0, lower);
        Assert.Equal(520.0, upper);
    }

    [Fact]
    public void The_equivalent_diameter_is_the_diameter_of_the_sphere_of_the_same_volume() {
        // Arrange — a 10 mm sphere holds 4/3·π·5³ mm³.
        MeasuredVolume anyTenMillimetreNodule = Any.MeasuredVolume().WithVolumeMm3(4.0 / 3.0 * Math.PI * 125.0).Generate();

        // Act
        double diameter = anyTenMillimetreNodule.EquivalentDiameterMm;

        // Assert
        Assert.Equal(10.0, diameter, precision: 9);
    }

    [Fact]
    public void A_measurement_as_uncertain_as_the_volume_itself_is_refused() {
        // Arrange / Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new MeasuredVolume(150.0, 150.0));

        // Assert
        Assert.Equal("uncertaintyMm3", error.ParamName);
    }

    [Fact]
    public void A_segmentation_counts_every_voxel_and_half_of_each_boundary_voxel_as_uncertain() {
        // Arrange — 0.5 × 0.5 × 1 mm voxels hold 0.25 mm³ each.
        Segmentation anySegmentation = Any.Segmentation()
                                          .WithSpacing(new VoxelSpacing(0.5, 0.5, 1.0))
                                          .WithVoxelCount(1_000)
                                          .WithBoundaryVoxelCount(200)
                                          .Generate();

        // Act
        MeasuredVolume volume = anySegmentation.Measure();

        // Assert
        Assert.Equal(250.0, volume.VolumeMm3, precision: 9);
        Assert.Equal(25.0, volume.UncertaintyMm3, precision: 9);
    }

    [Fact]
    public void A_segmentation_larger_than_a_nodule_can_be_is_refused_as_a_mass() {
        // Arrange — 20 001 voxels of 1 mm³ exceed the 20 000 mm³ a nodule can have.
        AnySegmentation anyOversizedSegmentation = Any.Segmentation()
                                                      .WithSpacing(new VoxelSpacing(1.0, 1.0, 1.0))
                                                      .WithVoxelCount(20_001);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => anyOversizedSegmentation.Generate());

        // Assert
        Assert.Equal("voxelCount", error.ParamName);
    }

    [Theory]
    [InlineData(0.625, true)]
    [InlineData(1.25, true)]
    [InlineData(2.5, false)]
    [InlineData(5.0, false)]
    public void Only_thin_slices_are_suitable_for_volumetry(double sliceMm, bool suitable) {
        // Arrange
        VoxelSpacing anySpacing = Any.VoxelSpacing().WithSliceMm(sliceMm).Generate();

        // Act
        bool isSuitable = anySpacing.IsSuitableForVolumetry;

        // Assert
        Assert.Equal(suitable, isSuitable);
    }

}
