// Scaffolded by `any` (JustDummies), then completed by hand: the segmentation is drawn on the exam's spacing.
// `any generate NoduleObservation --force` overwrites it.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="NoduleObservation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNoduleObservation : IAny<NoduleObservation> {

    private AnyCtExam       _examRecipe;
    private AnySegmentation _segmentationRecipe;

    private CtExam?       _examPin;
    private Segmentation? _segmentationPin;

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

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyNoduleObservation(AnyCtExam       exam,
                                 AnySegmentation segmentation) {
        _examRecipe         = exam;
        _segmentationRecipe = segmentation;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNoduleObservation(AnyNoduleObservation from) {
        _examRecipe         = from._examRecipe;
        _examPin            = from._examPin;
        _segmentationRecipe = from._segmentationRecipe;
        _segmentationPin    = from._segmentationPin;
    }

    /// <summary>Pins the exam; the segmentation is then drawn on its spacing.</summary>
    public AnyNoduleObservation WithExam(CtExam value) {
        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._examPin = value;

        return copy;
    }

    public AnyNoduleObservation WithExam(Func<AnyCtExam, AnyCtExam> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._examRecipe = configure(_examRecipe);
        copy._examPin    = null;

        return copy;
    }

    /// <summary>Pins the segmentation; the exam is then drawn on its spacing.</summary>
    public AnyNoduleObservation WithSegmentation(Segmentation value) {
        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._segmentationPin = value;

        return copy;
    }

    public AnyNoduleObservation WithSegmentation(Func<AnySegmentation, AnySegmentation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._segmentationRecipe = configure(_segmentationRecipe);
        copy._segmentationPin    = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NoduleObservation" />.</summary>
    public NoduleObservation Generate() {
        CtExam exam = _examPin
                   ?? (_segmentationPin is { } pinned ? _examRecipe.WithSpacing(pinned.Spacing).Generate() : _examRecipe.Generate());
        Segmentation segmentation = _segmentationPin
                                 ?? _segmentationRecipe.WithSpacing(exam.Spacing).Generate();

        return new NoduleObservation(exam, segmentation);
    }

}
