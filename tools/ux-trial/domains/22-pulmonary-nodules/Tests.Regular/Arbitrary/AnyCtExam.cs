// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CtExam --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<string>                                  _accessionNumber;
    private readonly IAny<DateOnly>                                _acquiredOn;
    private readonly IAny<VoxelSpacing>                            _spacing;
    private readonly IAny<double>                                  _sliceThicknessMm;
    private readonly IAny<IReadOnlyCollection<AnatomicalLandmark>> _landmarks;

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
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2035, 12, 31));
    }

    private static AnyVoxelSpacing AnyValidSpacing() {
        return new AnyVoxelSpacing().WithSliceMm(s => s.LessThanOrEqualTo(CtExam.MaxSliceThicknessMm));
    }

    private static AnyDouble AnyValidSliceThicknessMm() {
        return Any.Double().Between(CtExam.MinSliceThicknessMm, CtExam.MaxSliceThicknessMm);
    }

    private static AnyList<AnatomicalLandmark> AnyValidLandmarks() {
        return Any.ListOf(new AnyAnatomicalLandmark())
                  .WithMaxCount(Enum.GetValues<LandmarkKind>().Length)
                  .Distinct(SameKind.Instance);
    }

    private AnyCtExam(IAny<string>                                  accessionNumber,
                      IAny<DateOnly>                                acquiredOn,
                      IAny<VoxelSpacing>                            spacing,
                      IAny<double>                                  sliceThicknessMm,
                      IAny<IReadOnlyCollection<AnatomicalLandmark>> landmarks) {
        _accessionNumber  = accessionNumber;
        _acquiredOn       = acquiredOn;
        _spacing          = spacing;
        _sliceThicknessMm = sliceThicknessMm;
        _landmarks        = landmarks;
    }

    /// <summary>Pins <c>accessionNumber</c> to a fixed value.</summary>
    public AnyCtExam WithAccessionNumber(string value) {
        return new AnyCtExam(new FixedValue<string>(value), _acquiredOn, _spacing, _sliceThicknessMm, _landmarks);
    }

    /// <summary>Draws <c>accessionNumber</c> from <c>AnyValidAccessionNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithAccessionNumber(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCtExam(configure(AnyValidAccessionNumber()), _acquiredOn, _spacing, _sliceThicknessMm, _landmarks);
    }

    /// <summary>Pins <c>acquiredOn</c> to a fixed value.</summary>
    public AnyCtExam WithAcquiredOn(DateOnly value) {
        return new AnyCtExam(_accessionNumber, new FixedValue<DateOnly>(value), _spacing, _sliceThicknessMm, _landmarks);
    }

    /// <summary>Draws <c>acquiredOn</c> from <c>AnyValidAcquiredOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithAcquiredOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyCtExam(_accessionNumber, configure(AnyValidAcquiredOn()), _spacing, _sliceThicknessMm, _landmarks);
    }

    /// <summary>Pins <c>spacing</c> to a fixed value.</summary>
    public AnyCtExam WithSpacing(VoxelSpacing value) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, new FixedValue<VoxelSpacing>(value), _sliceThicknessMm, _landmarks);
    }

    /// <summary>Draws <c>spacing</c> from <c>AnyValidSpacing()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithSpacing(Func<AnyVoxelSpacing, AnyVoxelSpacing> configure) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, configure(AnyValidSpacing()), _sliceThicknessMm, _landmarks);
    }

    /// <summary>Pins <c>sliceThicknessMm</c> to a fixed value.</summary>
    public AnyCtExam WithSliceThicknessMm(double value) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, new FixedValue<double>(value), _landmarks);
    }

    /// <summary>Draws <c>sliceThicknessMm</c> from <c>AnyValidSliceThicknessMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithSliceThicknessMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, configure(AnyValidSliceThicknessMm()), _landmarks);
    }

    /// <summary>Pins <c>landmarks</c> to a fixed value.</summary>
    public AnyCtExam WithLandmarks(IReadOnlyCollection<AnatomicalLandmark> value) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, _sliceThicknessMm, new FixedValue<IReadOnlyCollection<AnatomicalLandmark>>(value));
    }

    /// <summary>Draws <c>landmarks</c> from <c>AnyValidLandmarks()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithLandmarks(Func<AnyList<AnatomicalLandmark>, AnyList<AnatomicalLandmark>> configure) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, _sliceThicknessMm, configure(AnyValidLandmarks()));
    }

    /// <summary>Produces one arbitrary <see cref="CtExam" />.</summary>
    public CtExam Generate() {
        // The slice spacing cannot exceed the slice thickness: whichever is drawn follows the other.
        VoxelSpacing spacing;
        double sliceThicknessMm;
        if (_sliceThicknessMm is AnyDouble anyThickness) {
            spacing          = _spacing.Generate();
            sliceThicknessMm = anyThickness.GreaterThanOrEqualTo(spacing.SliceMm).Generate();
        } else {
            sliceThicknessMm = _sliceThicknessMm.Generate();
            double thickness = sliceThicknessMm;
            spacing = (_spacing is AnyVoxelSpacing anySpacing ? anySpacing.WithSliceMm(s => s.LessThanOrEqualTo(thickness)) : _spacing).Generate();
        }

        return new CtExam(_accessionNumber.Generate(), _acquiredOn.Generate(), spacing, sliceThicknessMm, _landmarks.Generate());
    }

    private sealed class SameKind : IEqualityComparer<AnatomicalLandmark> {

        public static readonly SameKind Instance = new SameKind();

        public bool Equals(AnatomicalLandmark? x, AnatomicalLandmark? y) => x?.Kind == y?.Kind;

        public int GetHashCode(AnatomicalLandmark obj) => obj.Kind.GetHashCode();

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
