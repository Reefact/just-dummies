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

    private readonly IAny<CtExam>       _exam;
    private readonly IAny<Segmentation> _segmentation;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyNoduleObservation()
        : this(exam:         AnyValidExam(),
               segmentation: AnyValidSegmentation()) { }

    private static AnyCtExam AnyValidExam() {
        return new AnyCtExam();
    }

    private static AnySegmentation AnyValidSegmentation() {
        return new AnySegmentation();
    }

    private AnyNoduleObservation(IAny<CtExam>       exam,
                                 IAny<Segmentation> segmentation) {
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

    /// <summary>Pins <c>segmentation</c> to a fixed value.</summary>
    public AnyNoduleObservation WithSegmentation(Segmentation value) {
        return new AnyNoduleObservation(_exam, new FixedValue<Segmentation>(value));
    }

    /// <summary>Draws <c>segmentation</c> from <c>AnyValidSegmentation()</c>, refined by <paramref name="configure" />.</summary>
    public AnyNoduleObservation WithSegmentation(Func<AnySegmentation, AnySegmentation> configure) {
        return new AnyNoduleObservation(_exam, configure(AnyValidSegmentation()));
    }

    /// <summary>Produces one arbitrary <see cref="NoduleObservation" />.</summary>
    public NoduleObservation Generate() {
        // A segmentation is drawn on its exam: unless pinned, it takes the exam's spacing.
        CtExam exam = _exam.Generate();
        IAny<Segmentation> segmentation = _segmentation is AnySegmentation anySegmentation ? anySegmentation.WithSpacing(exam.Spacing) : _segmentation;

        return new NoduleObservation(exam, segmentation.Generate());
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
