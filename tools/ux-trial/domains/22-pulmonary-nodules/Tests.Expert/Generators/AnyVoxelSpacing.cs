// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VoxelSpacing --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="VoxelSpacing" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoxelSpacing : IAny<VoxelSpacing> {

    private IAny<double> _rowMm;
    private IAny<double> _columnMm;
    private IAny<double> _sliceMm;

    private AnyDouble _rowMmRecipe;
    private AnyDouble _columnMmRecipe;
    private AnyDouble _sliceMmRecipe;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVoxelSpacing(AnyDouble rowMm,
                            AnyDouble columnMm,
                            AnyDouble sliceMm) {
        _rowMm          = rowMm;
        _rowMmRecipe    = rowMm;
        _columnMm       = columnMm;
        _columnMmRecipe = columnMm;
        _sliceMm        = sliceMm;
        _sliceMmRecipe  = sliceMm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVoxelSpacing(AnyVoxelSpacing from) {
        _rowMm          = from._rowMm;
        _rowMmRecipe    = from._rowMmRecipe;
        _columnMm       = from._columnMm;
        _columnMmRecipe = from._columnMmRecipe;
        _sliceMm        = from._sliceMm;
        _sliceMmRecipe  = from._sliceMmRecipe;
    }

    /// <summary>Pins <c>rowMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoxelSpacing WithRowMm(double value) {
        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._rowMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rowMm</c> is drawn from — <c>AnyValidRowMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoxelSpacing WithRowMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._rowMmRecipe = configure(_rowMmRecipe);
        copy._rowMm       = copy._rowMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>columnMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoxelSpacing WithColumnMm(double value) {
        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._columnMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>columnMm</c> is drawn from — <c>AnyValidColumnMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoxelSpacing WithColumnMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._columnMmRecipe = configure(_columnMmRecipe);
        copy._columnMm       = copy._columnMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>sliceMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoxelSpacing WithSliceMm(double value) {
        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._sliceMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sliceMm</c> is drawn from — <c>AnyValidSliceMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoxelSpacing WithSliceMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoxelSpacing copy = new AnyVoxelSpacing(this);

        copy._sliceMmRecipe = configure(_sliceMmRecipe);
        copy._sliceMm       = copy._sliceMmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VoxelSpacing" />.</summary>
    public VoxelSpacing Generate() {
        return new VoxelSpacing(_rowMm.Generate(),
                                _columnMm.Generate(),
                                _sliceMm.Generate());
    }

}
