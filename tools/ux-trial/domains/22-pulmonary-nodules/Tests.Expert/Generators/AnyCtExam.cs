// Scaffolded by `any` (JustDummies), then completed by hand: the accession number, the slice rule between
// spacing and thickness, and the unique landmark kinds are written below.
// `any generate CtExam --force` overwrites it.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="CtExam" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCtExam : IAny<CtExam> {

    private IAny<string>                                  _accessionNumber;
    private IAny<DateOnly>                                _acquiredOn;
    private IAny<IReadOnlyCollection<AnatomicalLandmark>> _landmarks;

    private AnyPattern      _accessionNumberRecipe;
    private AnyDateOnly     _acquiredOnRecipe;
    private AnyVoxelSpacing _spacingRecipe;
    private AnyDouble       _sliceThicknessMmRecipe;

    private VoxelSpacing? _spacingPin;
    private double?       _sliceThicknessMmPin;

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
        // The domain accepts any date; a bounded window keeps AddDays safe in the tests that move an exam in time.
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2040, 12, 31));
    }

    private static AnyVoxelSpacing AnyValidSpacing() {
        // The slice spacing cannot exceed the thickness, and the thickness tops out at MaxSliceThicknessMm.
        return new AnyVoxelSpacing().WithSliceMm(s => s.LessThanOrEqualTo(CtExam.MaxSliceThicknessMm));
    }

    private static AnyDouble AnyValidSliceThicknessMm() {
        return Any.Double().Between(CtExam.MinSliceThicknessMm, CtExam.MaxSliceThicknessMm);
    }

    private static IAny<IReadOnlyCollection<AnatomicalLandmark>> AnyValidLandmarks() {
        // Each landmark kind at most once: draw distinct kinds as dictionary keys, give each a position.
        return Any.DictionaryOf(Any.Enum<LandmarkKind>(), new AnySpatialPoint())
                  .WithCountBetween(0, Enum.GetValues(typeof(LandmarkKind)).Length)
                  .As(positions => (IReadOnlyCollection<AnatomicalLandmark>)positions.Select(p => new AnatomicalLandmark(p.Key, p.Value)).ToList());
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCtExam(AnyPattern                                      accessionNumber,
                      AnyDateOnly                                    acquiredOn,
                      AnyVoxelSpacing                                spacing,
                      AnyDouble                                      sliceThicknessMm,
                      IAny<IReadOnlyCollection<AnatomicalLandmark>> landmarks) {
        _accessionNumber        = accessionNumber;
        _accessionNumberRecipe  = accessionNumber;
        _acquiredOn             = acquiredOn;
        _acquiredOnRecipe       = acquiredOn;
        _spacingRecipe          = spacing;
        _sliceThicknessMmRecipe = sliceThicknessMm;
        _landmarks              = landmarks;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCtExam(AnyCtExam from) {
        _accessionNumber        = from._accessionNumber;
        _accessionNumberRecipe  = from._accessionNumberRecipe;
        _acquiredOn             = from._acquiredOn;
        _acquiredOnRecipe       = from._acquiredOnRecipe;
        _spacingRecipe          = from._spacingRecipe;
        _spacingPin             = from._spacingPin;
        _sliceThicknessMmRecipe = from._sliceThicknessMmRecipe;
        _sliceThicknessMmPin    = from._sliceThicknessMmPin;
        _landmarks              = from._landmarks;
    }

    public AnyCtExam WithAccessionNumber(string value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._accessionNumber = Any.Fixed(value);

        return copy;
    }

    public AnyCtExam WithAcquiredOn(DateOnly value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._acquiredOn = Any.Fixed(value);

        return copy;
    }

    public AnyCtExam WithAcquiredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._acquiredOnRecipe = configure(_acquiredOnRecipe);
        copy._acquiredOn       = copy._acquiredOnRecipe;

        return copy;
    }

    /// <summary>Pins the spacing; the thickness is then drawn at least as thick as its slice spacing.</summary>
    public AnyCtExam WithSpacing(VoxelSpacing value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._spacingPin = value;

        return copy;
    }

    public AnyCtExam WithSpacing(Func<AnyVoxelSpacing, AnyVoxelSpacing> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._spacingRecipe = configure(_spacingRecipe);
        copy._spacingPin    = null;

        return copy;
    }

    /// <summary>Pins the thickness; the spacing is then drawn with a slice spacing no coarser than it.</summary>
    public AnyCtExam WithSliceThicknessMm(double value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._sliceThicknessMmPin = value;

        return copy;
    }

    public AnyCtExam WithSliceThicknessMm(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCtExam copy = new AnyCtExam(this);

        copy._sliceThicknessMmRecipe = configure(_sliceThicknessMmRecipe);
        copy._sliceThicknessMmPin    = null;

        return copy;
    }

    public AnyCtExam WithLandmarks(IReadOnlyCollection<AnatomicalLandmark> value) {
        AnyCtExam copy = new AnyCtExam(this);

        copy._landmarks = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CtExam" />.</summary>
    public CtExam Generate() {
        // A pinned thickness that the exam will refuse anyway (below the finest spacing, above the coarsest slice, NaN)
        // is left to the constructor: it cannot bound the spacing, and the refusal should be the exam's own.
        double sliceCeiling = _sliceThicknessMmPin is { } pinned && pinned >= VoxelSpacing.MinSpacingMm && pinned <= CtExam.MaxSliceThicknessMm
                                  ? pinned
                                  : CtExam.MaxSliceThicknessMm;
        VoxelSpacing spacing = _spacingPin
                            ?? _spacingRecipe.WithSliceMm(s => s.LessThanOrEqualTo(sliceCeiling)).Generate();
        double sliceThicknessMm = _sliceThicknessMmPin
                               ?? _sliceThicknessMmRecipe.GreaterThanOrEqualTo(spacing.SliceMm).Generate();

        return new CtExam(_accessionNumber.Generate(),
                          _acquiredOn.Generate(),
                          spacing,
                          sliceThicknessMm,
                          _landmarks.Generate());
    }

}
