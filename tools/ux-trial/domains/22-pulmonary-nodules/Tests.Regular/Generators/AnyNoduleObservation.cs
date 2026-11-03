// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate NoduleObservation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="NoduleObservation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyNoduleObservation : IAny<NoduleObservation> {

    private IAny<CtExam>       _exam;
    private IAny<Segmentation> _segmentation;

    private AnyCtExam       _examRecipe;
    private AnySegmentation _segmentationRecipe;

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
        _exam               = exam;
        _examRecipe         = exam;
        _segmentation       = segmentation;
        _segmentationRecipe = segmentation;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyNoduleObservation(AnyNoduleObservation from) {
        _exam               = from._exam;
        _examRecipe         = from._examRecipe;
        _segmentation       = from._segmentation;
        _segmentationRecipe = from._segmentationRecipe;
    }

    /// <summary>Pins <c>exam</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNoduleObservation WithExam(CtExam value) {
        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._exam = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>exam</c> is drawn from — <c>AnyValidExam()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCtExam, AnyCtExam&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNoduleObservation WithExam(Func<AnyCtExam, AnyCtExam> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._examRecipe = configure(_examRecipe);
        copy._exam       = copy._examRecipe;

        return copy;
    }

    /// <summary>Pins <c>segmentation</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyNoduleObservation WithSegmentation(Segmentation value) {
        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._segmentation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>segmentation</c> is drawn from — <c>AnyValidSegmentation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySegmentation, AnySegmentation&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyNoduleObservation WithSegmentation(Func<AnySegmentation, AnySegmentation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyNoduleObservation copy = new AnyNoduleObservation(this);

        copy._segmentationRecipe = configure(_segmentationRecipe);
        copy._segmentation       = copy._segmentationRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="NoduleObservation" />.</summary>
    public NoduleObservation Generate() {
        // The segmentation must have been drawn on this very exam: it takes the exam's spacing.
        CtExam exam = _exam.Generate();

        Segmentation segmentation = ReferenceEquals(_segmentation, _segmentationRecipe)
            ? _segmentationRecipe.WithSpacing(exam.Spacing).Generate()
            : _segmentation.Generate();

        return new NoduleObservation(exam, segmentation);
    }

}
