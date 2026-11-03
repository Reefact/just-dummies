// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Gauge --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Gauge" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGauge : IAny<Gauge> {

    private IAny<decimal> _stitchesPer10Cm;

    private AnyDecimal _stitchesPer10CmRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGauge()
        : this(stitchesPer10Cm: AnyValidStitchesPer10Cm()) { }

    private static AnyDecimal AnyValidStitchesPer10Cm() {
        return Any.Decimal().Between(4m, 60m);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGauge(AnyDecimal stitchesPer10Cm) {
        _stitchesPer10Cm       = stitchesPer10Cm;
        _stitchesPer10CmRecipe = stitchesPer10Cm;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGauge(AnyGauge from) {
        _stitchesPer10Cm       = from._stitchesPer10Cm;
        _stitchesPer10CmRecipe = from._stitchesPer10CmRecipe;
    }

    /// <summary>Pins <c>stitchesPer10Cm</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGauge WithStitchesPer10Cm(decimal value) {
        AnyGauge copy = new AnyGauge(this);

        copy._stitchesPer10Cm = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>stitchesPer10Cm</c> is drawn from — <c>AnyValidStitchesPer10Cm()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGauge WithStitchesPer10Cm(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGauge copy = new AnyGauge(this);

        copy._stitchesPer10CmRecipe = configure(_stitchesPer10CmRecipe);
        copy._stitchesPer10Cm       = copy._stitchesPer10CmRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Gauge" />.</summary>
    public Gauge Generate() {
        // Rows per 10 cm lie in 4..80 and within half to twice the stitches: draw them inside both.
        decimal stitches = _stitchesPer10Cm.Generate();
        decimal rows     = Any.Decimal().Between(Math.Max(4m, stitches / 2m), Math.Min(80m, stitches * 2m)).Generate();

        return new Gauge(stitches, rows);
    }

}
