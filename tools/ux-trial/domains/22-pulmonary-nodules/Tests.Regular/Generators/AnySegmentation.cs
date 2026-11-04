// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Segmentation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Segmentation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySegmentation : IAny<Segmentation> {

    private IAny<VoxelSpacing>  _spacing;
    private IAny<SpatialPoint>  _centroid;

    private AnyInt32        _voxelCountRecipe;
    private AnyInt32        _boundaryVoxelCountRecipe;
    private AnyVoxelSpacing _spacingRecipe;
    private AnySpatialPoint _centroidRecipe;

    private int? _voxelCountPin;
    private int? _boundaryVoxelCountPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySegmentation()
        : this(voxelCount:         AnyValidVoxelCount(),
               boundaryVoxelCount: AnyValidBoundaryVoxelCount(),
               spacing:            AnyValidSpacing(),
               centroid:           AnyValidCentroid()) { }

    private static AnyInt32 AnyValidVoxelCount() {
        return Any.Int32().Positive().LessThanOrEqualTo(Segmentation.MaxVoxelCount);
    }

    private static AnyInt32 AnyValidBoundaryVoxelCount() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyVoxelSpacing AnyValidSpacing() {
        return new AnyVoxelSpacing();
    }

    private static AnySpatialPoint AnyValidCentroid() {
        return new AnySpatialPoint();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySegmentation(AnyInt32        voxelCount,
                            AnyInt32        boundaryVoxelCount,
                            AnyVoxelSpacing spacing,
                            AnySpatialPoint centroid) {
        _voxelCountRecipe                = voxelCount;
        _boundaryVoxelCountRecipe        = boundaryVoxelCount;
        _spacing                         = spacing;
        _spacingRecipe                   = spacing;
        _centroid                        = centroid;
        _centroidRecipe                  = centroid;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySegmentation(AnySegmentation from) {
        _voxelCountRecipe                = from._voxelCountRecipe;
        _voxelCountPin                   = from._voxelCountPin;
        _boundaryVoxelCountRecipe        = from._boundaryVoxelCountRecipe;
        _boundaryVoxelCountPin           = from._boundaryVoxelCountPin;
        _spacing                         = from._spacing;
        _spacingRecipe                   = from._spacingRecipe;
        _centroid                        = from._centroid;
        _centroidRecipe                  = from._centroidRecipe;
    }

    /// <summary>Draws <c>boundaryVoxelCount</c> and <c>voxelCount</c> together, <c>boundaryVoxelCount</c> never after <c>voxelCount</c> as the constructor requires, each from its current recipe and pin.</summary>
    private (int, int) BoundaryVoxelCountAndVoxelCount(int maxVoxelCount = Segmentation.MaxVoxelCount) {
        (int, int) pair = _boundaryVoxelCountPin is { } boundaryVoxelCount ? (boundaryVoxelCount, _voxelCountPin ?? _voxelCountRecipe.LessThanOrEqualTo(maxVoxelCount).GreaterThanOrEqualTo(boundaryVoxelCount).Generate())
                          : _voxelCountPin is { } voxelCount ? (_boundaryVoxelCountRecipe.LessThanOrEqualTo(voxelCount).Generate(), voxelCount)
                          : Any.Pair(_boundaryVoxelCountRecipe, _voxelCountRecipe.LessThanOrEqualTo(maxVoxelCount)).Having.Item1LessThanOrEqualToItem2().Generate();

        return pair;
    }

    /// <summary>Pins <c>voxelCount</c> to a fixed value, which bounds <c>boundaryVoxelCount</c>; its recipe is kept for a later refinement.</summary>
    public AnySegmentation WithVoxelCount(int value) {
        AnySegmentation copy = new AnySegmentation(this);

        copy._voxelCountPin                   = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>voxelCount</c> is drawn from — <c>AnyValidVoxelCount()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>boundaryVoxelCount</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegmentation WithVoxelCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegmentation copy = new AnySegmentation(this);

        copy._voxelCountRecipe                = configure(_voxelCountRecipe);
        copy._voxelCountPin                   = null;

        return copy;
    }

    /// <summary>Pins <c>boundaryVoxelCount</c> to a fixed value, which bounds <c>voxelCount</c>; its recipe is kept for a later refinement.</summary>
    public AnySegmentation WithBoundaryVoxelCount(int value) {
        AnySegmentation copy = new AnySegmentation(this);

        copy._boundaryVoxelCountPin           = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>boundaryVoxelCount</c> is drawn from — <c>AnyValidBoundaryVoxelCount()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>voxelCount</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegmentation WithBoundaryVoxelCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegmentation copy = new AnySegmentation(this);

        copy._boundaryVoxelCountRecipe        = configure(_boundaryVoxelCountRecipe);
        copy._boundaryVoxelCountPin           = null;

        return copy;
    }

    /// <summary>Pins <c>spacing</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySegmentation WithSpacing(VoxelSpacing value) {
        AnySegmentation copy = new AnySegmentation(this);

        copy._spacing = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>spacing</c> is drawn from — <c>AnyValidSpacing()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVoxelSpacing, AnyVoxelSpacing&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegmentation WithSpacing(Func<AnyVoxelSpacing, AnyVoxelSpacing> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegmentation copy = new AnySegmentation(this);

        copy._spacingRecipe = configure(_spacingRecipe);
        copy._spacing       = copy._spacingRecipe;

        return copy;
    }

    /// <summary>Pins <c>centroid</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySegmentation WithCentroid(SpatialPoint value) {
        AnySegmentation copy = new AnySegmentation(this);

        copy._centroid = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>centroid</c> is drawn from — <c>AnyValidCentroid()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySpatialPoint, AnySpatialPoint&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySegmentation WithCentroid(Func<AnySpatialPoint, AnySpatialPoint> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySegmentation copy = new AnySegmentation(this);

        copy._centroidRecipe = configure(_centroidRecipe);
        copy._centroid       = copy._centroidRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Segmentation" />.</summary>
    public Segmentation Generate() {
        // The segmented volume cannot exceed what a nodule can have: draw the spacing first, then bound the voxel count by it.
        VoxelSpacing spacing = _spacing.Generate();
        int maxVoxelCount = (int)Math.Min(Segmentation.MaxVoxelCount, Math.Floor(MeasuredVolume.MaxVolumeMm3 / spacing.VoxelVolumeMm3));
        while (maxVoxelCount * spacing.VoxelVolumeMm3 > MeasuredVolume.MaxVolumeMm3) { maxVoxelCount--; }

        (int boundaryVoxelCount, int voxelCount) = BoundaryVoxelCountAndVoxelCount(maxVoxelCount);

        return new Segmentation(voxelCount,
                                boundaryVoxelCount,
                                spacing,
                                _centroid.Generate());
    }

}
