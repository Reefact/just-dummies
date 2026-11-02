// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeterIndex --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeterIndex" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeterIndex : IAny<MeterIndex> {

    private IAny<decimal> _kwh;

    private AnyDecimal _kwhRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeterIndex()
        : this(kwh: AnyValidKwh()) { }

    private static AnyDecimal AnyValidKwh() {
        return Any.Decimal().Between(0m, MeterIndex.MaxKwh).WithScale(3);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeterIndex(AnyDecimal kwh) {
        _kwh       = kwh;
        _kwhRecipe = kwh;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeterIndex(AnyMeterIndex from) {
        _kwh       = from._kwh;
        _kwhRecipe = from._kwhRecipe;
    }

    /// <summary>Pins <c>kwh</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeterIndex WithKwh(decimal value) {
        AnyMeterIndex copy = new AnyMeterIndex(this);

        copy._kwh = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kwh</c> is drawn from — <c>AnyValidKwh()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeterIndex WithKwh(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeterIndex copy = new AnyMeterIndex(this);

        copy._kwhRecipe = configure(_kwhRecipe);
        copy._kwh       = copy._kwhRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MeterIndex" />.</summary>
    public MeterIndex Generate() {
        return new MeterIndex(_kwh.Generate());
    }

}
