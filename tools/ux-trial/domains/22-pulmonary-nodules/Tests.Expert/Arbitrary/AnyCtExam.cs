// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CtExam --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="CtExam" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCtExam : IAny<CtExam> {

    private readonly IAny<string>                                  _accessionNumber;
    private readonly IAny<DateOnly>                                _acquiredOn;
    private readonly IAny<VoxelSpacing>                            _spacing;
    private readonly Func<VoxelSpacing, IAny<double>>              _sliceThicknessMm;
    private readonly IAny<IReadOnlyCollection<AnatomicalLandmark>> _landmarks;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCtExam()
        : this(accessionNumber:  AnyValidAccessionNumber(),
               acquiredOn:       AnyValidAcquiredOn(),
               spacing:          AnyValidSpacing(),
               sliceThicknessMm: AnyValidSliceThicknessMm,
               landmarks:        AnyValidLandmarks()) { }

    private static AnyPattern AnyValidAccessionNumber() {
        return Any.StringMatching("CT[0-9]{8}");
    }

    // No guard bounds the date. The window is the era of clinical CT, wide enough to be arbitrary and
    // far enough from DateOnly's limits for a follow-up to be scheduled months later.
    private static AnyDateOnly AnyValidAcquiredOn() {
        return Any.DateOnly().Between(new DateOnly(1980, 1, 1), new DateOnly(2090, 12, 31));
    }

    // An exam reconstructs slices no thicker than 5 mm, and its slice spacing cannot exceed its thickness.
    private static AnyVoxelSpacing AnyValidSpacing() {
        return new AnyVoxelSpacing().WithSliceMm(s => s.LessThanOrEqualTo(CtExam.MaxSliceThicknessMm));
    }

    private static AnyDouble AnyValidSliceThicknessMm(VoxelSpacing spacing) {
        return Any.Double().Between(Math.Max(CtExam.MinSliceThicknessMm, spacing.SliceMm), CtExam.MaxSliceThicknessMm);
    }

    // Each landmark kind is located at most once per exam. Distinctness under a comparer cannot tell how
    // many kinds exist, so the count says it: without it, a draw of nine landmarks exhausts the budget.
    private static AnyList<AnatomicalLandmark> AnyValidLandmarks() {
        return Any.ListOf(new AnyAnatomicalLandmark())
                  .Distinct(LandmarkKindComparer.Instance)
                  .WithMaxCount(Enum.GetValues<LandmarkKind>().Length);
    }

    private AnyCtExam(IAny<string>                                  accessionNumber,
                      IAny<DateOnly>                                acquiredOn,
                      IAny<VoxelSpacing>                            spacing,
                      Func<VoxelSpacing, IAny<double>>              sliceThicknessMm,
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
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, _ => new FixedValue<double>(value), _landmarks);
    }

    /// <summary>Draws <c>sliceThicknessMm</c> from <c>AnyValidSliceThicknessMm(spacing)</c>, refined by <paramref name="configure" />.</summary>
    public AnyCtExam WithSliceThicknessMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCtExam(_accessionNumber, _acquiredOn, _spacing, spacing => configure(AnyValidSliceThicknessMm(spacing)), _landmarks);
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
        VoxelSpacing spacing = _spacing.Generate();

        return new CtExam(_accessionNumber.Generate(),
                          _acquiredOn.Generate(),
                          spacing,
                          _sliceThicknessMm(spacing).Generate(),
                          _landmarks.Generate());
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
