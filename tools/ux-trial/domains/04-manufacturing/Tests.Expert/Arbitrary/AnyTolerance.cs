// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tolerance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tolerance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTolerance : IAny<Tolerance> {

    private IAny<decimal> _nominalMm;
    private IAny<decimal> _minusMm;
    private IAny<decimal> _plusMm;

    private AnyDecimal _nominalMmRecipe;
    private AnyDecimal _minusMmRecipe;
    private AnyDecimal _plusMmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTolerance()
        : this(nominalMm: AnyValidNominalMm(),
               minusMm:   AnyValidMinusMm(),
               plusMm:    AnyValidPlusMm()) { }

    private static AnyDecimal AnyValidNominalMm() {
        // (0, 5000] mm, micrometre resolution. From 1 mm up, so that any minus below 1 mm keeps the lower limit above zero.
        return Any.Decimal().Between(1m, 5_000m).WithScale(3);
    }

    private static AnyDecimal AnyValidMinusMm() {
        // Below 1 mm: always under the nominal (see AnyValidNominalMm). May be zero, because the plus side is never zero.
        return Any.Decimal().Between(0m, 0.999m).WithScale(3);
    }

    private static AnyDecimal AnyValidPlusMm() {
        // Never zero, so that the band minus + plus is never empty.
        return Any.Decimal().Between(0.001m, 1m).WithScale(3);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyTolerance(AnyDecimal nominalMm,
                         AnyDecimal minusMm,
                         AnyDecimal plusMm) {
        _nominalMm       = nominalMm;
        _nominalMmRecipe = nominalMm;
        _minusMm         = minusMm;
        _minusMmRecipe   = minusMm;
        _plusMm          = plusMm;
        _plusMmRecipe    = plusMm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyTolerance(AnyTolerance from) {
        _nominalMm       = from._nominalMm;
        _nominalMmRecipe = from._nominalMmRecipe;
        _minusMm         = from._minusMm;
        _minusMmRecipe   = from._minusMmRecipe;
        _plusMm          = from._plusMm;
        _plusMmRecipe    = from._plusMmRecipe;
    }

    /// <summary>Pins <c>nominalMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTolerance WithNominalMm(decimal value) {
        AnyTolerance copy = new AnyTolerance(this);

        copy._nominalMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nominalMm</c> is drawn from — <c>AnyValidNominalMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTolerance WithNominalMm(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTolerance copy = new AnyTolerance(this);

        copy._nominalMmRecipe = configure(_nominalMmRecipe);
        copy._nominalMm       = copy._nominalMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>minusMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTolerance WithMinusMm(decimal value) {
        AnyTolerance copy = new AnyTolerance(this);

        copy._minusMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minusMm</c> is drawn from — <c>AnyValidMinusMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTolerance WithMinusMm(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTolerance copy = new AnyTolerance(this);

        copy._minusMmRecipe = configure(_minusMmRecipe);
        copy._minusMm       = copy._minusMmRecipe;

        return copy;
    }

    /// <summary>Pins <c>plusMm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyTolerance WithPlusMm(decimal value) {
        AnyTolerance copy = new AnyTolerance(this);

        copy._plusMm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>plusMm</c> is drawn from — <c>AnyValidPlusMm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyTolerance WithPlusMm(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyTolerance copy = new AnyTolerance(this);

        copy._plusMmRecipe = configure(_plusMmRecipe);
        copy._plusMm       = copy._plusMmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Tolerance" />.</summary>
    public Tolerance Generate() {
        return new Tolerance(_nominalMm.Generate(),
                             _minusMm.Generate(),
                             _plusMm.Generate());
    }

}
