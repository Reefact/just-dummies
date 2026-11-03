// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StitchCountMismatchException --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="StitchCountMismatchException" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStitchCountMismatchException : IAny<StitchCountMismatchException> {

    private IAny<int> _rowNumber;
    private IAny<int> _liveStitches;
    private IAny<int> _requiredStitches;

    private AnyInt32 _rowNumberRecipe;
    private AnyInt32 _liveStitchesRecipe;
    private AnyInt32 _requiredStitchesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStitchCountMismatchException()
        : this(rowNumber:        AnyValidRowNumber(),
               liveStitches:     AnyValidLiveStitches(),
               requiredStitches: AnyValidRequiredStitches()) { }

    private static AnyInt32 AnyValidRowNumber() {
        return Any.Int32();
    }

    private static AnyInt32 AnyValidLiveStitches() {
        return Any.Int32();
    }

    private static AnyInt32 AnyValidRequiredStitches() {
        return Any.Int32();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStitchCountMismatchException(AnyInt32 rowNumber,
                                            AnyInt32 liveStitches,
                                            AnyInt32 requiredStitches) {
        _rowNumber              = rowNumber;
        _rowNumberRecipe        = rowNumber;
        _liveStitches           = liveStitches;
        _liveStitchesRecipe     = liveStitches;
        _requiredStitches       = requiredStitches;
        _requiredStitchesRecipe = requiredStitches;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStitchCountMismatchException(AnyStitchCountMismatchException from) {
        _rowNumber              = from._rowNumber;
        _rowNumberRecipe        = from._rowNumberRecipe;
        _liveStitches           = from._liveStitches;
        _liveStitchesRecipe     = from._liveStitchesRecipe;
        _requiredStitches       = from._requiredStitches;
        _requiredStitchesRecipe = from._requiredStitchesRecipe;
    }

    /// <summary>Pins <c>rowNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchCountMismatchException WithRowNumber(int value) {
        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._rowNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>rowNumber</c> is drawn from — <c>AnyValidRowNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchCountMismatchException WithRowNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._rowNumberRecipe = configure(_rowNumberRecipe);
        copy._rowNumber       = copy._rowNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>liveStitches</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchCountMismatchException WithLiveStitches(int value) {
        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._liveStitches = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>liveStitches</c> is drawn from — <c>AnyValidLiveStitches()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchCountMismatchException WithLiveStitches(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._liveStitchesRecipe = configure(_liveStitchesRecipe);
        copy._liveStitches       = copy._liveStitchesRecipe;

        return copy;
    }

    /// <summary>Pins <c>requiredStitches</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStitchCountMismatchException WithRequiredStitches(int value) {
        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._requiredStitches = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>requiredStitches</c> is drawn from — <c>AnyValidRequiredStitches()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStitchCountMismatchException WithRequiredStitches(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStitchCountMismatchException copy = new AnyStitchCountMismatchException(this);

        copy._requiredStitchesRecipe = configure(_requiredStitchesRecipe);
        copy._requiredStitches       = copy._requiredStitchesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="StitchCountMismatchException" />.</summary>
    public StitchCountMismatchException Generate() {
        return new StitchCountMismatchException(_rowNumber.Generate(),
                                                _liveStitches.Generate(),
                                                _requiredStitches.Generate());
    }

}
