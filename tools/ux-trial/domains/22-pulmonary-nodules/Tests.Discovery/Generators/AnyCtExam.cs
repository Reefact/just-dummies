// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CtExam --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CtExam" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCtExam : IAny<CtExam> {

    private IAny<string>                                  _accessionNumber;
    private IAny<DateOnly>                                _acquiredOn;
    private IAny<VoxelSpacing>                            _spacing;
    private IAny<double>                                  _sliceThicknessMm;
    private IAny<IReadOnlyCollection<AnatomicalLandmark>> _landmarks;

    private AnyPattern                   _accessionNumberRecipe;
    private AnyDateOnly                 _acquiredOnRecipe;
    private AnyVoxelSpacing             _spacingRecipe;
    private AnyDouble                   _sliceThicknessMmRecipe;
    private AnyList<AnatomicalLandmark> _landmarksRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCtExam()
        : this(accessionNumber:  AnyValidAccessionNumber(),
               acquiredOn:       AnyValidAcquiredOn(),
               spacing:          AnyValidSpacing(),
               sliceThicknessMm: AnyValidSliceThicknessMm(),
               landmarks:        AnyValidLandmarks()) { }

    private static AnyPattern AnyValidAccessionNumber() {
        return Any.StringMatching("CT[0-9]{8}");
    }

    private static AnyDateOnly AnyValidAcquiredOn() {
        return Any.DateOnly();
    }

    private static AnyVoxelSpacing AnyValidSpacing() {
        // The thinnest slice an exam can have is 0.5 mm: a slice spacing no larger than that fits under any thickness.
        return new AnyVoxelSpacing().WithSliceMm(s => s.LessThanOrEqualTo(CtExam.MinSliceThicknessMm));
    }

    private static AnyDouble AnyValidSliceThicknessMm() {
        return Any.Double().Between(CtExam.MinSliceThicknessMm, CtExam.MaxSliceThicknessMm);
    }

    private static AnyList<AnatomicalLandmark> AnyValidLandmarks() {
        return Any.ListOf(new AnyAnatomicalLandmark()).DistinctBy(l => l.Kind).WithMaxCount(4);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCtExam(AnyPattern                   accessionNumber,
                      AnyDateOnly                 acquiredOn,
                      AnyVoxelSpacing             spacing,
                      AnyDouble                   sliceThicknessMm,
                      AnyList<AnatomicalLandmark> landmarks) {
        _accessionNumber        = accessionNumber;
        _accessionNumberRecipe  = accessionNumber;
        _acquiredOn             = acquiredOn;
        _acquiredOnRecipe       = acquiredOn;
        _spacing                = spacing;
        _spacingRecipe          = spacing;
        _sliceThicknessMm       = sliceThicknessMm;
        _sliceThicknessMmRecipe = sliceThicknessMm;
        _landmarks              = landmarks;
        _landmarksRecipe        = landmarks;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCtExam(AnyCtExam from) {
        _accessionNumber        = from._accessionNumber;
        _accessionNumberRecipe  = from._accessionNumberRecipe;
        _acquiredOn             = from._acquiredOn;
        _acquiredOnRecipe       = from._acquiredOnRecipe;
        _spacing                = from._spacing;
        _spacingRecipe          = from._spacingRecipe;
        _sliceThicknessMm       = from._sliceThicknessMm;
        _sliceThicknessMmRecipe = from._sliceThicknessMmRecipe;
        _landmarks              = from._landmarks;
        _landmarksRecipe        = from._landmarksRecipe;
    }

    /// <summary>Pins <c>accessionNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCtExam WithAccessionNumber(string value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._accessionNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accessionNumber</c> is drawn from — <c>AnyValidAccessionNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCtExam WithAccessionNumber(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._accessionNumberRecipe = configure(_accessionNumberRecipe);
        copy._accessionNumber       = copy._accessionNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>acquiredOn</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCtExam WithAcquiredOn(DateOnly value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._acquiredOn = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>acquiredOn</c> is drawn from — <c>AnyValidAcquiredOn()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCtExam WithAcquiredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._acquiredOnRecipe = configure(_acquiredOnRecipe);
        copy._acquiredOn       = copy._acquiredOnRecipe;

        return copy;
    }

    /// <summary>Pins <c>spacing</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCtExam WithSpacing(VoxelSpacing value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._spacing = Any.Fixed(value);

        // A slice spacing coarser than the slice thickness is refused: unless the thickness is pinned, keep it above this spacing.
        if (ReferenceEquals(_sliceThicknessMm, _sliceThicknessMmRecipe)) {
            copy._sliceThicknessMmRecipe = _sliceThicknessMmRecipe.GreaterThanOrEqualTo(value.SliceMm);
            copy._sliceThicknessMm       = copy._sliceThicknessMmRecipe;
        }

        return copy;
    }

    /// <summary>Refines the recipe <c>spacing</c> is drawn from — <c>AnyValidSpacing()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyVoxelSpacing, AnyVoxelSpacing&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCtExam WithSpacing(Func<AnyVoxelSpacing, AnyVoxelSpacing> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._spacingRecipe = configure(_spacingRecipe);
        copy._spacing       = copy._spacingRecipe;

        return copy;
    }

    /// <summary>Pins <c>sliceThicknessMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCtExam WithSliceThicknessMm(double value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._sliceThicknessMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sliceThicknessMm</c> is drawn from — <c>AnyValidSliceThicknessMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCtExam WithSliceThicknessMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._sliceThicknessMmRecipe = configure(_sliceThicknessMmRecipe);
        copy._sliceThicknessMm       = copy._sliceThicknessMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>landmarks</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCtExam WithLandmarks(IReadOnlyCollection<AnatomicalLandmark> value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._landmarks = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>landmarks</c> is drawn from — <c>AnyValidLandmarks()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;AnatomicalLandmark&gt;, AnyList&lt;AnatomicalLandmark&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCtExam WithLandmarks(Func<AnyList<AnatomicalLandmark>, AnyList<AnatomicalLandmark>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._landmarksRecipe = configure(_landmarksRecipe);
        copy._landmarks       = copy._landmarksRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CtExam" />.</summary>
    public CtExam Generate() {
        return new CtExam(_accessionNumber.Generate(),
                          _acquiredOn.Generate(),
                          _spacing.Generate(),
                          _sliceThicknessMm.Generate(),
                          _landmarks.Generate());
    }

}
