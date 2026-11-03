// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CopyRelation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CopyRelation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCopyRelation : IAny<CopyRelation> {

    private IAny<WitnessSiglum> _exemplar;
    private IAny<WitnessSiglum> _copy;
    private IAny<double>        _confidence;
    private IAny<int>           _sharedErrorCount;

    private AnyWitnessSiglum _exemplarRecipe;
    private AnyWitnessSiglum _copyRecipe;
    private AnyDouble        _confidenceRecipe;
    private AnyInt32         _sharedErrorCountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCopyRelation()
        : this(exemplar:         AnyValidExemplar(),
               copy:             AnyValidCopy(),
               confidence:       AnyValidConfidence(),
               sharedErrorCount: AnyValidSharedErrorCount()) { }

    private static AnyWitnessSiglum AnyValidExemplar() {
        return new AnyWitnessSiglum();
    }

    private static AnyWitnessSiglum AnyValidCopy() {
        return new AnyWitnessSiglum();
    }

    private static AnyDouble AnyValidConfidence() {
        return Any.Double().Positive().LessThanOrEqualTo(1d);
    }

    private static AnyInt32 AnyValidSharedErrorCount() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCopyRelation(AnyWitnessSiglum exemplar,
                            AnyWitnessSiglum copy,
                            AnyDouble        confidence,
                            AnyInt32         sharedErrorCount) {
        _exemplar               = exemplar;
        _exemplarRecipe         = exemplar;
        _copy                   = copy;
        _copyRecipe             = copy;
        _confidence             = confidence;
        _confidenceRecipe       = confidence;
        _sharedErrorCount       = sharedErrorCount;
        _sharedErrorCountRecipe = sharedErrorCount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCopyRelation(AnyCopyRelation from) {
        _exemplar               = from._exemplar;
        _exemplarRecipe         = from._exemplarRecipe;
        _copy                   = from._copy;
        _copyRecipe             = from._copyRecipe;
        _confidence             = from._confidence;
        _confidenceRecipe       = from._confidenceRecipe;
        _sharedErrorCount       = from._sharedErrorCount;
        _sharedErrorCountRecipe = from._sharedErrorCountRecipe;
    }

    /// <summary>Pins <c>exemplar</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCopyRelation WithExemplar(WitnessSiglum value) {
        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._exemplar = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>exemplar</c> is drawn from — <c>AnyValidExemplar()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWitnessSiglum, AnyWitnessSiglum&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCopyRelation WithExemplar(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._exemplarRecipe = configure(_exemplarRecipe);
        copy._exemplar       = copy._exemplarRecipe;

        return copy;
    }

    /// <summary>Pins <c>copy</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCopyRelation WithCopy(WitnessSiglum value) {
        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._copy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>copy</c> is drawn from — <c>AnyValidCopy()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyWitnessSiglum, AnyWitnessSiglum&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCopyRelation WithCopy(Func<AnyWitnessSiglum, AnyWitnessSiglum> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._copyRecipe = configure(_copyRecipe);
        copy._copy       = copy._copyRecipe;

        return copy;
    }

    /// <summary>Pins <c>confidence</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCopyRelation WithConfidence(double value) {
        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._confidence = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>confidence</c> is drawn from — <c>AnyValidConfidence()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDouble, AnyDouble&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCopyRelation WithConfidence(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._confidenceRecipe = configure(_confidenceRecipe);
        copy._confidence       = copy._confidenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>sharedErrorCount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCopyRelation WithSharedErrorCount(int value) {
        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._sharedErrorCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sharedErrorCount</c> is drawn from — <c>AnyValidSharedErrorCount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCopyRelation WithSharedErrorCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCopyRelation copy = new AnyCopyRelation(this);

        copy._sharedErrorCountRecipe = configure(_sharedErrorCountRecipe);
        copy._sharedErrorCount       = copy._sharedErrorCountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CopyRelation" />.</summary>
    public CopyRelation Generate() {
        // A witness is never a copy of itself: the exemplar is drawn different from the copy.
        WitnessSiglum copy = _copy.Generate();

        return new CopyRelation(_exemplar.DifferentFrom(copy).Generate(),
                                copy,
                                _confidence.Generate(),
                                _sharedErrorCount.Generate());
    }

}
