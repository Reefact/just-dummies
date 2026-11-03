// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DrawingScale --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="DrawingScale" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDrawingScale : IAny<DrawingScale> {

    private IAny<int> _numerator;
    private IAny<int> _denominator;

    private AnyInt32 _numeratorRecipe;
    private AnyInt32 _denominatorRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDrawingScale()
        : this(numerator:   AnyValidNumerator(),
               denominator: AnyValidDenominator()) { }

    private static AnyInt32 AnyValidNumerator() {
        return Any.Int32().Positive();
    }

    private static AnyInt32 AnyValidDenominator() {
        return Any.Int32().Positive();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDrawingScale(AnyInt32 numerator,
                            AnyInt32 denominator) {
        _numerator         = numerator;
        _numeratorRecipe   = numerator;
        _denominator       = denominator;
        _denominatorRecipe = denominator;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDrawingScale(AnyDrawingScale from) {
        _numerator         = from._numerator;
        _numeratorRecipe   = from._numeratorRecipe;
        _denominator       = from._denominator;
        _denominatorRecipe = from._denominatorRecipe;
    }

    /// <summary>Pins <c>numerator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawingScale WithNumerator(int value) {
        AnyDrawingScale copy = new AnyDrawingScale(this);

        copy._numerator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>numerator</c> is drawn from — <c>AnyValidNumerator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawingScale WithNumerator(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawingScale copy = new AnyDrawingScale(this);

        copy._numeratorRecipe = configure(_numeratorRecipe);
        copy._numerator       = copy._numeratorRecipe;

        return copy;
    }

    /// <summary>Pins <c>denominator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDrawingScale WithDenominator(int value) {
        AnyDrawingScale copy = new AnyDrawingScale(this);

        copy._denominator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>denominator</c> is drawn from — <c>AnyValidDenominator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDrawingScale WithDenominator(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDrawingScale copy = new AnyDrawingScale(this);

        copy._denominatorRecipe = configure(_denominatorRecipe);
        copy._denominator       = copy._denominatorRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DrawingScale" />.</summary>
    public DrawingScale Generate() {
        // One side of an ISO 5455 scale is always 1: an enlargement n:1 or a reduction 1:n.
        // The side that is not 1 is drawn from its recipe; a test that cares which kind it gets writes the scale as a literal.
        bool enlargement = Any.Boolean().Generate();

        return enlargement ? new DrawingScale(_numerator.Generate(), 1)
                           : new DrawingScale(1, _denominator.Generate());
    }

}
