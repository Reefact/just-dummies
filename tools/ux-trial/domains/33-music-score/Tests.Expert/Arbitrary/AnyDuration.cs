// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Duration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Duration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDuration : IAny<Duration> {

    private IAny<long> _numerator;
    private IAny<long> _denominator;

    private AnyInt64 _numeratorRecipe;
    private AnyInt64 _denominatorRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDuration()
        : this(numerator:   AnyValidNumerator(),
               denominator: AnyValidDenominator()) { }

    private static AnyInt64 AnyValidNumerator() {
        // Non-negative is the domain's rule. The ceiling is not: it keeps the exact arithmetic of
        // Duration (+, comparisons) far from long overflow, which no guard declares.
        return Any.Int64().Between(0, 4096);
    }

    private static AnyInt64 AnyValidDenominator() {
        // Any denominator up to Duration.MaxDenominator reduces to a notatable one: the normalised
        // denominator can only be smaller than the one given.
        return Any.Int64().Between(1, Duration.MaxDenominator);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDuration(AnyInt64 numerator,
                        AnyInt64 denominator) {
        _numerator         = numerator;
        _numeratorRecipe   = numerator;
        _denominator       = denominator;
        _denominatorRecipe = denominator;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDuration(AnyDuration from) {
        _numerator         = from._numerator;
        _numeratorRecipe   = from._numeratorRecipe;
        _denominator       = from._denominator;
        _denominatorRecipe = from._denominatorRecipe;
    }

    /// <summary>Pins <c>numerator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDuration WithNumerator(long value) {
        AnyDuration copy = new AnyDuration(this);

        copy._numerator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>numerator</c> is drawn from — <c>AnyValidNumerator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt64, AnyInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDuration WithNumerator(Func<AnyInt64, AnyInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDuration copy = new AnyDuration(this);

        copy._numeratorRecipe = configure(_numeratorRecipe);
        copy._numerator       = copy._numeratorRecipe;

        return copy;
    }

    /// <summary>Pins <c>denominator</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDuration WithDenominator(long value) {
        AnyDuration copy = new AnyDuration(this);

        copy._denominator = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>denominator</c> is drawn from — <c>AnyValidDenominator()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt64, AnyInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDuration WithDenominator(Func<AnyInt64, AnyInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDuration copy = new AnyDuration(this);

        copy._denominatorRecipe = configure(_denominatorRecipe);
        copy._denominator       = copy._denominatorRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Duration" />.</summary>
    public Duration Generate() {
        return new Duration(_numerator.Generate(),
                            _denominator.Generate());
    }

}
