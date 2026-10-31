namespace PulmonaryNodules.Domain;

/// <summary>The physical size of one voxel of a CT volume, in millimetres along each axis.</summary>
public sealed class VoxelSpacing : IEquatable<VoxelSpacing>
{
    public const double MinSpacingMm = 0.1;
    public const double MaxInPlaneSpacingMm = 2.0;
    public const double MaxSliceSpacingMm = 10.0;

    public VoxelSpacing(double rowMm, double columnMm, double sliceMm)
    {
        RowMm = Guard(rowMm, MaxInPlaneSpacingMm, nameof(rowMm));
        ColumnMm = Guard(columnMm, MaxInPlaneSpacingMm, nameof(columnMm));
        SliceMm = Guard(sliceMm, MaxSliceSpacingMm, nameof(sliceMm));
    }

    public double RowMm { get; }

    public double ColumnMm { get; }

    public double SliceMm { get; }

    /// <summary>The volume of a single voxel, in cubic millimetres.</summary>
    public double VoxelVolumeMm3 => RowMm * ColumnMm * SliceMm;

    /// <summary>True when the slice spacing is no coarser than the in-plane resolution allows for volumetry (≤ 1.25 mm).</summary>
    public bool IsSuitableForVolumetry => SliceMm <= 1.25;

    public bool Equals(VoxelSpacing? other) =>
        other is not null && RowMm.Equals(other.RowMm) && ColumnMm.Equals(other.ColumnMm) && SliceMm.Equals(other.SliceMm);

    public override bool Equals(object? obj) => Equals(obj as VoxelSpacing);

    public override int GetHashCode() => HashCode.Combine(RowMm, ColumnMm, SliceMm);

    public override string ToString() => $"{RowMm:0.###} x {ColumnMm:0.###} x {SliceMm:0.###} mm";

    private static double Guard(double value, double max, string name)
    {
        if (double.IsNaN(value) || value < MinSpacingMm || value > max)
        {
            throw new ArgumentOutOfRangeException(name, value, $"A voxel spacing must lie within [{MinSpacingMm}, {max}] mm.");
        }

        return value;
    }
}
