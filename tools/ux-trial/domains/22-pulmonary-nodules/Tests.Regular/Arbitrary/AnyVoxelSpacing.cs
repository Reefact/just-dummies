// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoxelSpacing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VoxelSpacing" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoxelSpacing : IAny<VoxelSpacing> {

    private readonly IAny<double> _rowMm;
    private readonly IAny<double> _columnMm;
    private readonly IAny<double> _sliceMm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoxelSpacing()
        : this(rowMm:    AnyValidRowMm(),
               columnMm: AnyValidColumnMm(),
               sliceMm:  AnyValidSliceMm()) { }

    private static AnyDouble AnyValidRowMm() {
        return Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm);
    }

    private static AnyDouble AnyValidColumnMm() {
        return Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxInPlaneSpacingMm);
    }

    private static AnyDouble AnyValidSliceMm() {
        return Any.Double().Between(VoxelSpacing.MinSpacingMm, VoxelSpacing.MaxSliceSpacingMm);
    }

    private AnyVoxelSpacing(IAny<double> rowMm,
                            IAny<double> columnMm,
                            IAny<double> sliceMm) {
        _rowMm    = rowMm;
        _columnMm = columnMm;
        _sliceMm  = sliceMm;
    }

    /// <summary>Pins <c>rowMm</c> to a fixed value.</summary>
    public AnyVoxelSpacing WithRowMm(double value) {
        return new AnyVoxelSpacing(new FixedValue<double>(value), _columnMm, _sliceMm);
    }

    /// <summary>Draws <c>rowMm</c> from <c>AnyValidRowMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoxelSpacing WithRowMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVoxelSpacing(configure(AnyValidRowMm()), _columnMm, _sliceMm);
    }

    /// <summary>Pins <c>columnMm</c> to a fixed value.</summary>
    public AnyVoxelSpacing WithColumnMm(double value) {
        return new AnyVoxelSpacing(_rowMm, new FixedValue<double>(value), _sliceMm);
    }

    /// <summary>Draws <c>columnMm</c> from <c>AnyValidColumnMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoxelSpacing WithColumnMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVoxelSpacing(_rowMm, configure(AnyValidColumnMm()), _sliceMm);
    }

    /// <summary>Pins <c>sliceMm</c> to a fixed value.</summary>
    public AnyVoxelSpacing WithSliceMm(double value) {
        return new AnyVoxelSpacing(_rowMm, _columnMm, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>sliceMm</c> from <c>AnyValidSliceMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVoxelSpacing WithSliceMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyVoxelSpacing(_rowMm, _columnMm, configure(AnyValidSliceMm()));
    }

    /// <summary>Produces one arbitrary <see cref="VoxelSpacing" />.</summary>
    public VoxelSpacing Generate() {
        return new VoxelSpacing(_rowMm.Generate(),
                                _columnMm.Generate(),
                                _sliceMm.Generate());
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
