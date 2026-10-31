// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Segmentation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="Segmentation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySegmentation : IAny<Segmentation> {

    private readonly Func<VoxelSpacing, IAny<int>> _voxelCount;
    private readonly Func<int, IAny<int>>          _boundaryVoxelCount;
    private readonly IAny<VoxelSpacing>            _spacing;
    private readonly IAny<SpatialPoint>            _centroid;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySegmentation()
        : this(voxelCount:         AnyValidVoxelCount,
               boundaryVoxelCount: AnyValidBoundaryVoxelCount,
               spacing:            AnyValidSpacing(),
               centroid:           AnyValidCentroid()) { }

    // Three guards tie the parameters: the voxels must fit a nodule's volume at the drawn spacing,
    // and the boundary is part of the segmentation. Each dependent base is a function of what it
    // depends on, so a refinement starts from it and keeps the tie.
    private static AnyInt32 AnyValidVoxelCount(VoxelSpacing spacing) {
        int fittingANodule = (int)Math.Min(Segmentation.MaxVoxelCount, Math.Floor(MeasuredVolume.MaxVolumeMm3 / spacing.VoxelVolumeMm3));

        return Any.Int32().Between(1, fittingANodule);
    }

    private static AnyInt32 AnyValidBoundaryVoxelCount(int voxelCount) {
        return Any.Int32().Between(0, voxelCount);
    }

    private static AnyVoxelSpacing AnyValidSpacing() {
        return new AnyVoxelSpacing();
    }

    private static AnySpatialPoint AnyValidCentroid() {
        return new AnySpatialPoint();
    }

    private AnySegmentation(Func<VoxelSpacing, IAny<int>> voxelCount,
                            Func<int, IAny<int>>          boundaryVoxelCount,
                            IAny<VoxelSpacing>            spacing,
                            IAny<SpatialPoint>            centroid) {
        _voxelCount         = voxelCount;
        _boundaryVoxelCount = boundaryVoxelCount;
        _spacing            = spacing;
        _centroid           = centroid;
    }

    /// <summary>Pins <c>voxelCount</c> to a fixed value.</summary>
    public AnySegmentation WithVoxelCount(int value) {
        return new AnySegmentation(_ => new FixedValue<int>(value), _boundaryVoxelCount, _spacing, _centroid);
    }

    /// <summary>Draws <c>voxelCount</c> from <c>AnyValidVoxelCount(spacing)</c>, refined by <paramref name="configure" />.</summary>
    public AnySegmentation WithVoxelCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnySegmentation(spacing => configure(AnyValidVoxelCount(spacing)), _boundaryVoxelCount, _spacing, _centroid);
    }

    /// <summary>Pins <c>boundaryVoxelCount</c> to a fixed value.</summary>
    public AnySegmentation WithBoundaryVoxelCount(int value) {
        return new AnySegmentation(_voxelCount, _ => new FixedValue<int>(value), _spacing, _centroid);
    }

    /// <summary>Draws <c>boundaryVoxelCount</c> from <c>AnyValidBoundaryVoxelCount(voxelCount)</c>, refined by <paramref name="configure" />.</summary>
    public AnySegmentation WithBoundaryVoxelCount(Func<AnyInt32, AnyInt32> configure) {
        return new AnySegmentation(_voxelCount, voxelCount => configure(AnyValidBoundaryVoxelCount(voxelCount)), _spacing, _centroid);
    }

    /// <summary>Pins <c>spacing</c> to a fixed value.</summary>
    public AnySegmentation WithSpacing(VoxelSpacing value) {
        return new AnySegmentation(_voxelCount, _boundaryVoxelCount, new FixedValue<VoxelSpacing>(value), _centroid);
    }

    /// <summary>Draws <c>spacing</c> from <c>AnyValidSpacing()</c>, refined by <paramref name="configure" />.</summary>
    public AnySegmentation WithSpacing(Func<AnyVoxelSpacing, AnyVoxelSpacing> configure) {
        return new AnySegmentation(_voxelCount, _boundaryVoxelCount, configure(AnyValidSpacing()), _centroid);
    }

    /// <summary>Pins <c>centroid</c> to a fixed value.</summary>
    public AnySegmentation WithCentroid(SpatialPoint value) {
        return new AnySegmentation(_voxelCount, _boundaryVoxelCount, _spacing, new FixedValue<SpatialPoint>(value));
    }

    /// <summary>Draws <c>centroid</c> from <c>AnyValidCentroid()</c>, refined by <paramref name="configure" />.</summary>
    public AnySegmentation WithCentroid(Func<AnySpatialPoint, AnySpatialPoint> configure) {
        return new AnySegmentation(_voxelCount, _boundaryVoxelCount, _spacing, configure(AnyValidCentroid()));
    }

    /// <summary>Produces one arbitrary <see cref="Segmentation" />.</summary>
    public Segmentation Generate() {
        VoxelSpacing spacing    = _spacing.Generate();
        int          voxelCount = _voxelCount(spacing).Generate();

        return new Segmentation(voxelCount,
                                _boundaryVoxelCount(voxelCount).Generate(),
                                spacing,
                                _centroid.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
