namespace PulmonaryNodules.Domain;

/// <summary>
/// The voxel mask of a nodule in one CT volume, summarised by its voxel counts and centroid.
/// Boundary voxels are those partially covered by the nodule: they carry the segmentation uncertainty.
/// </summary>
public sealed class Segmentation
{
    public const int MaxVoxelCount = 2_000_000;

    public Segmentation(int voxelCount, int boundaryVoxelCount, VoxelSpacing spacing, SpatialPoint centroid)
    {
        if (voxelCount < 1 || voxelCount > MaxVoxelCount)
        {
            throw new ArgumentOutOfRangeException(nameof(voxelCount), voxelCount, $"A segmentation must hold between 1 and {MaxVoxelCount} voxels.");
        }

        if (boundaryVoxelCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boundaryVoxelCount), boundaryVoxelCount, "A boundary voxel count cannot be negative.");
        }

        if (boundaryVoxelCount > voxelCount)
        {
            throw new ArgumentException($"The boundary ({boundaryVoxelCount} voxels) cannot exceed the segmentation ({voxelCount} voxels).", nameof(boundaryVoxelCount));
        }

        ArgumentNullException.ThrowIfNull(spacing);
        ArgumentNullException.ThrowIfNull(centroid);
        double volume = voxelCount * spacing.VoxelVolumeMm3;
        if (volume > MeasuredVolume.MaxVolumeMm3)
        {
            throw new ArgumentException($"The segmented volume ({volume:0.#} mm³) exceeds the {MeasuredVolume.MaxVolumeMm3} mm³ a nodule can have.", nameof(voxelCount));
        }

        VoxelCount = voxelCount;
        BoundaryVoxelCount = boundaryVoxelCount;
        Spacing = spacing;
        Centroid = centroid;
    }

    public int VoxelCount { get; }

    public int BoundaryVoxelCount { get; }

    public VoxelSpacing Spacing { get; }

    public SpatialPoint Centroid { get; }

    /// <summary>
    /// The measured volume: every voxel counts, and each boundary voxel is assumed half-covered,
    /// so the uncertainty is half the boundary volume.
    /// </summary>
    public MeasuredVolume Measure()
    {
        double volume = VoxelCount * Spacing.VoxelVolumeMm3;
        double uncertainty = 0.5 * BoundaryVoxelCount * Spacing.VoxelVolumeMm3;
        return new MeasuredVolume(volume, uncertainty);
    }
}
