// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NoduleObservation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="NoduleObservation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNoduleObservation : IAny<NoduleObservation> {

    private readonly IAny<CtExam>                         _exam;
    private readonly Func<VoxelSpacing, IAny<Segmentation>> _segmentation;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNoduleObservation()
        : this(exam:         AnyValidExam(),
               segmentation: spacing => AnyValidSegmentation().WithSpacing(spacing)) { }

    private static AnyCtExam AnyValidExam() {
        return new AnyCtExam();
    }

    private static AnySegmentation AnyValidSegmentation() {
        return new AnySegmentation();
    }

    // Hand-edited: a segmentation is drawn on its exam, so its spacing is the exam's. The segmentation
    // is therefore drawn once the exam is known, rather than independently.
    private AnyNoduleObservation(IAny<CtExam>                         exam,
                                 Func<VoxelSpacing, IAny<Segmentation>> segmentation) {
        _exam         = exam;
        _segmentation = segmentation;
    }

    /// <summary>Pins <c>exam</c> to a fixed value.</summary>
    public AnyNoduleObservation WithExam(CtExam value) {
        return new AnyNoduleObservation(new FixedValue<CtExam>(value), _segmentation);
    }

    /// <summary>Draws <c>exam</c> from <c>AnyValidExam()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNoduleObservation WithExam(Func<AnyCtExam, AnyCtExam> configure) {
        return new AnyNoduleObservation(configure(AnyValidExam()), _segmentation);
    }

    /// <summary>Pins <c>segmentation</c> to a fixed value; it must have been drawn on the exam's spacing.</summary>
    public AnyNoduleObservation WithSegmentation(Segmentation value) {
        return new AnyNoduleObservation(_exam, _ => new FixedValue<Segmentation>(value));
    }

    /// <summary>Draws <c>segmentation</c> from <c>AnyValidSegmentation()</c>, refined by <paramref name="configure" />, on the exam's spacing.</summary>
    public AnyNoduleObservation WithSegmentation(Func<AnySegmentation, AnySegmentation> configure) {
        return new AnyNoduleObservation(_exam, spacing => configure(AnyValidSegmentation()).WithSpacing(spacing));
    }

    /// <summary>Produces one arbitrary <see cref="NoduleObservation" />.</summary>
    public NoduleObservation Generate() {
        CtExam exam = _exam.Generate();

        return new NoduleObservation(exam, _segmentation(exam.Spacing).Generate());
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
